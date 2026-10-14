using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using static DialogueDown.Playbook.Checking.PlaybookRefusal;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a <see cref="BranchNode"/> with no branch that has a condition, or whose else is not
/// its last branch.
/// </summary>
/// <remarks>
/// <para>
/// A branch node's branches are tried in the order they appear, and the <c>else</c> is taken
/// whenever it is reached. The compiler always writes the <c>else</c> last; a hand-edited or
/// tool-written playbook may not, and every branch after an early <c>else</c> could then never be
/// taken.
/// </para>
/// <para>
/// Two checks, both required: at least one branch has a condition (an <c>else</c> needs something
/// to fall back from), and the <c>else</c>, which has no condition, is the last branch. The last
/// check also refuses a second branch without a condition, since only one branch can be last. A
/// succession edge is not a branch and is not checked here.
/// </para>
/// </remarks>
public sealed class BranchOrderChecker : IPlaybookChecker
{
    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        foreach (var node in playbook.Nodes)
        {
            if (node is BranchNode branchNode)
            {
                CheckBranchOrder(branchNode);
            }
        }
    }

    private static void CheckBranchOrder(BranchNode node)
    {
        var branches = node.Out.OfType<BranchEdge>().ToArray();

        // A branch node with no branches has nothing to check; the outward-shape check refuses it.
        if (branches.Length == 0)
        {
            return;
        }

        RefuseNoGatedBranch(node, branches);
        RefuseElseNotLast(node, branches);
    }

    private static void RefuseNoGatedBranch(BranchNode node, BranchEdge[] branches)
    {
        if (branches.All(edge => edge.Condition is null))
        {
            Refuse(
                $"Node {node.Id}, a branch, carries no gated branch; every branch is an else. "
                    + "A branch node is a block condition, so at least one branch carries a condition.");
        }
    }

    private static void RefuseElseNotLast(BranchNode node, BranchEdge[] branches)
    {
        var elseIndex = Array.FindIndex(branches, edge => edge.Condition is null);

        if (elseIndex >= 0 && elseIndex != branches.Length - 1)
        {
            Refuse(
                $"Node {node.Id}, a branch, carries its else before another branch; "
                    + "the else is tried last.");
        }
    }
}
