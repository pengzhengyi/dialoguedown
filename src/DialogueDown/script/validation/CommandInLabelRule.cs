using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a command written inside a link's text. A link is shown as one piece, so nothing inside
/// it has a moment at which to run, and the runner would never perform the command. A query may
/// stand in a link's text, since it only reads a value into the words.
/// </summary>
/// <remarks>
/// The diagnostic points at the command's own code span, and its message shows the command in its
/// canonical form with its arguments, so a writer can find it in a link that holds several.
/// </remarks>
internal sealed class CommandInLabelRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } = DiagnosticCatalog.CommandInLabel;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var command in nodes.OfType<GameCall>().Where(IsCommand))
        {
            // A link never holds a line, so any link above the command means it is inside one.
            if (nodes.AncestorsOf(command).Any(IsLink))
            {
                report(command.Span, command.Canonical());
            }
        }
    }

    // Every kind is named, so a kind added later arrives here as a failure rather than being
    // silently allowed into a link.
    private static bool IsCommand(GameCall call) => call switch
    {
        Query => false,
        DefaultCommand or CustomCommand => true,
        _ => throw new NotSupportedException(
            $"No label placement is defined for {call.GetType().Name}."),
    };

    private static bool IsLink(ScriptNode node) => node is Link;
}
