using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a <see cref="BranchNode"/> whose arms are not in the order they are tried.
/// </summary>
/// <remarks>
/// <para>
/// A runtime may walk the arms in array order or sort them by <see cref="BranchEdge.Order"/>, so
/// the two must agree. The compiler always writes them that way; a hand-edited or tool-written
/// playbook may not, and two runtimes would then take different arms.
/// </para>
/// <para>
/// Three checks, all required: at least one arm has a condition (an <c>else</c> needs something to
/// fall back from), each arm's order is greater than the one before it, and the <c>else</c>, which
/// has no condition, is the last arm. The last check also refuses a second arm without a
/// condition, since only one arm can be last. A succession edge is not an arm and is not checked
/// here.
/// </para>
/// </remarks>
public sealed class BranchArmOrderChecker : IPlaybookChecker
{
    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        foreach (var node in playbook.Nodes)
        {
            if (node is BranchNode branch)
            {
                CheckArmOrder(branch);
            }
        }
    }

    private static void CheckArmOrder(BranchNode branch)
    {
        var arms = branch.Out.OfType<BranchEdge>().ToArray();

        // A branch with no arms has nothing to order; the outward-shape check refuses it.
        if (arms.Length == 0)
        {
            return;
        }

        RefuseNoGatedArm(branch, arms);
        RefuseOutOfOrder(branch, arms);
        RefuseElseNotLast(branch, arms);
    }

    private static void RefuseNoGatedArm(BranchNode branch, BranchEdge[] arms)
    {
        if (arms.All(arm => arm.Condition is null))
        {
            Refuse(
                $"Node {branch.Id}, a branch, carries no gated arm; every arm is an else. "
                    + "A branch is a block condition, so at least one arm carries one.");
        }
    }

    private static void RefuseOutOfOrder(BranchNode branch, BranchEdge[] arms)
    {
        for (var index = 1; index < arms.Length; index++)
        {
            var previous = arms[index - 1].Order;
            var order = arms[index].Order;

            if (order < previous)
            {
                Refuse(
                    $"Node {branch.Id}, a branch, lists its arms out of order: "
                        + $"order {order} follows order {previous}.");
            }

            if (order == previous)
            {
                Refuse(
                    $"Node {branch.Id}, a branch, gives two arms the same order ({order}); "
                        + "the arms are tried one after another.");
            }
        }
    }

    private static void RefuseElseNotLast(BranchNode branch, BranchEdge[] arms)
    {
        var elseIndex = Array.FindIndex(arms, arm => arm.Condition is null);

        if (elseIndex >= 0 && elseIndex != arms.Length - 1)
        {
            Refuse(
                $"Node {branch.Id}, a branch, carries its else arm before another arm; "
                    + "the else is tried last.");
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Refuse(string message) => throw new InvalidPlaybookException(message);
}
