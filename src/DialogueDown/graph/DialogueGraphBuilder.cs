using DialogueDown.Diagnostics;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph;

/// <inheritdoc />
internal sealed class DialogueGraphBuilder : IDialogueGraphBuilder
{
    public DialogueGraph Build(SemanticModel model, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        var nodes = new List<DialogueNode>();
        var end = new EndNode(new NodeId(nodes.Count));
        nodes.Add(end);

        // An empty document runs straight to the End sentinel.
        return new DialogueGraph(nodes, entry: end.Id, end: end.Id);
    }
}
