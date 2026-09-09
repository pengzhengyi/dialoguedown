using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a node whose ways out are the wrong shape for its kind.
/// </summary>
/// <remarks>
/// <para>
/// Each node kind offers one kind of arm — a divert for a line or a control, an option for a
/// choice, a random-option for a random choice, a branch for a branch — and either always has a
/// way on or does not. The compiler only ever emits nodes that hold to this, but the format does
/// not say so, so a hand-edited or tool-written playbook can carry a line with two ways forward,
/// an option out of an end, or a choice that can withhold every arm with nowhere to fall through.
/// A runtime would then play whichever edge came first in the array.
/// </para>
/// <para>
/// The static half of the rule — which arm kind, and how many — is the table in
/// <see cref="NodeShape.For"/>. The rest is read from the node itself: a fall-through is needed unless
/// some arm always applies, which means an unconditional arm on a node that is not itself
/// conditional. A node that could reach a dead end, or one carrying an edge kind it cannot act
/// on, is refused. A succession that can never run is accepted: it plays no differently.
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
        // An end carries no ways out by construction — its record takes none — so there is
        // nothing here to check. A hand-written end that leads somewhere is the schema's
        // `maxItems: 0` to catch.
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

            // Every arm can be withheld — the no-arms case included, vacuously — so likewise.
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
