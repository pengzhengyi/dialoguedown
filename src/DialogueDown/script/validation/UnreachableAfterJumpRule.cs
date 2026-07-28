using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Warns when content follows a jump. A jump does not return, so anything after it on the same
/// line — trailing speech, or a second effect — can never play. Purely structural: it looks at the
/// fragments directly in each <see cref="Line"/>'s speech and each <see cref="ControlLine"/>'s
/// effects, so a jump nested inside a jump's own label does not count.
/// </summary>
internal sealed class UnreachableAfterJumpRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.UnreachableContentAfterJump;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var line in nodes.OfType<Line>())
        {
            ReportUnreachable(line.Speech, report);
        }

        foreach (var control in nodes.OfType<ControlLine>())
        {
            ReportUnreachable(control.Effects, report);
        }
    }

    private static void ReportUnreachable(IReadOnlyList<InlineFragment> fragments, Reporter report)
    {
        var firstJump = fragments.FindIndex(fragment => fragment is Jump);
        if (firstJump < 0)
        {
            return;
        }

        // Blank text after the jump is just padding; only real fragments left on the line count.
        var unreachable = fragments.Skip(firstJump + 1).Where(fragment => fragment.NonBlank()).ToList();
        if (unreachable.Count == 0)
        {
            return;
        }

        report(SourceSpan.Covering(unreachable[0].Span, unreachable[^1].Span));
    }
}
