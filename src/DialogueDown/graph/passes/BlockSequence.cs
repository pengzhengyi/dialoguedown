using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Walks the blocks of a sequence — and of any option body nested in it — pairing each block with
/// the node control reaches once that block is done: the next block in its own sequence, or the
/// sequence's <b>continuation</b> when it is the last. Because an option's body continues where
/// the choice itself would have, the arms of a branch weave back together:
/// <code>
/// Guide: Pick.        n0
/// - Alice: Left.      n1 is the choice group; n2 is this arm's first block
///   Alice: Onward.    n3
/// - Alice: Right.     n4
/// Guide: Done.        n5
///
/// n0 ──succession──▶ n1 ──option──▶ n2 ──succession──▶ n3 ──┐
///                    └──option──▶ n4 ──succession───────────┤
///                                                           ▼
///                                         n5 ──succession──▶ End
/// </code>
/// Both arms end at <c>n5</c>, the choice's own continuation. A nested choice repeats the shape
/// one level down, so its arms weave back to wherever the enclosing body continues.
/// </summary>
internal static class BlockSequence
{
    public static IEnumerable<(ScriptBlock Block, NodeId Next)> WithSuccessors(
        IReadOnlyList<ScriptBlock> sequence, NodeId continuation, GraphDraft draft)
    {
        for (var position = 0; position < sequence.Count; position++)
        {
            var block = sequence[position];
            var next = position + 1 < sequence.Count
                ? draft.IdOf(sequence[position + 1])
                : continuation;
            yield return (block, next);

            if (block is not ChoiceGroup group)
            {
                continue;
            }

            foreach (var body in group.OptionBodies())
            {
                foreach (var nested in WithSuccessors(body, next, draft))
                {
                    yield return nested;
                }
            }
        }
    }
}
