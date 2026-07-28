using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Assembles a jump and the pieces around it into one <see cref="Jump"/> in every fragment
/// sequence, delegating the per-sequence fold to <see cref="JumpAssembler"/>.
/// </summary>
internal sealed class JumpAssemblyRule : DesugarRule
{
    protected override IReadOnlyList<InlineFragment> RewriteFragments(
        IReadOnlyList<InlineFragment> fragments) =>
        JumpAssembler.Assemble(base.RewriteFragments(fragments));
}
