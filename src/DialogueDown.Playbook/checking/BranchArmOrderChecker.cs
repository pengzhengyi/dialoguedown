using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a <see cref="BranchNode"/> with no arm that has a condition, or whose else is not its last arm.
/// </summary>
/// <remarks>
/// <para>
/// A branch's arms are tried in the order they appear, and the <c>else</c> is taken whenever it is
/// reached. The compiler always writes the <c>else</c> last; a hand-edited or tool-written playbook
/// may not, and every arm after an early <c>else</c> could then never be taken.
/// </para>
/// <para>
/// Two checks, both required: at least one arm has a condition (an <c>else</c> needs something to
/// fall back from), and the <c>else</c>, which has no condition, is the last arm. The last check
/// also refuses a second arm without a condition, since only one arm can be last. A succession edge
/// is not an arm and is not checked here.
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

        // A branch with no arms has nothing to check; the outward-shape check refuses it.
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
