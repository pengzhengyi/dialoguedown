using DialogueDown.Configuration;
using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Runs the real compiler stages — parse, transpile, desugar, analyze — over source, so an
/// integration test can feed a downstream stage a genuine model instead of a hand-built one.
/// The stages are stateless, so they are constructed once and shared.
/// </summary>
internal static class Pipeline
{
    private static readonly IMarkdownParser _parser = MarkdownParserFactory.MarkdownParser();
    private static readonly IScriptTranspiler _transpiler = TranspilerBuilderFactory.ScriptTranspiler();
    private static readonly IScriptDesugarer _desugarer = DesugarerFactory.ScriptDesugarer();
    private static readonly ISemanticAnalyzer _analyzer = new SemanticAnalyzer(new SemanticAnalyzerOptions([]));

    public static DesugaredScriptDocument UntilDesugared(string source)
    {
        var context = DiagnosticsContextFactory.Context(source);
        var markdown = _parser.Parse(source);
        var script = _transpiler.Transpile(markdown, context);
        return _desugarer.Desugar(script, context);
    }

    public static SemanticModel UntilAnalyzed(string source) =>
        _analyzer.Analyze(UntilDesugared(source), DiagnosticsContextFactory.Context(source));
}
