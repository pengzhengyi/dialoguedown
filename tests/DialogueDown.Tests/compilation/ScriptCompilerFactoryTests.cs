using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.CompilationAssert;
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

        var result = AssertSuccess(ScriptCompilerFactory.CreateDefault().Compile(source));

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

        var result = AssertSuccess(ScriptCompilerFactory.CreateDefault().Compile(source));

        Assert.Empty(result.Diagnostics);

        var random = Assert.IsType<RandomChoices>(result.Desugared.Body[2]);
        AssertNumberWeight(random.Options[0], 70);
        AssertAutoWeight(random.Options[1]);

        var first = AssertLine(Assert.Single(random.Options[0].Body));
        AssertSpeakerNameReference(first.Speaker!, "Alice");
        AssertSpeechText(first, "Heads it is.");
    }

    [Fact]
    public void CreateDefault_CompilesAControlBlockThroughEveryStage()
    {
        var source =
            """
            # Entrance

            > `if` `Rich?`
            >
            > Alice: Welcome upstairs.
            >
            > `else`
            >
            > Alice: Try downstairs.
            """;

        var result = AssertSuccess(ScriptCompilerFactory.CreateDefault().Compile(source));

        Assert.Empty(result.Diagnostics);

        var control = AssertControlBlock(result.Desugared.Body[1]);
        Assert.Collection(
            control.Branches,
            branch =>
            {
                AssertCondition(branch.Condition!, "Rich");
                AssertSpeechText(AssertLine(Assert.Single(branch.Body)), "Welcome upstairs.");
            },
            branch =>
            {
                Assert.Null(branch.Condition);
                AssertSpeechText(AssertLine(Assert.Single(branch.Body)), "Try downstairs.");
            });
    }

    [Fact]
    public void CreateDefault_WithAConfiguredDefaultSpeaker_UsesItForSpeakerlessLines()
    {
        var options = new CompilerOptions { Speakers = [DefaultConfiguredSpeaker("Narrator")] };

        var result = AssertSuccess(ScriptCompilerFactory.CreateDefault(options).Compile("Hi."));

        var speaker = result.Semantics.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span()));
        Assert.Equal("Narrator", speaker.Name);
    }

    [Fact]
    public void CreateDefault_ContentAfterAJump_SurfacesTheUnreachableWarning()
    {
        var source =
            """
            # A

            # B

            Alice: Go => [A](#a) => [B](#b)
            """;

        var result = ScriptCompilerFactory.CreateDefault().Compile(source);

        var warning = Assert.Single(
            result.Diagnostics,
            d => d.Descriptor.Code == DiagnosticCatalog.UnreachableContentAfterJump.Code);
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

        AssertFailure(result);
        AssertReported(result.Diagnostics, DiagnosticCatalog.TagsWithoutSpeaker);
    }

    [Fact]
    public void CreateDefault_BestEffort_TagsWithoutSpeaker_RecoversToADefaultSpeaker()
    {
        // Best-effort runs every stage, so the recovery is visible — but the error means the
        // compile did not succeed, and the desugared tree rides along on the failure.
        var result = AssertFailure(BestEffortCompiler().Compile("#lonely: Hi"));

        AssertReported(result.Diagnostics, DiagnosticCatalog.TagsWithoutSpeaker);
        AssertDefaultSpeaker(AssertLine(result.Desugared!.Body[0]).Speaker);
    }

    [Fact]
    public void CreateDefault_NotAGameCall_HaltsAtTheStageBoundary()
    {
        var result = ScriptCompilerFactory.CreateDefault().Compile("Alice: say `not a call`");

        AssertFailure(result);
        AssertReported(result.Diagnostics, DiagnosticCatalog.NotAGameCall);
    }

    [Fact]
    public void CreateDefault_BestEffort_NotAGameCall_RecoversToLiteralText()
    {
        var result = AssertFailure(BestEffortCompiler().Compile("Alice: say `not a call`"));

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

    [Fact]
    public void CreateDefault_CleanScript_CompilesAllTheWayToAGraph()
    {
        var result = AssertSuccess(ScriptCompilerFactory.CreateDefault().Compile("""
            # Gate

            Alice: Who goes there?

            => [The end](#END)
            """));

        Assert.False(result.HasErrors);
        Assert.NotEmpty(result.Graph.Nodes);
    }

    [Fact]
    public void CreateDefault_ScriptWithErrors_ProducesNoGraph()
    {
        // A misplaced heading is reported and recovered, so analysis still describes the script.
        // There is no coherent flow to run, so the outcome is a failure and carries no graph.
        var result = AssertFailure(ScriptCompilerFactory.CreateDefault().Compile("""
            > `if` `"Rich"?`
            >
            > # Upstairs
            >
            > Alice: Welcome.
            """));

        Assert.True(result.HasErrors);
        Assert.NotNull(result.Semantics);
    }

    private static IScriptCompiler BestEffortCompiler() =>
        ScriptCompilerFactory.CreateDefault(new CompilerOptions { Mode = CompilationMode.BestEffort });
}
