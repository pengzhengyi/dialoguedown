using DialogueDown.Configuration;
using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Validation;

namespace DialogueDown.Compilation;

/// <summary>
/// The default <see cref="IScriptCompiler"/>: it drives the stages — parse, transpile, desugar,
/// validate, analyze — in order, reporting each through a <see cref="CompilationSession"/> that
/// owns the sink and the <see cref="CompilationMode"/> flow policy, and assembles their artifacts
/// into a <see cref="CompilationResult"/>. The compiler stays a plain phase driver: the session
/// decides which sink to report through and whether to stop at a stage boundary.
/// </summary>
internal sealed class ScriptCompiler : IScriptCompiler
{
    private readonly IMarkdownParser _parser;
    private readonly IScriptTranspiler _transpiler;
    private readonly IScriptDesugarer _desugarer;
    private readonly IStructuralValidator _validator;
    private readonly ISemanticAnalyzer _analyzer;
    private readonly CompilationMode _mode;

    internal ScriptCompiler(
        IMarkdownParser parser,
        IScriptTranspiler transpiler,
        IScriptDesugarer desugarer,
        IStructuralValidator validator,
        ISemanticAnalyzer analyzer,
        CompilationMode mode)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(transpiler);
        ArgumentNullException.ThrowIfNull(desugarer);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(analyzer);
        _parser = parser;
        _transpiler = transpiler;
        _desugarer = desugarer;
        _validator = validator;
        _analyzer = analyzer;
        _mode = mode;
    }

    /// <inheritdoc />
    public CompilationResult Compile(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var session = CompilationSession.Start(source, _mode);

        var markdown = _parser.Parse(source);
        var script = _transpiler.Transpile(markdown, session.Context);

        // The transpiler is the only stage that reports errors before analysis, so a stage-boundary
        // compile halts here rather than analyzing material its errors made unreliable — the result
        // is partial. A future desugar producer would add one more checkpoint below.
        if (session.ShouldHalt)
        {
            return CompilationFailure.AtTranspile(source, markdown, script, session.Diagnostics);
        }

        var desugared = _desugarer.Desugar(script, session.Context);
        _validator.Validate(desugared, session.Context.Diagnostics);
        var semantics = _analyzer.Analyze(desugared, session.Context);

        // TODO(graph-build): build the flow graph from the semantic model here as that stage
        // lands; it adds one more artifact to a success, and an erroring compile becomes a
        // failure at analysis rather than a success without a graph.
        return new CompilationSuccess(
            source, markdown, script, desugared, semantics, session.Diagnostics);
    }
}
