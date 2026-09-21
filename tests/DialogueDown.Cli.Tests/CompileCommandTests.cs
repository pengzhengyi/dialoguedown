using System.Text;
using DialogueDown.Cli.Tests.Support;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Playbook;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Render;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class CompileCommandTests
{
    private const string NarratorConfig = """
        [[speakers]]
        name = "Narrator"
        default = true
        """;

    [Fact]
    public void Compile_ValidScript_Succeeds()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
    }

    [Fact]
    public void Compile_PassesTheScriptSourceToTheCompiler()
    {
        var source = """
            # Hello

            Alice: Hi.
            """;
        using var script = new TempScript(source);
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        var tester = CliTester.Create(compiler);

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        compiler.Received(1).Compile(source);
    }

    [Fact]
    public void Compile_WithConfig_BuildsTheCompilerFromTheResolvedOptions()
    {
        using var tree = new TempTree();
        var configPath = tree.File("dialogue.toml", NarratorConfig);
        using var script = new TempScript("# Scene");
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        var factory = Substitute.For<Func<CompilerOptions, IScriptCompiler>>();
        factory(Arg.Any<CompilerOptions>()).Returns(compiler);
        var tester = CliTester.Create(compilerFactory: factory);

        var result = tester.Run("compile", script.Path, "--config", configPath);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        factory.Received(1).Invoke(
            Arg.Is<CompilerOptions>(o => o != null && o.Speakers.Any(s => s.Name == "Narrator")));
        compiler.Received(1).Compile(Arg.Any<string>());
    }

    [Fact]
    public void Compile_ScriptWithAnError_RendersErrataAndReturnsDataError()
    {
        using var script = new TempScript("#lonely: Hi"); // tags without a speaker → DLG1101
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.Contains("DLG1101", result.Output, StringComparison.Ordinal);
        Assert.Contains("error", result.Output, StringComparison.Ordinal);
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

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("DLG1003", result.Output, StringComparison.Ordinal);
        Assert.Contains("warning", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_MissingConfig_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--config", "no-such.toml");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("not found", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_MalformedConfig_FailsWithALocatedError()
    {
        using var tree = new TempTree();
        var configPath = tree.File("dialogue.toml", "broken =");
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--config", configPath);

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.Contains("dialogue.toml", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create();

        var result = tester.Run("compile", "does-not-exist.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("not found", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_Mode_OverridesTheCompilationMode()
    {
        using var script = new TempScript("# Scene");
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        var factory = Substitute.For<Func<CompilerOptions, IScriptCompiler>>();
        factory(Arg.Any<CompilerOptions>()).Returns(compiler);
        var tester = CliTester.Create(compilerFactory: factory);

        tester.Run("compile", script.Path, "--mode", "best-effort");

        factory.Received(1).Invoke(Arg.Is<CompilerOptions>(o => o != null && o.Mode == CompilationMode.BestEffort));
    }

    [Fact]
    public void Compile_WithoutMode_InheritsTheResolvedMode()
    {
        using var script = new TempScript("# Scene");
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Returns(ScriptCompilerFactory.CreateDefault().Compile(""));
        var factory = Substitute.For<Func<CompilerOptions, IScriptCompiler>>();
        factory(Arg.Any<CompilerOptions>()).Returns(compiler);
        var tester = CliTester.Create(compilerFactory: factory);

        tester.Run("compile", script.Path);

        factory.Received(1).Invoke(Arg.Is<CompilerOptions>(o => o != null && o.Mode == CompilationMode.StageBoundary));
    }

    [Fact]
    public void Compile_UnknownMode_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--mode", "turbo");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("--mode", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_FixWithEmit_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix", "--emit", "dot");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("--fix", result.Output, StringComparison.Ordinal);
        Assert.Contains("--emit", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_FixWithOutput_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix", "-o", "fixed.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("--fix", result.Output, StringComparison.Ordinal);
        Assert.Contains("-o", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_AppliesThePreferredFixInPlace()
    {
        var source = """
            # The Workshop

            Alice: The rule is simple => the lever opens the door.
            """;
        using var script = new TempScript(source);
        var tester = CliTester.Create();

        var result = tester.Run("compile", script.Path, "--fix");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("\\=> the lever", File.ReadAllText(script.Path), StringComparison.Ordinal);
        // The diagnostics are exactly what a plain compile prints, hint included.
        Assert.Contains("warning DLG1113", result.Output, StringComparison.Ordinal);
        Assert.Contains("1 warning", result.Output, StringComparison.Ordinal);
        Assert.Contains("1 fixable with --fix", result.Output, StringComparison.Ordinal);
        // Then the fix section: the write notice, the note, and the hunk.
        Assert.Contains("(1 fix)", result.Output, StringComparison.Ordinal);
        Assert.Contains("1. Applied Fix: Escape as literal text", result.Output, StringComparison.Ordinal);
        Assert.Contains("-Alice: The rule is simple => the lever opens the door.", result.Output, StringComparison.Ordinal);
        Assert.Contains("+Alice: The rule is simple \\=> the lever opens the door.", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("  fix applied", result.Output, StringComparison.Ordinal);
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

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Equal(2, result.Output.Split("Applied Fix: Escape as literal text").Length - 1);
        Assert.Equal(2, result.Output.Split("| +Alice: Go").Length - 1);
        Assert.Contains("(2 fixes)", result.Output, StringComparison.Ordinal);
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

        Assert.Contains("DLG1107", plain.Output, StringComparison.Ordinal);
        Assert.Equal(plain.Output, fix.Output);
        Assert.Equal(ExitCodes.Success, fix.ExitCode);
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

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.Contains("\\=> the lever", File.ReadAllText(script.Path), StringComparison.Ordinal);
        Assert.Contains("1 error, 1 warning", result.Output, StringComparison.Ordinal);
        Assert.Contains("(1 fix; 1 error remains)", result.Output, StringComparison.Ordinal);
        Assert.Contains("1. Applied Fix: Escape as literal text", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_PreservesALeadingBom()
    {
        using var tree = new TempTree();
        var path = tree.File("scene.dialogue.md");
        File.WriteAllBytes(
            path,
            [
                0xEF,
                0xBB,
                0xBF,
                .. Encoding.UTF8.GetBytes("# The Workshop\n\nAlice: The rule is simple => the lever opens.\n"),
            ]);
        var tester = CliTester.Create();

        var result = tester.Run("compile", path, "--fix");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Contains("\\=>", Encoding.UTF8.GetString(bytes[3..]), StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_Fix_IsIdempotent()
    {
        using var script = new TempScript("""
            # The Workshop

            Alice: The rule is simple => the lever opens the door.
            """);
        Assert.Equal(ExitCodes.Success, CliTester.Create().Run("compile", script.Path, "--fix").ExitCode);
        var corrected = File.ReadAllText(script.Path);
        var written = File.GetLastWriteTimeUtc(script.Path);

        var second = CliTester.Create().Run("compile", script.Path, "--fix");

        Assert.Equal(ExitCodes.Success, second.ExitCode);
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

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
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

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        runner.DidNotReceive().RunEmit(
            Arg.Any<string>(), Arg.Any<EmitFormat>(), Arg.Any<string?>(), Arg.Any<CompilerOptions>());
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

        Assert.Equal(ExitCodes.Success, result.ExitCode);
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

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Empty(Directory.EnumerateFiles(tree.Root));
        PlaybookReader.Default.Read(standardOutput.ToString());
    }

    [Fact]
    public void Compile_WithoutEmit_StillEmitsAPlaybook()
    {
        // A playbook is what compiling produces, so asking for nothing else asks for one. The
        // help text, the guide, and the pipe example all promise it.
        using var script = new TempScript("Alice: Hello.");
        var standardOutput = new StringWriter();

        var result = CliTester.Create(standardOutput: standardOutput).Run("compile", script.Path);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        PlaybookReader.Default.Read(standardOutput.ToString());
    }

    [Fact]
    public void Compile_AScriptWithErrors_LeavesStandardOutputEmpty()
    {
        // Nothing half-written to pipe into the next command: a failed compile has no playbook,
        // and its diagnostics belong on standard error.
        using var script = new TempScript("""
            # Gate

            Alice: One.

            # Gate

            Bob: Two.
            """);
        var standardOutput = new StringWriter();

        var result = CliTester.Create(standardOutput: standardOutput).Run("compile", script.Path);

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.Empty(standardOutput.ToString());
    }

    [Fact]
    public void Compile_AScriptWithErrors_LeavesTheOutputAlone()
    {
        using var tree = new TempTree();
        using var script = new TempScript("""
            # Gate

            Alice: One.

            # Gate

            Bob: Two.
            """);
        var destination = Path.Combine(tree.Root, "untouched.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        Assert.Equal(ExitCodes.DataError, result.ExitCode);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void Compile_EmitMermaid_FailsWithMigrationGuidance()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run("compile", script.Path, "--emit", "mermaid");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
        Assert.Contains("Mermaid stage emission was removed", result.Output, StringComparison.Ordinal);
        Assert.Contains("--emit dot", result.Output, StringComparison.Ordinal);
        Assert.Contains("fenced `mermaid` blocks", result.Output, StringComparison.Ordinal);
        runner.DidNotReceive().RunEmit(
            Arg.Any<string>(), Arg.Any<EmitFormat>(), Arg.Any<string?>(), Arg.Any<CompilerOptions>());
    }

    [Fact]
    public void Compile_EmitUnknownFormat_FailsValidationWithoutRunning()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner);

        var result = tester.Run("compile", script.Path, "--emit", "yaml");

        Assert.NotEqual(0, result.ExitCode);
        runner.DidNotReceive().RunEmit(
            Arg.Any<string>(), Arg.Any<EmitFormat>(), Arg.Any<string?>(), Arg.Any<CompilerOptions>());
    }

    [Fact]
    public void Compile_OutputWithoutEmit_AsksForTheDefaultFormat()
    {
        // `--output` alone used to be an error, when the only thing to emit was the stage graphs.
        // A playbook is the compiler's own artifact, so naming a destination now asks for one.
        using var tree = new TempTree();
        using var script = new TempScript("# Scene");
        var destination = Path.Combine(tree.Root, "scene.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.True(File.Exists(destination));
    }
}
