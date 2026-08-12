using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// The default <see cref="IScriptTranspiler"/>: it walks the Markdown block tree with the
/// <see cref="BlockBuilder"/> and wraps the result in a <see cref="ScriptDocument"/>.
/// </summary>
internal sealed class ScriptTranspiler(BlockBuilder blockBuilder) : IScriptTranspiler
{
    public ScriptDocument Transpile(MarkdownDocument document, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        return new ScriptDocument(blockBuilder.Build(document.Blocks, context.Diagnostics));
    }
}
