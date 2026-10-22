using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Diagnostics;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Laws over generated scripts about <c>DLG1116</c>'s fix, which writes one space in place of
/// spaces typed together.
/// </summary>
/// <remarks>
/// <see cref="SpacedScriptGenCoverageTests"/> checks that every kind of spacing and every piece of
/// speech is generated.
/// </remarks>
public sealed class ExtraSpacesFixPropertyTests
{
    private const int Samples = 200;

    /// <summary>
    /// A script and the same script with every <c>DLG1116</c> fix applied compile to playbooks the
    /// player reads the same.
    /// </summary>
    /// <remarks>
    /// Each speech list the playbooks carry, a line's speech and each edge's label, reads as the
    /// same plain text and holds the same commands, tags, images, and queries in the same order.
    /// The fragments may be split differently, since the fix turns spacing written as entities,
    /// each a text of its own, into a literal space within its neighbors' text.
    /// </remarks>
    [Fact]
    public void TheFixKeepsWhatThePlayerReads() =>
        SpacedScriptGen.Script().Sample(
            source => Assert.Equal(Read(source), Read(WithEveryFix(source))),
            iter: Samples,
            print: source => source);

    /// <summary>A script with every <c>DLG1116</c> fix applied has no extra spaces left to note.</summary>
    [Fact]
    public void FixingEveryNoteOnceLeavesNothingToNote() =>
        SpacedScriptGen.Script().Sample(
            source => Assert.Empty(Notes(WithEveryFix(source))),
            iter: Samples,
            print: source => source);

    private static IReadOnlyList<Diagnostic> Notes(string source) =>
        [.. Pipeline.Compile(source).Diagnostics.Where(
            diagnostic => diagnostic.Descriptor == DiagnosticCatalog.ExtraSpaces)];

    // The source with each note's preferred fix applied, from the end so the spans still to apply
    // stay where they are.
    private static string WithEveryFix(string source) =>
        Notes(source)
            .SelectMany(note => note.Fixes[0].Edits)
            .OrderByDescending(edit => edit.Span.Start)
            .Aggregate(source, (text, edit) =>
                string.Concat(text[..edit.Span.Start], edit.NewText, text[edit.Span.End..]));

    // Each speech list the playbook carries, in node order, as its plain text and then the
    // commands, tags, images, and queries within it.
    private static IEnumerable<string> Read(string source) =>
        Playbooks.Of(source, "spaced.dialogue.md").Nodes
            .SelectMany(SpeechListsOf)
            .Select(speech => $"{SpeechText.Of(speech)} | {string.Join(" ", Unseen(speech))}");

    private static IEnumerable<ImmutableArray<SpeechFragment>> SpeechListsOf(Node node) =>
        (node is LineNode line ? [line.Speech] : Enumerable.Empty<ImmutableArray<SpeechFragment>>())
            .Concat(node.Out.Select(edge => edge switch
            {
                OptionEdge option => option.Label,
                DivertEdge divert => divert.Label,
                _ => [],
            }));

    // Everything in the speech that is not words, in order, inside styled text, links, and alt
    // text too.
    private static IEnumerable<string> Unseen(IEnumerable<SpeechFragment> speech) =>
        SpeechFragments.All(speech).SelectMany(Unseen);

    // What one fragment is that is not words: where a link or image points, a command, a tag, or
    // a query.
    private static IEnumerable<string> Unseen(SpeechFragment fragment) =>
        fragment switch
        {
            LinkFragment link => [$"link {link.Target}"],
            ImageFragment image => [$"image {image.Source}"],
            DefaultCommandFragment command => [$"`({command.Action})`"],
            CustomCommandFragment command => [$"`{command.Name}({string.Join(", ", command.Args)})`"],
            TagFragment tag => [$"#{tag.Name}={tag.Value}"],
            QueryFragment query => [$"`\"{query.Key}\"`"],
            _ => [],
        };
}
