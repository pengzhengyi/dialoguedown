using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a <see cref="BranchNode"/> that has no gated arm, or whose else is not its last arm.
/// </summary>
/// <remarks>
/// <para>
/// A branch's arms are tried in the order they appear in its ways out, and the conditionless
/// <c>else</c> is taken whenever it is reached. The compiler writes the arms in source order, but a
/// hand-edited or tool-written playbook can put the <c>else</c> before a gated arm, and every arm
/// after it could then never be taken.
/// </para>
/// <para>
/// Two guards, independent and both required: at least one arm is gated (a branch is a block
/// condition, and an <c>else</c> needs one to fall back from), and the <c>else</c>, when present,
/// is the last arm. The second guard refuses a second conditionless arm, since at most one arm can
/// be last. A fall-through (<c>succession</c>) is not an arm, so its position is left to the
/// outward-shape rule.
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

        // An arm count of zero is the outward-shape rule's to refuse; there is nothing to check.
        if (arms.Length == 0)
        {
            return;
        }

        RefuseNoGatedArm(branch, arms);
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
