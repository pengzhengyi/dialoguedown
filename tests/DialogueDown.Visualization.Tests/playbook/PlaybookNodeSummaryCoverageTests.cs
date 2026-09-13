using System.Reflection;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Visualization.Playbook;
using static DialogueDown.Visualization.Tests.Support.PlaybookNodeFactory;

namespace DialogueDown.Visualization.Tests.Playbook;

/// <summary>
/// That every node kind the format declares gets a summary, and that no summary grows long enough
/// to bloat the payload or wrap a row into a wall of text.
/// </summary>
public sealed class PlaybookNodeSummaryCoverageTests
{
    private static readonly (Node Node, string Kind)[] _everyKind =
    [
        (Line("Take a torch."), NodeKinds.Line),
        (Choice(Option("Go left")), NodeKinds.Choice),
        (RandomChoice(Chance(100)), NodeKinds.RandomChoice),
        (Branch(Arm("Hero.IsBrave")), NodeKinds.Branch),
        (Control(DefaultCommand("fade out")), NodeKinds.Control),
        (End(), NodeKinds.End),
    ];

    // Matching on a class hierarchy compiles with a discard arm, so a seventh node kind would fall
    // through it in silence. Reading the registrations turns that into a failing test naming the
    // kind nobody accounted for.
    [Fact]
    public void Of_AccountsForEveryNodeKindTheFormatDeclares()
    {
        var registered = typeof(Node)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(registration => registration.DerivedType.Name)
            .Order();

        Assert.Equal(registered, _everyKind.Select(kind => kind.Node.GetType().Name).Order());
    }

    [Fact]
    public void Of_SaysSomethingForEveryNodeKind()
    {
        Assert.All(
            _everyKind,
            kind => Assert.False(
                string.IsNullOrWhiteSpace(PlaybookNodeSummary.Of(kind.Node, Speakers("Alice"))),
                $"A {kind.Kind} node produced no summary."));
    }

    [Fact]
    public void Of_ASummaryThatFitsTheCap_IsLeftWhole()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 20));

        Assert.Equal(
            $"Alice: {words}", PlaybookNodeSummary.Of(Line(words), Speakers("Alice")));
    }

    // One enormous paragraph would otherwise travel in the payload and then wrap a single row into
    // a wall of text, so a long summary is cut — between words, so none is left half-written.
    [Fact]
    public void Of_ASummaryLongerThanTheCap_IsCutBetweenWordsAndTrailsAnEllipsis()
    {
        var summary = PlaybookNodeSummary.Of(
            Line(string.Join(" ", Enumerable.Repeat("word", 200))), Speakers("Alice"));

        Assert.EndsWith("word…", summary, StringComparison.Ordinal);
        Assert.True(summary.Length <= 201, $"The summary ran to {summary.Length} characters.");
        Assert.StartsWith("Alice: word word", summary, StringComparison.Ordinal);
    }

    // Every target stays listed in the Leads to column, so cutting the summary of a long menu
    // never costs a reader the count of ways out.
    [Fact]
    public void Of_AChoiceOfManyOptions_IsCutLikeAnyOtherSummary()
    {
        var summary = PlaybookNodeSummary.Of(
            Choice([.. Enumerable.Range(0, 40).Select(n => Option($"Take the {n} road"))]),
            Speakers());

        Assert.EndsWith("…", summary, StringComparison.Ordinal);
        Assert.True(summary.Length <= 201, $"The summary ran to {summary.Length} characters.");
    }
}
