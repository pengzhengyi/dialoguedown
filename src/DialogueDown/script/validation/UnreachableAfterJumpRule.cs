using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Warns when a line has content after a jump. A jump does not return, so anything following it on
/// the same line — trailing speech, or a second jump — can never play. Purely structural: it looks
/// at the fragments directly in each <see cref="Line"/>'s speech, so a jump nested inside a jump's
/// own label does not count.
/// </summary>
internal sealed class UnreachableAfterJumpRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.UnreachableContentAfterJump;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var line in nodes.OfType<Line>())
        {
            ReportUnreachable(line, report);
        }
    }

    private static void ReportUnreachable(Line line, Reporter report)
    {
        var speech = line.Speech;
        var firstJump = FirstJumpIndex(speech);
        if (firstJump < 0)
        {
            return;
        }

        // Blank text after the jump is just padding, not content; a soft line break already ended
        // the jump upstream, so only real fragments left on the line count as unreachable.
        var unreachable = speech.Skip(firstJump + 1).Where(fragment => !fragment.IsBlank()).ToList();
        if (unreachable.Count == 0)
        {
            return;
        }

        report(SourceSpan.Covering(unreachable[0].Span, unreachable[^1].Span));
    }

    private static int FirstJumpIndex(IReadOnlyList<InlineFragment> speech)
    {
        for (var index = 0; index < speech.Count; index++)
        {
            if (speech[index] is Jump)
            {
                return index;
            }
        }

        return -1;
    }
}
