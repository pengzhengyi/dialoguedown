using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Assembles a jump and the pieces around it into one <see cref="Jump"/> in every fragment
/// sequence, delegating the per-sequence fold to <see cref="JumpAssembler"/>.
/// </summary>
internal sealed class JumpAssemblyRule : DesugarRule
{
    private readonly JumpAssembler _assembler;

    public JumpAssemblyRule(IDiagnosticSink diagnostics) => _assembler = new JumpAssembler(diagnostics);

    protected override IReadOnlyList<InlineFragment> RewriteFragments(
        IReadOnlyList<InlineFragment> fragments) =>
        _assembler.Assemble(base.RewriteFragments(fragments));
}
