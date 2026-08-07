using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Diagnostics.Errors;
using DialogueDown.Graph;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Validation;
using DialogueDown.Tests.Support;
using NSubstitute;
using NSubstitute.Extensions;
using static DialogueDown.Tests.Support.CompilationAssert;

namespace DialogueDown.Tests.Compilation;

public sealed class ScriptCompilerTests
{
    [Fact]
    public void Compile_RunsStagesInOrderThreadingSource_AndAssemblesResult()
    {
        var source = "Alice: hi";
        var markdown = new MarkdownDocument([]);
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);
        var semantics = SemanticModelFactory.Minimal(desugared);

        var parser = Substitute<IMarkdownParser, MarkdownDocument>(markdown);
        var transpiler = Substitute<IScriptTranspiler, ScriptDocument>(script);
        var desugarer = Substitute<IScriptDesugarer, DesugaredScriptDocument>(desugared);
        var validator = NSubstitute.Substitute.For<IStructuralValidator>();
        var analyzer = Substitute<ISemanticAnalyzer, SemanticModel>(semantics);
        var graph = DialogueGraphFactory.EmptyGraph();
        var graphBuilder = Substitute<IDialogueGraphBuilder, DialogueGraph>(graph);

        var result = AssertSuccess(new ScriptCompiler(
                parser, transpiler, desugarer, validator, analyzer, graphBuilder, CompilationMode.BestEffort)
            .Compile(source));

