using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a <see cref="BranchNode"/> whose arms are not in the order they are tried.
/// </summary>
/// <remarks>
/// <para>
/// A branch's arms are tried in order, and each carries an explicit <see cref="BranchEdge.Order"/>
/// because a reader is not obliged to preserve the array it was written in. The compiler emits the
/// arms in source order with the order equal to the arm's index, but the format does not say so, so
/// a hand-edited or tool-written playbook can list them out of order or put the <c>else</c> before a
/// gated arm. Two conformant readers — one that walks the array, one that sorts by <c>order</c> —
/// would then take different arms.
/// </para>
/// <para>
/// Three guards, independent and all required: at least one arm is gated (a branch is a block
/// condition, and an <c>else</c> needs one to fall back from), the arms ascend as they appear, and
/// the conditionless <c>else</c>, when present, is the last arm. The last guard refuses a second
/// conditionless arm, since at most one arm can be last. A fall-through (<c>succession</c>) is not
/// an arm, so its position is left to the outward-shape rule.
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

        // An arm count of zero is the outward-shape rule's to refuse; there is nothing to order.
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
