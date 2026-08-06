using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Walks the blocks of a sequence — and of any body nested in it, a choice option's or a
/// conditional block's branch — pairing each block with
/// its <b>continuation</b>: the node control reaches once that block is done, which is the next
/// block in its own sequence, or the sequence's own continuation when it is the last. Because an
/// option's body continues where the choice itself would have, the arms of a branch weave back
/// together:
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
/// Both arms continue at <c>n5</c>, the choice's own continuation. A nested group repeats the
/// shape one level down, so its arms weave back to wherever the enclosing body continues.
/// </summary>
internal static class BlockSequence
{
    public static IEnumerable<(ScriptBlock Block, NodeId Continuation)> AllContinuations(
        IReadOnlyList<ScriptBlock> sequence, NodeId sequenceContinuation, GraphDraft draft)
    {
        for (var position = 0; position < sequence.Count; position++)
        {
            var block = sequence[position];
            var continuation = position + 1 < sequence.Count
                ? draft.IdOf(sequence[position + 1])
                : sequenceContinuation;
            yield return (block, continuation);

            foreach (var body in NestedBodies(block))
            {
                foreach (var nested in AllContinuations(body, continuation, draft))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>
    /// Where taking <paramref name="body"/> leads. A body with no content of its own plays
    /// nothing, so taking it resumes right where the block holding it would have continued.
    /// </summary>
    public static NodeId EntryOf(
        IReadOnlyList<ScriptBlock> body, NodeId continuation, GraphDraft draft) =>
        body.Count > 0 ? draft.IdOf(body[0]) : continuation;

    // The bodies a block holds, which continue where the block itself does. Both branching kinds
    // weave back the same way, so the walk does not care which kind it is descending into.
    private static IEnumerable<IReadOnlyList<ScriptBlock>> NestedBodies(ScriptBlock block) =>
        block switch
        {
            ChoiceGroup group => group.OptionBodies(),
            ControlBlock conditional => conditional.BranchBodies(),
            _ => [],
        };
}
