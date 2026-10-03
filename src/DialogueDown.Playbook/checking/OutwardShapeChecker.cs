using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a node whose ways out are the wrong shape for its kind.
/// </summary>
/// <remarks>
/// <para>
/// Each node kind takes one kind of arm — a divert for a line or a control, an option for a
/// choice, a random option for a random choice, a branch arm for a branch — within a count given
/// by <see cref="NodeShape.For"/>. The compiler always writes nodes of this shape; a hand-edited
/// or tool-written playbook may carry a line with two ways forward, or a choice whose every option
/// may be withheld with nowhere to fall through, and a runtime would then play whichever edge came
/// first in the array.
/// </para>
/// <para>
/// A node also needs a succession to fall through to unless it is sure to leave by an arm: the
/// node has no condition of its own and has an arm without one. A node that could reach a dead
/// end, or that carries an edge kind it cannot act on, is refused. A succession that can never be
/// taken is accepted, since it changes nothing in play.
/// </para>
/// </remarks>
public sealed class OutwardShapeChecker : IPlaybookChecker
{
    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        foreach (var node in playbook.Nodes)
        {
            CheckWaysOut(node);
        }
    }

    private static void CheckWaysOut(Node node)
    {
        // An end has no ways out: its record takes none, so an `out` list written on an end is
        // not read.
        if (node is EndNode)
        {
            return;
        }

        var shape = NodeShape.For(node);

        var arms = node.Out.Where(edge => edge is not SuccessionEdge).ToArray();
        var successionEdgeCount = node.Out.Length - arms.Length;

        RefuseForeignArm(node, shape, arms);
        RefuseArmCountOutOfRange(node, shape, arms);
        RefuseBadFallThrough(node, shape, arms, successionEdgeCount);
    }

    private static void RefuseForeignArm(Node node, NodeShape shape, Edge[] arms)
    {
        if (arms.FirstOrDefault(edge => edge.GetType() != shape.ArmEdge) is { } foreign)
        {
            Refuse(
                $"Node {node.Id}, a {shape.Kind}, carries a '{NameOf(foreign.GetType())}' way out; "
                    + $"its arms are '{NameOf(shape.ArmEdge)}' edges.");
        }
    }

    private static void RefuseArmCountOutOfRange(Node node, NodeShape shape, Edge[] arms)
    {
        if (arms.Length < shape.MinArms)
        {
            Refuse(
                $"Node {node.Id}, a {shape.Kind}, carries {arms.Length} arms; "
                    + $"it needs at least {shape.MinArms}.");
        }

        if (arms.Length > shape.MaxArms)
        {
            Refuse(
                $"Node {node.Id}, a {shape.Kind}, carries {arms.Length} '{NameOf(shape.ArmEdge)}' edges; "
                    + $"it takes at most {shape.MaxArms}.");
        }
    }

    private static void RefuseBadFallThrough(
        Node node, NodeShape shape, Edge[] arms, int successionEdgeCount)
    {
        switch (successionEdgeCount)
        {
            case > 1:
                Refuse(
                    $"Node {node.Id} carries {successionEdgeCount} succession edges; "
                        + "a node falls through at most one way.");
                break;

            // A conditional node may be skipped, so it needs a succession to land on.
            case 0 when shape.NodeIsConditional:
                Refuse(CannotLeave(node, shape));
                break;

            // Every arm has a condition, or there are no arms, so none may be taken.
            case 0 when arms.All(edge => IsGated(edge)):
                Refuse(CannotLeave(node, shape));
                break;
        }
    }

    private static bool IsGated(Edge edge) => edge switch
    {
        DivertEdge divert => divert.Condition is not null,
        OptionEdge option => option.Condition is not null,
        RandomOptionEdge option => option.Condition is not null,
        BranchEdge branch => branch.Condition is not null,
        _ => false,
    };

    private static string CannotLeave(Node node, NodeShape shape) =>
        $"Node {node.Id}, a {shape.Kind}, can lead nowhere: nothing guarantees a way out, "
            + "and it has no succession to fall through to.";

    private static string NameOf(Type edgeKind) =>
        edgeKind == typeof(SuccessionEdge) ? EdgeKinds.Succession
        : edgeKind == typeof(OptionEdge) ? EdgeKinds.Option
        : edgeKind == typeof(RandomOptionEdge) ? EdgeKinds.RandomOption
        : edgeKind == typeof(BranchEdge) ? EdgeKinds.Branch
        : edgeKind == typeof(DivertEdge) ? EdgeKinds.Divert
        : edgeKind.Name;

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Refuse(string message) => throw new InvalidPlaybookException(message);
}
