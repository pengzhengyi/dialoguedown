using System.Text;
using DialogueDown.Cli.Tests.Support;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Playbook;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Render;
using NSubstitute;
using static DialogueDown.Cli.Tests.Support.CliAssert;
using static DialogueDown.Cli.Tests.Support.ConfigFiles;
using static DialogueDown.Cli.Tests.Support.OutputAssert;
using static DialogueDown.Cli.Tests.Support.VisualizeRunnerAssert;

namespace DialogueDown.Cli.Tests;

public sealed class CompileCommandTests
{
    /// <summary>A line whose arrow leads nowhere: a DLG1113 warning that <c>--fix</c> escapes.</summary>
    private const string ADanglingArrow = """
        # The Workshop

        Alice: The rule is simple => the lever opens the door.
        """;

    /// <summary>Two scenes with the same name: a DLG2001 error, so the compile fails.</summary>
    private const string TwoScenesNamedAlike = """
        # Gate

        Alice: One.

        # Gate

        Bob: Two.
        """;

    [Fact]
    public void Compile_ValidScript_Succeeds()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        AssertSucceeded(result);
    }

    [Fact]
    public void Compile_PassesTheScriptSourceToTheCompiler()
    {
        var source = """
            # Hello

            Alice: Hi.
            """;
        using var script = new TempScript(source);
        var compiler = ACompilerThatSucceeds();
        var tester = CliTester.Create(compiler);

        var result = tester.Run("compile", script.Path);

        AssertSucceeded(result);
        compiler.Received(1).Compile(source);
    }

    [Fact]
    public void Compile_WithConfig_BuildsTheCompilerFromTheResolvedOptions()
    {
        using var tree = new TempTree();
        var configPath = tree.File("dialogue.toml", NarratorByDefault);
        using var script = new TempScript("# Scene");
        var compiler = ACompilerThatSucceeds();
        var factory = AFactoryOf(compiler);
        var tester = CliTester.Create(compilerFactory: factory);

        var result = tester.Run("compile", script.Path, "--config", configPath);

        AssertSucceeded(result);
        Assert.Contains(OptionsTheCompilerWasBuiltWith(factory).Speakers, speaker => speaker.Name == "Narrator");
        compiler.Received(1).Compile(Arg.Any<string>());
    }

    [Fact]
    public void Compile_ScriptWithAnError_RendersErrataAndReturnsDataError()
    {
        using var script = new TempScript("#lonely: Hi"); // tags without a speaker → DLG1101
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        AssertExited(result, ExitCodes.DataError, "DLG1101", "error");
    }

    [Fact]
    public void Compile_ScriptWithOnlyAWarning_RendersErrataButSucceeds()
    {
        var source = """
            # A

            # B

            Alice: Go => [A](#a) => [B](#b)
            """;
        using var script = new TempScript(source);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        AssertSucceeded(result, "DLG1003", "warning");
    }

    [Fact]
    public void Compile_MissingConfig_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--config", "no-such.toml");

        AssertExited(result, ExitCodes.UsageError, "not found");
    }

    [Fact]
    public void Compile_MalformedConfig_FailsWithALocatedError()
    {
        using var tree = new TempTree();
        var configPath = tree.File("dialogue.toml", "broken =");
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--config", configPath);

        AssertExited(result, ExitCodes.DataError, "dialogue.toml");
    }

    [Fact]
    public void Compile_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create();

        var result = tester.Run("compile", "does-not-exist.dialogue.md");

        AssertExited(result, ExitCodes.UsageError, "not found");
    }

    [Fact]
    public void Compile_Mode_OverridesTheCompilationMode()
    {
        using var script = new TempScript("# Scene");
        var factory = AFactoryOf(ACompilerThatSucceeds());
        var tester = CliTester.Create(compilerFactory: factory);

        tester.Run("compile", script.Path, "--mode", "best-effort");

        Assert.Equal(CompilationMode.BestEffort, OptionsTheCompilerWasBuiltWith(factory).Mode);
    }

    [Fact]
    public void Compile_WithoutMode_InheritsTheResolvedMode()
    {
        using var script = new TempScript("# Scene");
        var factory = AFactoryOf(ACompilerThatSucceeds());
        var tester = CliTester.Create(compilerFactory: factory);

        tester.Run("compile", script.Path);

        Assert.Equal(CompilationMode.StageBoundary, OptionsTheCompilerWasBuiltWith(factory).Mode);
    }

    [Fact]
    public void Compile_UnknownMode_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--mode", "turbo");

        AssertExited(result, ExitCodes.UsageError, "--mode");
    }

    [Fact]
    public void Compile_FixWithEmit_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix", "--emit", "dot");

        AssertExited(result, ExitCodes.UsageError, "--fix", "--emit");
    }

    [Fact]
    public void Compile_FixWithOutput_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix", "-o", "fixed.dialogue.md");

        AssertExited(result, ExitCodes.UsageError, "--fix", "-o");
    }

    [Fact]
    public void Compile_Fix_AppliesThePreferredFixInPlace()
    {
        using var script = new TempScript(ADanglingArrow);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix");

        var output = AssertSucceeded(result);
        AssertInOrder(
            output,
            // The diagnostics are exactly what a plain compile prints, hint included.
            "warning DLG1113",
            "1 warning",
            "1 fixable with --fix",
            // Then the fix section: the write notice, the note, and the hunk.
            "(1 fix)",
            "1. Applied Fix: Escape as literal text",
            "-Alice: The rule is simple => the lever opens the door.",
            "+Alice: The rule is simple \\=> the lever opens the door.");
        Assert.DoesNotContain("  fix applied", output, StringComparison.Ordinal);
        Assert.Contains("\\=> the lever", File.ReadAllText(script.Path), StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_WithTwoFixes_ShowsANoteAndHunkPerFix()
    {
        var source = """
            # The Workshop

            Alice: Go => left, then => right.
            """;
        using var script = new TempScript(source);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix");

        var output = AssertSucceeded(result, "(2 fixes)");
        AssertOccurs(output, "Applied Fix: Escape as literal text", times: 2);
        AssertOccurs(output, "| +Alice: Go", times: 2);
    }

    [Fact]
    public void Compile_Fix_WithNothingToFix_PrintsWhatAPlainCompilePrints()
    {
        var source = """
            # The Workshop

            *Bob*: Did you read the manual?
            """;
        using var script = new TempScript(source);
        var written = File.GetLastWriteTimeUtc(script.Path);

        var plain = CliTester.Create().Run("compile", script.Path);
        var fix = CliTester.Create().Run("compile", script.Path, "--fix");

        var plainOutput = AssertSucceeded(plain, "DLG1107");
        Assert.Equal(plainOutput, AssertSucceeded(fix));
        Assert.Equal(source, File.ReadAllText(script.Path));
        Assert.Equal(written, File.GetLastWriteTimeUtc(script.Path));
    }

    [Fact]
    public void Compile_Fix_WithASurvivingError_WritesTheCorrectionAndKeepsTheDataError()
    {
        var source = """
            # The Workshop

            Alice: The rule is simple => the lever opens the door.

            # The Workshop

            Alice: Again, then.
            """;
        using var script = new TempScript(source);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix");

        AssertExited(
            result,
            ExitCodes.DataError,
            "1 error, 1 warning",
            "(1 fix; 1 error remains)",
            "1. Applied Fix: Escape as literal text");
        Assert.Contains("\\=> the lever", File.ReadAllText(script.Path), StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_AnErrorLaterOnTheFixedLine_IsNotReportedAgainAfterFixing()
    {
        // The escape moves the unresolved jump one column right, but it is the same error.
        var source = """
            # Start

            Alice: Go => then => [onward](#missing)
            """;
        using var script = new TempScript(source);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix");

        var output = AssertExited(result, ExitCodes.DataError, "(1 fix; 1 error remains)");
        Assert.DoesNotContain("after fixing:", output, StringComparison.Ordinal);
        Assert.Contains("Go \\=> then", File.ReadAllText(script.Path), StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_PreservesALeadingBom()
    {
        using var tree = new TempTree();
        var path = tree.File("scene.dialogue.md");
        File.WriteAllBytes(path, [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(ADanglingArrow)]);
        var tester = CliTester.Create();

        var result = tester.Run("compile", path, "--fix");

        AssertSucceeded(result);
        var bytes = File.ReadAllBytes(path);
        var bom = Encoding.UTF8.GetPreamble();
        Assert.Equal(bom, bytes[..bom.Length]);
        Assert.Contains("\\=>", Encoding.UTF8.GetString(bytes[bom.Length..]), StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_IsIdempotent()
    {
        using var script = new TempScript(ADanglingArrow);
        AssertSucceeded(CliTester.Create().Run("compile", script.Path, "--fix"));
        var corrected = File.ReadAllText(script.Path);
        var written = File.GetLastWriteTimeUtc(script.Path);

        var second = CliTester.Create().Run("compile", script.Path, "--fix");

        AssertSucceeded(second);
        Assert.Equal(string.Empty, second.Output);
        Assert.Equal(corrected, File.ReadAllText(script.Path));
        Assert.Equal(written, File.GetLastWriteTimeUtc(script.Path));
    }

    [Fact]
    public void Compile_FailFastMode_IsRejected()
    {
        // Fail-fast throws instead of collecting errata, so it is not offered as a CLI mode.
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--mode", "fail-fast");

        AssertExited(result, ExitCodes.UsageError);
    }

    [Fact]
    public void Compile_EmitDotWithOutput_WritesTheStageGraphsToTheFile()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        tester.Run("compile", script.Path, "--emit", "dot", "-o", "scene.dot");

        runner.Received(1).RunEmit(script.Path, EmitFormat.Dot, "scene.dot", Arg.Any<CompilerOptions>());
    }

    [Fact]
    public void Compile_EmitDotWithoutOutput_WritesToStandardOutput()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        tester.Run("compile", script.Path, "--emit", "dot");

        runner.Received(1).RunEmit(script.Path, EmitFormat.Dot, null, Arg.Any<CompilerOptions>());
    }

    [Fact]
    public void Compile_WithoutEmit_DoesNotRenderStageGraphs()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run("compile", script.Path);

        AssertSucceeded(result);
        AssertNothingEmitted(runner);
    }

    [Fact]
    public void Compile_AnOutputWithNoFormat_WritesAPlaybookAReaderTakesBack()
    {
        // A playbook is the compiler's own artifact, so naming a destination is enough to ask
        // for one. The stage graphs are the export that has to say so.
        using var tree = new TempTree();
        using var script = new TempScript("Alice: Hello.");
        var destination = Path.Combine(tree.Root, "chapter-01.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        AssertSucceeded(result);
        PlaybookReader.Default.Read(File.ReadAllText(destination));
    }

    [Fact]
    public void Compile_EmitPlaybook_SaysWhichScriptItCameFrom()
    {
        using var tree = new TempTree();
        using var script = new TempScript("Alice: Hello.");
        var destination = Path.Combine(tree.Root, "out.playbook.json");

        CliTester.Create().Run("compile", script.Path, "--emit", "playbook", "-o", destination);

        var playbook = PlaybookReader.Default.Read(File.ReadAllText(destination));

        Assert.Equal(Path.GetFileName(script.Path), playbook.Script);
    }

    [Fact]
    public void Compile_EmitPlaybookWithoutOutput_GoesToStandardOutput()
    {
        // Standard output, like the stage graphs beside it, so a playbook can be piped.
        using var tree = new TempTree();
        using var script = new TempScript("Alice: Hello.");
        var standardOutput = new StringWriter();

        var result = CliTester.Create(standardOutput: standardOutput)
            .Run("compile", script.Path, "--emit", "playbook");

        AssertSucceeded(result);
        Assert.Empty(Directory.EnumerateFiles(tree.Root));
        PlaybookReader.Default.Read(standardOutput.ToString());
    }

    [Fact]
    public void Compile_WithoutEmit_StillEmitsAPlaybook()
    {
        // A playbook is what compiling produces, so asking for nothing else asks for one.
        using var script = new TempScript("Alice: Hello.");
        var standardOutput = new StringWriter();

        var result = CliTester.Create(standardOutput: standardOutput).Run("compile", script.Path);

        AssertSucceeded(result);
        PlaybookReader.Default.Read(standardOutput.ToString());
    }

    [Fact]
    public void Compile_AScriptWithErrors_LeavesStandardOutputEmpty()
    {
        // Nothing half-written to pipe into the next command: a failed compile has no playbook,
        // and its diagnostics belong on standard error.
        using var script = new TempScript(TwoScenesNamedAlike);
        var standardOutput = new StringWriter();

        var result = CliTester.Create(standardOutput: standardOutput).Run("compile", script.Path);

        AssertExited(result, ExitCodes.DataError);
        Assert.Empty(standardOutput.ToString());
    }

    [Fact]
    public void Compile_AScriptWithErrors_LeavesTheOutputAlone()
    {
        using var tree = new TempTree();
        using var script = new TempScript(TwoScenesNamedAlike);
        var destination = Path.Combine(tree.Root, "untouched.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        AssertExited(result, ExitCodes.DataError);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void Compile_EmitMermaid_FailsWithMigrationGuidance()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run("compile", script.Path, "--emit", "mermaid");

        AssertExited(
            result,
            ExitCodes.UsageError,
            "'--emit mermaid' is not a compile format",
            "--emit dot",
            "fenced `mermaid` blocks");
        AssertNothingEmitted(runner);
    }

    [Fact]
    public void Compile_EmitUnknownFormat_FailsValidationWithoutRunning()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run("compile", script.Path, "--emit", "yaml");

        AssertExited(result, ExitCodes.UsageError);
        AssertNothingEmitted(runner);
    }

    [Fact]
    public void Compile_OutputWithoutEmit_AsksForTheDefaultFormat()
    {
        // A playbook is the compiler's own artifact, so naming a destination asks for one.
        using var tree = new TempTree();
        using var script = new TempScript("# Scene");
        var destination = Path.Combine(tree.Root, "scene.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        AssertSucceeded(result);
        Assert.True(File.Exists(destination));
    }

    /// <summary>A compiler that reports a clean compile of an empty script, whatever it is given.</summary>
    /// <returns>A substitute a test can ask what it was given.</returns>
    private static IScriptCompiler ACompilerThatSucceeds()
    {
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        return compiler;
    }

    /// <summary>A compiler factory that hands out <paramref name="compiler"/> whatever the options.</summary>
    /// <param name="compiler">The compiler every call returns.</param>
    /// <returns>A substitute a test can ask which options it was given.</returns>
    private static Func<CompilerOptions, IScriptCompiler> AFactoryOf(IScriptCompiler compiler)
    {
        var factory = Substitute.For<Func<CompilerOptions, IScriptCompiler>>();
        factory(Arg.Any<CompilerOptions>()).Returns(compiler);
        return factory;
    }

    /// <summary>The options the command built its one compiler from.</summary>
    /// <param name="factory">The factory the command was given.</param>
    /// <returns>The options of the factory's only call.</returns>
    private static CompilerOptions OptionsTheCompilerWasBuiltWith(Func<CompilerOptions, IScriptCompiler> factory) =>
        Assert.IsType<CompilerOptions>(Assert.Single(factory.ReceivedCalls()).GetArguments()[0]);
}
