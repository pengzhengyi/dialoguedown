using DialogueDown.Diagnostics;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.CompilationAssert;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.SpeechAssert;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// A game call written in a label reaches a real compile: a query is carried into the playbook
/// wherever the label lands, and a command or a condition fails the compile at its code span.
/// </summary>
public sealed class GameCallInLabelCompilationTests
{
    [Fact]
    public void Compile_AQueryInAMenuOptionsLabel_ReachesTheOptionsLabel()
    {
        const string Script = """
            # Square

            Alice: Who should come along?

            - => [Ask `"CompanionName"` to join](#join)

            # Join

            Alice: Welcome aboard.
            """;

        var option = Assert.IsType<OptionEdge>(
            Assert.Single(Single<ChoiceNode>(Playbooks.Of(Script, "square.dialogue.md").Nodes).Out));

        Assert.Collection(
            option.Label,
            fragment => AssertSays(fragment, "Ask "),
            fragment => AssertQueries(fragment, "CompanionName"),
            fragment => AssertSays(fragment, " to join"));
    }

    [Fact]
    public void Compile_AQueryInADivertsLabel_ReachesTheDivertsLabel()
    {
        const string Script = """
            # Market

            Alice: The inn is this way. => [Follow Alice to `"InnName"`](#inn)

            # Inn

            Alice: Here we are.
            """;

        var line = First<LineNode>(Playbooks.Of(Script, "market.dialogue.md").Nodes);
        var divert = Assert.IsType<DivertEdge>(Assert.Single(line.Out));

        Assert.Collection(
            divert.Label,
            fragment => AssertSays(fragment, "Follow Alice to "),
            fragment => AssertQueries(fragment, "InnName"));
    }

    [Fact]
    public void Compile_ACommandInALabel_FailsWithAnErrorAtTheCommand()
    {
        const string Script = """
            # Hall

            - => [Leave `SlamDoor()`](#exit)

            # Exit

            Alice: Gone.
            """;

        var failure = AssertFailure(Pipeline.Compile(Script));

        var diagnostic = AssertReported(failure.Diagnostics, DiagnosticCatalog.CommandInLabel);
        Assert.Equal("`SlamDoor()`", SourceAt(Script, diagnostic));
    }

    [Fact]
    public void Compile_AConditionInALabel_FailsWithAnErrorAtTheCondition()
    {
        const string Script = """
            # Hall

            Alice: [Only if `"Met"?`](#inn)

            # Inn

            Alice: Here we are.
            """;

        var failure = AssertFailure(Pipeline.Compile(Script));

        var diagnostic = AssertReported(failure.Diagnostics, DiagnosticCatalog.OrphanCondition);
        Assert.Equal("`\"Met\"?`", SourceAt(Script, diagnostic));
    }

    [Fact]
    public void Compile_AQueryInASceneHeadingsLink_LeavesTheAnchor()
    {
        const string Script = """
            # Visit [the `"Inn"` gate](#gate)

            Alice: Hi.

            # Gate

            Alice: Here we are.
            """;

        var playbook = Playbooks.Of(Script, "visit.dialogue.md");

        Assert.Contains("visit-the--gate", playbook.Anchors.Keys);
    }

    private static TNode Single<TNode>(IEnumerable<Node> nodes) where TNode : Node =>
        Assert.Single(nodes.OfType<TNode>());

    private static TNode First<TNode>(IEnumerable<Node> nodes) where TNode : Node =>
        nodes.OfType<TNode>().First();

    private static string SourceAt(string source, Diagnostic diagnostic) =>
        source.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
}
