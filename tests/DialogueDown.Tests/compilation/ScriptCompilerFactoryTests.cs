using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.ConfigurationFactory;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Compilation;

public sealed class ScriptCompilerFactoryTests
{
    [Fact]
    public void CreateDefault_CompilesAScriptThroughEveryStage()
    {
        var source =
            """
            # Scene

            Alice: Ready? => [Play](#scene)

            No speaker here.
            """;

        var result = ScriptCompilerFactory.CreateDefault().Compile(source);

        Assert.Equal(source, result.Source);
        Assert.NotNull(result.Markdown);
        Assert.NotNull(result.Script);

        // The desugared tree upholds the post-desugar invariants: the jump is assembled
        // and the speaker-less line has a default speaker.
        var body = result.Desugared.Body;
        AssertSceneHeading(body[0], "Scene", 1);

        var spoken = AssertLine(body[1]);
        AssertSpeakerNameReference(spoken.Speaker!, "Alice");
        AssertJump(spoken.Speech[^1], "#scene");

        var silent = AssertLine(body[2]);
        AssertDefaultSpeaker(silent.Speaker);

        // The semantic model is bound: the heading became a scene the jump resolves to.
        Assert.True(result.Semantics.Anchors.TryResolve("scene", out _));
        Assert.IsType<SceneJump>(Assert.Single(result.Semantics.Jumps.Resolutions));
    }

    [Fact]
    public void CreateDefault_CompilesARandomChoiceThroughEveryStage()
    {
        var source =
            """
            # Coin

            The coin spins.

            - `70%` Alice: Heads it is.
            - `%` Alice: Tails.
            """;

        var result = ScriptCompilerFactory.CreateDefault().Compile(source);

        Assert.Empty(result.Diagnostics);

        var random = Assert.IsType<RandomChoices>(result.Desugared.Body[2]);
        Assert.Equal(new NumberWeight(70), random.Options[0].Weight);
        Assert.IsType<AutoWeight>(random.Options[1].Weight);

        var first = AssertLine(Assert.Single(random.Options[0].Body));
        AssertSpeakerNameReference(first.Speaker!, "Alice");
        AssertSpeechText(first, "Heads it is.");
    }

    [Fact]
    public void CreateDefault_WithAConfiguredDefaultSpeaker_UsesItForSpeakerlessLines()
    {
        var options = new CompilerOptions { Speakers = [DefaultConfiguredSpeaker("Narrator")] };

        var result = ScriptCompilerFactory.CreateDefault(options).Compile("Hi.");

        var speaker = result.Semantics.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span()));
        Assert.Equal("Narrator", speaker.Name);
    }

    [Fact]
    public void CreateDefault_ALineWithTwoJumps_SurfacesTheMultipleJumpsWarning()
    {
        var source =
            """
            # A

            # B

            Alice: Go => [A](#a) => [B](#b)
            """;

        var result = ScriptCompilerFactory.CreateDefault().Compile(source);

        var warning = Assert.Single(result.Diagnostics, d => d.Descriptor.Code == DiagnosticCatalog.MultipleJumpsOnLine.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void CreateDefault_FourChoiceLevels_SurfacesTheNestingWarning()
    {
        var source =
            """
            - Level 1
                - Level 2
                    - Level 3
                        - Level 4
            """;

        var result = ScriptCompilerFactory.CreateDefault().Compile(source);

        var warning = AssertReported(
            result.Diagnostics, DiagnosticCatalog.DeeplyNestedChoiceBranch);
        var located = AssertLocated(
            result.LocatedDiagnostics,
            DiagnosticCatalog.DeeplyNestedChoiceBranch,
            DiagnosticSeverity.Warning,
            new LinePosition(4, 13));
        var markerOffset = source.IndexOf("- Level 4", StringComparison.Ordinal);

        Assert.Equal([4, 3], warning.MessageArguments);
        Assert.Equal(markerOffset, located.StartOffset);
        Assert.Equal(markerOffset, located.EndOffset);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void CreateDefault_TagsWithoutSpeaker_HaltsAtTheStageBoundary()
    {
        // The tags-without-speaker error is reported during transpile, so a stage-boundary
        // compile stops before analysis and returns a partial result.
        var result = ScriptCompilerFactory.CreateDefault().Compile("#lonely: Hi");

        Assert.False(result.IsComplete);
        AssertReported(result.Diagnostics, DiagnosticCatalog.TagsWithoutSpeaker);
    }

    [Fact]
    public void CreateDefault_BestEffort_TagsWithoutSpeaker_RecoversToADefaultSpeaker()
    {
        var result = BestEffortCompiler().Compile("#lonely: Hi");

        Assert.True(result.IsComplete);
        AssertReported(result.Diagnostics, DiagnosticCatalog.TagsWithoutSpeaker);
        AssertDefaultSpeaker(AssertLine(result.Desugared.Body[0]).Speaker);
    }

    [Fact]
    public void CreateDefault_NotAGameCall_HaltsAtTheStageBoundary()
    {
        var result = ScriptCompilerFactory.CreateDefault().Compile("Alice: say `not a call`");

        Assert.False(result.IsComplete);
        AssertReported(result.Diagnostics, DiagnosticCatalog.NotAGameCall);
    }

    [Fact]
    public void CreateDefault_BestEffort_NotAGameCall_RecoversToLiteralText()
    {
        var result = BestEffortCompiler().Compile("Alice: say `not a call`");

        Assert.True(result.IsComplete);
        AssertReported(result.Diagnostics, DiagnosticCatalog.NotAGameCall);
    }

    [Fact]
    public void CreateDefault_LocatedDiagnostics_LocatesAndRendersEachDiagnostic()
    {
        // The tags-without-speaker error (DLG1101) sits at the start of the only line.
        var result = BestEffortCompiler().Compile("#lonely: Hi");

        var located = AssertLocated(
            result.LocatedDiagnostics,
            DiagnosticCatalog.TagsWithoutSpeaker,
            DiagnosticSeverity.Error,
            new LinePosition(1, 1));
        Assert.Contains("names no speaker", located.Message);
    }

    private static IScriptCompiler BestEffortCompiler() =>
        ScriptCompilerFactory.CreateDefault(new CompilerOptions { Mode = CompilationMode.BestEffort });
}
