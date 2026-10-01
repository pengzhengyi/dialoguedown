using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// A <see cref="ScriptDocument"/> that has been desugared — a distinct pipeline-stage type
/// so a later stage (semantic analysis) can take only a desugared tree, and Desugar cannot
/// be skipped. It is a thin wrapper that exposes the document's <see cref="Body"/>. The
/// type records only that Desugar ran; the desugar rules, not the type, guarantee that no
/// <c>JumpIndicator</c> remains and every line has a speaker.
/// </summary>
internal sealed record DesugaredScriptDocument(ScriptDocument Document)
{
    public IReadOnlyList<ScriptBlock> Body => Document.Body;
}
