using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.ConfigurationFactory;
using static DialogueDown.Tests.Support.DiagnosticsAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SemanticAnalyzerTests
{
    private readonly SemanticAnalyzer _analyzer = new(new SemanticAnalyzerOptions([]));

    [Fact]
    public void Analyze_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _analyzer.Analyze(null!, DiagnosticsContextFactory.Context("source")));

    [Fact]
    public void Analyze_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => _analyzer.Analyze(new DesugaredScriptDocument(new ScriptDocument([])), null!));

    [Fact]
    public void Analyze_HoldsTheDesugaredTree()
    {
        var desugared = Pipeline.UntilDesugared("Alice: Hi.");

        var model = _analyzer.Analyze(desugared, DiagnosticsContextFactory.Context("Alice: Hi."));

        Assert.Same(desugared, model.Desugared);
    }

    [Fact]
    public void Analyze_BindsSpeakers()
    {
        var model = Analyze("Alice @A #happy ##default: Ready?");

        var alice = model.Speakers.Resolve(new SpeakerNameReference("Alice", SourceSpanFactory.Span()));
        Assert.Equal("Alice", alice.Name);
        Assert.Equal("A", alice.Id);
        Assert.True(alice.IsDefault);
        Assert.Contains(alice.Tags, tag => tag.Name == "happy");
    }

    [Fact]
    public void Analyze_ConfiguredDefaultSpeaker_ResolvesSpeakerlessLines()
    {
        var analyzer = new SemanticAnalyzer(new SemanticAnalyzerOptions([DefaultConfiguredSpeaker("Narrator")]));

        var model = analyzer.Analyze(Pipeline.UntilDesugared("Hi."), DiagnosticsContextFactory.Context("Hi."));

        var speaker = model.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span()));
        Assert.Equal("Narrator", speaker.Name);
        Assert.True(speaker.IsDefault);
    }

    [Fact]
    public void Analyze_InFileDefault_OverridesTheConfiguredDefault()
    {
        var analyzer = new SemanticAnalyzer(new SemanticAnalyzerOptions([DefaultConfiguredSpeaker("Narrator")]));
        var source = "Bob ##default: Hi.";

        var model = analyzer.Analyze(Pipeline.UntilDesugared(source), DiagnosticsContextFactory.Context(source));

        Assert.Equal("Bob", model.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span())).Name);
    }

    [Fact]
    public void Analyze_BuildsTheSceneTreeAndAnchors()
    {
        var model = Analyze(
            """
            ## Play tennis

            Alice: Ready?
            """);

        var scene = Assert.Single(model.SceneRoot.ChildScenes);
        Assert.Equal("play-tennis", scene.Anchor);
        Assert.True(model.Anchors.TryResolve("play-tennis", out _));
    }

    [Fact]
    public void Analyze_ResolvesASameFileJumpToItsScene()
    {
        var model = Analyze(
            """
            ## Play tennis

            Alice: Ready? => [Go](#play-tennis)
            """);

        var resolution = Assert.Single(model.Jumps.Resolutions);
        var sceneJump = Assert.IsType<SceneJump>(resolution);
        Assert.Equal("play-tennis", sceneJump.Scene.Anchor);
    }

    [Fact]
    public void Analyze_ReportsAMissingJumpTarget_AfterBuildingAnchors()
    {
        Analyze(
            """
            ## Play tennis

            Alice: Ready? => [Go](#no-such-scene)
            """,
            out var diagnostics);

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.MissingScene);
    }

    [Fact]
    public void Analyze_ReportsAnUnknownReservedTag()
    {
        Analyze("Alice ##bogus: Hi.", out var diagnostics);

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.UnknownReservedTag);
    }

    private SemanticModel Analyze(string source) => Analyze(source, out _);

    private SemanticModel Analyze(string source, out DiagnosticBag diagnostics)
    {
        var context = DiagnosticsContextFactory.Context(out diagnostics, source);
        return _analyzer.Analyze(Pipeline.UntilDesugared(source), context);
    }
}
