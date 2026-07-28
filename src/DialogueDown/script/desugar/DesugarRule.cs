using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Base class for a desugar rule: a rewriter whose <see cref="Apply"/> runs its rewrite over the
/// whole document. A concrete rule only overrides the rewriter hooks for the nodes it changes,
/// so it never repeats the "rewrite the document" plumbing.
/// </summary>
internal abstract class DesugarRule : DialogueAstRewriter, IDesugarRule
{
    public ScriptDocument Apply(ScriptDocument document) => Rewrite(document);
}
