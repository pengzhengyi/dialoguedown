using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Diagnostics;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.CompilationAssert;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Laws over generated labels that hold game calls, in every placement a label can take.
/// </summary>
/// <remarks>
/// Each expectation comes from the model <see cref="LabelGen"/> returns with its script, never from
/// the compiler's own reading of it. <see cref="LabelGenCoverageTests"/> checks that every placement
/// and every kind of code span is generated.
/// </remarks>
public sealed class GameCallInLabelPropertyTests
{
    private const int Samples = 200;

    /// <summary>
    /// A label holding a command or a condition fails the compile with one error at each, and a
    /// label holding only queries compiles to a label that asks for them.
    /// </summary>
    /// <remarks>
    /// A failed compile reports <c>DLG1103</c> at each command and <c>DLG1106</c> at each
    /// condition, in the order they are written, and no other error. A successful one emits a
    /// label that asks for the model's keys in order and holds no command at any depth.
    /// </remarks>
    [Fact]
    public void ALabelsCodeSpansDecideTheCompilesOutcome() =>
        LabelGen.Script().Sample(
            script =>
            {
                var rejected = script.Calls.Where(call => call.Kind != LabelCallKind.Query).ToList();

                if (rejected.Count > 0)
                {
                    AssertFailsAtEach(script, rejected);
                }
                else
                {
                    AssertAsksForEachQuery(script);
                }
            },
            iter: Samples);

    /// <summary>
    /// Wrapping speech in a link's brackets changes how its words are shown, not what they ask.
    /// </summary>
    /// <remarks>
    /// Speech of words, emphasis, and queries, compiled bare and again as a link's label, asks for
    /// the same keys in the same order and reads as the same text. So a query in a label is filled
    /// exactly like one in speech.
    /// </remarks>
    [Fact]
    public void BracketsChangeHowWordsAreShownNotWhatTheyAsk() =>
        LabelGen.QueryLabel().Sample(
            label =>
            {
                var bare = Single<LineNode>(Playbooks.Of(Bare(label), "bare.dialogue.md")).Speech;
                var wrapped = Assert.Single(
                    Single<LineNode>(Playbooks.Of(Wrapped(label), "wrapped.dialogue.md"))
                        .Speech.OfType<LinkFragment>()).Label;

                Assert.Equal(SpeechTemplate.Keys(bare), SpeechTemplate.Keys(wrapped));
                Assert.Equal(SpeechText.Of(bare), SpeechText.Of(wrapped));
            },
            iter: Samples);

    /// <summary>A scene whose one line says <paramref name="speech"/>.</summary>
    /// <remarks>
    /// <code>
    /// # The Square
    ///
    /// Alice: dawn `"PlaceName"` *map `"Alice.Mood"`*
    /// </code>
    /// </remarks>
    private static string Bare(string speech) =>
        $"""
        # The Square

        Alice: {speech}

        """;

    /// <summary>A scene whose one line holds only a link labeled <paramref name="speech"/>.</summary>
    /// <remarks>
    /// <code>
    /// # The Square
    ///
    /// Alice: [dawn `"PlaceName"` *map `"Alice.Mood"`*](#the-square)
    /// </code>
    /// </remarks>
    private static string Wrapped(string speech) =>
        $"""
        # The Square

        Alice: [{speech}](#the-square)

        """;

    private static TNode Single<TNode>(PlaybookDocument playbook) where TNode : Node =>
        Assert.Single(playbook.Nodes.OfType<TNode>());

    private static void AssertFailsAtEach(LabelScript script, IReadOnlyList<LabelCall> rejected)
    {
        var failure = AssertFailure(Pipeline.Compile(script.Source));

        var reported = failure.Diagnostics
            .Where(diagnostic => diagnostic.IsError)
            .OrderBy(diagnostic => diagnostic.Span.Start)
            .Select(diagnostic => (diagnostic.Descriptor.Code, SourceAt(script.Source, diagnostic)));

        Assert.Equal(rejected.Select(call => (CodeFor(call.Kind), call.Written)), reported);
    }

    private static void AssertAsksForEachQuery(LabelScript script)
    {
        var label = EmittedLabel(Playbooks.Of(script.Source, "label.dialogue.md"), script.Placement);

        Assert.Equal(script.Calls.Select(call => call.Key).Distinct(), SpeechTemplate.Keys(label));
        Assert.DoesNotContain(
            Descendants(label),
            fragment => fragment is DefaultCommandFragment or CustomCommandFragment);
    }

    private static string CodeFor(LabelCallKind kind) => kind switch
    {
        LabelCallKind.Command => DiagnosticCatalog.CommandInLabel.Code,
        LabelCallKind.Condition => DiagnosticCatalog.OrphanCondition.Code,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A query is not rejected."),
    };

    // The words the placement's label compiles to: the link or image in the line's speech, or the
    // label on the edge the jump becomes.
    private static ImmutableArray<SpeechFragment> EmittedLabel(
        PlaybookDocument playbook, LabelPlacement placement) => placement switch
        {
            LabelPlacement.Link => Assert.Single(SpeechOf(playbook).OfType<LinkFragment>()).Label,
            LabelPlacement.AltText => Assert.Single(SpeechOf(playbook).OfType<ImageFragment>()).Alt,
            LabelPlacement.MenuOption => Assert.Single(EdgesOf(playbook).OfType<OptionEdge>()).Label,
            LabelPlacement.Divert => Assert.Single(
                playbook.Nodes.OfType<LineNode>().SelectMany(line => line.Out).OfType<DivertEdge>()).Label,
            _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unknown placement."),
        };

    private static IEnumerable<SpeechFragment> SpeechOf(PlaybookDocument playbook) =>
        playbook.Nodes.OfType<LineNode>().SelectMany(line => line.Speech);

    private static IEnumerable<Edge> EdgesOf(PlaybookDocument playbook) =>
        playbook.Nodes.SelectMany(node => node.Out);

    private static IEnumerable<SpeechFragment> Descendants(IEnumerable<SpeechFragment> fragments) =>
        fragments.SelectMany(fragment => fragment switch
        {
            StyledTextFragment styled => Descendants(styled.Children).Prepend(fragment),
            LinkFragment link => Descendants(link.Label).Prepend(fragment),
            ImageFragment image => Descendants(image.Alt).Prepend(fragment),
            _ => [fragment],
        });

    private static string SourceAt(string source, Diagnostic diagnostic) =>
        source.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
}