        Assert.Equal(source, result.Source);
        Assert.Same(markdown, result.Markdown);
        Assert.Same(script, result.Script);
        Assert.Same(desugared, result.Desugared);
        Assert.Same(semantics, result.Semantics);
        Assert.Same(graph, result.Graph);
        Received.InOrder(() =>
        {
            parser.Parse(source);
            transpiler.Transpile(Arg.Is(markdown), Arg.Is<DiagnosticsContext>(c => c!.Source == source));
            desugarer.Desugar(Arg.Is(script), Arg.Is<DiagnosticsContext>(c => c!.Source == source));
            validator.Validate(Arg.Is(desugared), Arg.Any<IDiagnosticSink>());
            analyzer.Analyze(Arg.Is(desugared), Arg.Is<DiagnosticsContext>(c => c!.Source == source));
            graphBuilder.Build(Arg.Is(semantics), Arg.Is<DiagnosticsContext>(c => c!.Source == source));
        });
    }

    [Fact]
    public void Compile_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() => Compiler(out _, out _).Compile(null!));

    [Fact]
    public void Compile_NoStageReports_HasEmptyDiagnosticsAndNoErrors()
    {
        var result = Compiler(out _, out _).Compile("Alice: hi");

        Assert.Empty(result.Diagnostics);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Compile_AStageReportsAnError_SurfacesItOnTheResultAndFlipsHasErrors()
    {
        var compiler = Compiler(out _, out var analyzer);
        var diagnostic = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error);
        analyzer.When(a => a.Analyze(Arg.Any<DesugaredScriptDocument>(), Arg.Any<DiagnosticsContext>()))
            .Do(call => call.Arg<DiagnosticsContext>()!.Diagnostics.Report(diagnostic));

        var result = compiler.Compile("Alice: hi");

        Assert.Contains(diagnostic, result.Diagnostics);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Compile_FailFast_ATranspileError_ThrowsCarryingTheDiagnostic()
    {
        var error = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error);
        var compiler = CompilerWhoseTranspilerReports(CompilationMode.FailFast, error);

        var thrown = Assert.Throws<DiagnosticException>(() => compiler.Compile("Alice: hi"));

        Assert.Same(error, thrown.Diagnostic);
    }

    [Fact]
    public void Compile_StageBoundary_ATranspileError_HaltsWithAPartialResult()
    {
        var error = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error);
        var compiler = CompilerWhoseTranspilerReports(CompilationMode.StageBoundary, error);

        var result = compiler.Compile("Alice: hi");

        AssertFailure(result);
        Assert.True(result.HasErrors);
        Assert.Contains(error, result.Diagnostics);
    }

    [Fact]
    public void Compile_BestEffort_ATranspileError_RunsEveryStageButStillFails()
    {
        var error = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error);
        var compiler = CompilerWhoseTranspilerReports(CompilationMode.BestEffort, error);

        // Best-effort never halts, so analysis ran and its artifacts are here — reaching every
        // stage is not the same as succeeding.
        var result = AssertFailure(compiler.Compile("Alice: hi"));

        Assert.NotNull(result.Desugared);
        Assert.NotNull(result.Semantics);
        Assert.True(result.HasErrors);
        Assert.Contains(error, result.Diagnostics);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Constructor_NullDependency_Throws(int nullIndex)
    {
        var parser = nullIndex == 0 ? null! : Substitute<IMarkdownParser, MarkdownDocument>();
        var transpiler = nullIndex == 1 ? null! : Substitute<IScriptTranspiler, ScriptDocument>();
        var desugarer = nullIndex == 2 ? null! : Substitute<IScriptDesugarer, DesugaredScriptDocument>();
        var validator = nullIndex == 3 ? null! : NSubstitute.Substitute.For<IStructuralValidator>();
        var analyzer = nullIndex == 4 ? null! : Substitute<ISemanticAnalyzer, SemanticModel>();
        var graphBuilder = nullIndex == 5 ? null! : Substitute<IDialogueGraphBuilder, DialogueGraph>();

        Assert.Throws<ArgumentNullException>(() => new ScriptCompiler(
            parser, transpiler, desugarer, validator, analyzer, graphBuilder, CompilationMode.BestEffort));
    }

    // Builds a compiler whose stages are substitutes that yield empty artifacts, so a test can focus
    // on the facade. Outs the transpiler and analyzer so a test can install a spy on either, and
    // takes the mode (best-effort by default, so every stage runs).
    private static ScriptCompiler Compiler(
        out IScriptTranspiler transpiler,
        out ISemanticAnalyzer analyzer,
        CompilationMode mode = CompilationMode.BestEffort)
    {
        var desugared = new DesugaredScriptDocument(new ScriptDocument([]));
        transpiler = Substitute<IScriptTranspiler, ScriptDocument>(new ScriptDocument([]));
        analyzer = Substitute<ISemanticAnalyzer, SemanticModel>(SemanticModelFactory.Minimal(desugared));
        return new ScriptCompiler(
            Substitute<IMarkdownParser, MarkdownDocument>(new MarkdownDocument([])),
            transpiler,
            Substitute<IScriptDesugarer, DesugaredScriptDocument>(desugared),
            NSubstitute.Substitute.For<IStructuralValidator>(),
            analyzer,
            Substitute<IDialogueGraphBuilder, DialogueGraph>(DialogueGraphFactory.EmptyGraph()),
            mode);
    }

    // Builds a compiler in the given mode whose transpiler reports the diagnostic while transpiling,
    // so a test can exercise how the mode reacts to an error raised before analysis.
    private static ScriptCompiler CompilerWhoseTranspilerReports(CompilationMode mode, Diagnostic diagnostic)
    {
        var compiler = Compiler(out var transpiler, out _, mode);
        transpiler.When(t => t.Transpile(Arg.Any<MarkdownDocument>(), Arg.Any<DiagnosticsContext>()))
            .Do(call => call.Arg<DiagnosticsContext>()!.Diagnostics.Report(diagnostic));
        return compiler;
    }

    // Builds a stage substitute and, when a value is given, has every member that returns
    // TReturn return it — so a test reads "a parser that yields this markdown" in one line.
    private static TTarget Substitute<TTarget, TReturn>(TReturn? value = null)
        where TTarget : class
        where TReturn : class
    {
        var substitute = NSubstitute.Substitute.For<TTarget>();
        if (value is not null)
        {
            substitute.ReturnsForAll(value);
        }

        return substitute;
    }
}
