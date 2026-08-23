using DialogueDown.Playbook.Nodes;
using GraphNodes = DialogueDown.Graph.Nodes;

namespace DialogueDown.Emission;

/// <summary>
/// Writes the steps of a playthrough.
/// </summary>
/// <remarks>
/// A node keeps only what playing it needs. The source span it was lowered from is left behind:
/// a span addresses text that a runtime does not have and cannot be given, since a playbook is
/// written to be carried elsewhere and interpreted there. Source positions serve diagnostics,
/// and diagnostics stay with the compiler.
/// </remarks>
internal static class NodeMapping
{
    /// <summary>Writes one node and the ways out of it.</summary>
    /// <param name="node">The node to write.</param>
    /// <param name="nodes">Where each node will sit.</param>
    /// <param name="speakers">Where each speaker sits.</param>
    /// <returns>The same node as a playbook carries it.</returns>
    public static Node Write(
        GraphNodes.DialogueNode node, NodeNumbering nodes, SpeakerNumbering speakers)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(speakers);

        var id = nodes.Position(node.Id);
        var edges = EdgeMapping.Write(node.Out, nodes);

        return node switch
        {
            GraphNodes.LineNode line => new LineNode(
                id,
                speakers.Position(line.Speaker),
                SpeechMapping.Write(line.Speech),
                ConditionMapping.Write(line.Condition),
                edges),

            GraphNodes.ChoiceNode choice => new ChoiceNode(id, choice.IsOrdered, edges),
            GraphNodes.RandomChoiceNode => new RandomChoiceNode(id, edges),
            GraphNodes.BranchNode => new BranchNode(id, edges),

            GraphNodes.ControlNode control => new ControlNode(
                id,
                EffectMapping.Write(control.Effects),
                ConditionMapping.Write(control.Condition),
                edges),

            // An end leads nowhere, so it has no ways out to write.
            GraphNodes.EndNode => new EndNode(id),

            _ => throw new NotSupportedException(
                $"No playbook node is defined for {node.GetType().Name}."),
        };
    }
}
