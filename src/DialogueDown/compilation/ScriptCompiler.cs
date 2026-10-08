using DialogueDown.Configuration;
using DialogueDown.Graph;
using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Validation;

namespace DialogueDown.Compilation;

/// <summary>
/// The default <see cref="IScriptCompiler"/>: it runs the stages in order (parse, transpile,
/// desugar, validate, analyze, build the graph) and assembles their artifacts into a
/// <see cref="CompilationResult"/>. A <see cref="CompilationSession"/> chooses, from the
/// <see cref="CompilationMode"/>, the sink each stage reports through and whether to stop at a
/// stage boundary.
/// </summary>
internal sealed class ScriptCompiler : IScriptCompiler
{
    private readonly IMarkdownParser _parser;
    private readonly IScriptTranspiler _transpiler;
    private readonly IScriptDesugarer _desugarer;
    private readonly IStructuralValidator _validator;
    private readonly ISemanticAnalyzer _analyzer;
    private readonly IDialogueGraphBuilder _graphBuilder;
    private readonly CompilationMode _mode;

    internal ScriptCompiler(
        IMarkdownParser parser,
        IScriptTranspiler transpiler,
        IScriptDesugarer desugarer,
        IStructuralValidator validator,
        ISemanticAnalyzer analyzer,
        IDialogueGraphBuilder graphBuilder,
        CompilationMode mode)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(transpiler);
        ArgumentNullException.ThrowIfNull(desugarer);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(graphBuilder);
        _parser = parser;
        _transpiler = transpiler;
        _desugarer = desugarer;
        _validator = validator;
        _analyzer = analyzer;
        _graphBuilder = graphBuilder;
        _mode = mode;
    }

    /// <inheritdoc />
    public CompilationResult Compile(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var session = CompilationSession.Start(source, _mode);

        var markdown = _parser.Parse(source, session.Context);
        var script = _transpiler.Transpile(markdown, session.Context);

        // A stage-boundary compile stops here after an error from parsing or transpiling, since
        // the later stages would read material that error made unreliable.
        if (session.ShouldHalt)
        {
            return CompilationFailure.AtTranspile(source, markdown, script, session.Diagnostics);
        }

        var desugared = _desugarer.Desugar(script, session.Context);
        _validator.Validate(desugared, session.Context.Diagnostics);
        var semantics = _analyzer.Analyze(desugared, session.Context);

        // An error means the recovered model does not describe what the writer wrote, so the
        // compile failed. The failure keeps everything it reached, so a tool can still show a
        // broken script.
        if (session.HasErrors)
        {
            return CompilationFailure.AtAnalysis(
                source, markdown, script, desugared, semantics, session.Diagnostics);
        }

        var graph = _graphBuilder.Build(semantics, session.Context);

        return new CompilationSuccess(
            source, markdown, script, desugared, semantics, graph, session.Diagnostics);
    }
}
