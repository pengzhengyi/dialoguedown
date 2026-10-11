using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a command written inside a label: a link's text, an image's alt text, or a jump's text.
/// A label is shown as one piece, so nothing inside it has a moment at which to run, and the runner
/// would never perform the command. A query may stand in a label, since it only reads a value into
/// the words.
/// </summary>
/// <remarks>
/// The diagnostic points at the command's own code span, and its message shows the command in its
/// canonical form with its arguments, so a writer can find it in a label that holds several.
/// </remarks>
internal sealed class CommandInLabelRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } = DiagnosticCatalog.CommandInLabel;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var command in nodes.OfType<GameCall>().Where(IsCommand))
        {
            // A label never holds a line, so any label above the command means it is inside one.
            if (nodes.AncestorsOf(command).Any(IsLabel))
            {
                report(command.Span, command.Canonical());
            }
        }
    }

    // Every kind is named, so a kind added later arrives here as a failure rather than being
    // silently allowed into a label.
    private static bool IsCommand(GameCall call) => call switch
    {
        Query => false,
        DefaultCommand or CustomCommand => true,
        _ => throw new NotSupportedException(
            $"No label placement is defined for {call.GetType().Name}."),
    };

    private static bool IsLabel(ScriptNode node) => node is Link or Image or Jump;
}
