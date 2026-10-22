using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Notes extra spaces: two or more collapsible spaces written together in what the player reads.
/// The playbook carries them as one space, as a Markdown preview shows them, so a writer who meant
/// the width learns that it does not show.
/// </summary>
/// <remarks>
/// <para>
/// The rule reads the words each node shows the player, a line's words or a jump's label (see
/// <see cref="ScriptNodeExtensions.Shown"/>), and every text within them, down into styled text,
/// link labels, and alt text (see <see cref="ScriptNodeExtensions.Texts"/>).
/// </para>
/// <para>
/// Spaces are typed together when each starts where the one before it ends in the source, even
/// across texts, so spacing split across entities is one place:
/// </para>
/// <code>
/// Alice: a&amp;#32;&amp;#32;b    texts "a", " ", " ", "b"    one note over &amp;#32;&amp;#32;
/// </code>
/// <para>
/// Anything written between two spaces sets them apart, such as a command or the edge of a link. A
/// space on each side of a command still becomes one space in the playbook, but the writer typed
/// one space in each place rather than two together, so it is not reported.
/// </para>
/// </remarks>
internal sealed class ExtraSpacesRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } = DiagnosticCatalog.ExtraSpaces;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var spaces in nodes.OfType<ScriptNode>().SelectMany(ExtraSpacesIn))
        {
            report(spaces, fixes: [UseOneSpace(spaces)]);
        }
    }

    // Each place in the words a node shows where two or more spaces are typed together, as one
    // span from the first space to the last. Anything written between two spaces, such as a
    // command or the edge of a link, sets them apart.
    private static IEnumerable<SourceSpan> ExtraSpacesIn(ScriptNode node) =>
        ContiguousRuns(node.Shown().Texts().SelectMany(SpacesIn))
            .Where(run => run.Count > 1)
            .Select(run => SourceSpan.Covering(run[0], run[^1]));

    // The preferred fix: one space in place of all of them, which reads the same to the player.
    private static DiagnosticFix UseOneSpace(SourceSpan spaces) =>
        new("Use one space", [new DiagnosticEdit(spaces, " ")]);

    // Each collapsible space in the text, at the span it is written over. A text's characters
    // stand one for one in its span, unless it was decoded from source of another length, such as
    // the entity &#32;, one character written as five; such a text is one space if all of it is.
    private static IEnumerable<SourceSpan> SpacesIn(Text text)
    {
        if (text.Content.Length != text.Span.Length)
        {
            return text.Content.All(CharExtensions.IsCollapsibleSpace) ? [text.Span] : [];
        }

        return Enumerable.Range(0, text.Content.Length)
            .Where(offset => text.Content[offset].IsCollapsibleSpace())
            .Select(offset => new SourceSpan(text.Span.Start + offset, 1));
    }

    // Groups the spans, in order, into sequences in which each span starts where the one before
    // it ends.
    private static IEnumerable<IReadOnlyList<SourceSpan>> ContiguousRuns(IEnumerable<SourceSpan> spans)
    {
        List<SourceSpan> run = [];
        foreach (var span in spans)
        {
            if (run.Count > 0 && span.Start != run[^1].End)
            {
                yield return run;
                run = [];
            }

            run.Add(span);
        }

        if (run.Count > 0)
        {
            yield return run;
        }
    }
}
