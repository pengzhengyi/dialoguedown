using DialogueDown.Cli.Tests.Support;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Playbook;
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
        using var dir = new TempDir();
        var configPath = dir.Write("dialogue.toml", NarratorConfig);
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
        using var dir = new TempDir();
        var configPath = dir.Write("dialogue.toml", "broken =");
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
        using var dir = new TempDir();
        using var script = new TempScript("Alice: Hello.");
        var destination = Path.Combine(dir.Path, "chapter-01.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        PlaybookReader.Default.Read(File.ReadAllText(destination));
    }

    [Fact]
    public void Compile_EmitPlaybook_SaysWhichScriptItCameFrom()
    {
        using var dir = new TempDir();
        using var script = new TempScript("Alice: Hello.");
        var destination = Path.Combine(dir.Path, "out.playbook.json");

        CliTester.Create().Run("compile", script.Path, "--emit", "playbook", "-o", destination);

        var playbook = PlaybookReader.Default.Read(File.ReadAllText(destination));

        Assert.Equal(Path.GetFileName(script.Path), playbook.Script);
    }

    [Fact]
    public void Compile_EmitPlaybookWithoutOutput_GoesToStandardOutput()
    {
        // Standard output, like the stage graphs beside it, so a playbook can be piped.
        using var dir = new TempDir();
        using var script = new TempScript("Alice: Hello.");
        var standardOutput = new StringWriter();

        var result = CliTester.Create(standardOutput: standardOutput)
            .Run("compile", script.Path, "--emit", "playbook");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Empty(Directory.EnumerateFiles(dir.Path));
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
        using var dir = new TempDir();
        using var script = new TempScript("""
            # Gate

            Alice: One.

            # Gate

            Bob: Two.
            """);
        var destination = Path.Combine(dir.Path, "untouched.playbook.json");

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
        using var dir = new TempDir();
        using var script = new TempScript("# Scene");
        var destination = Path.Combine(dir.Path, "scene.playbook.json");

        var result = CliTester.Create().Run("compile", script.Path, "-o", destination);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.True(File.Exists(destination));
    }
}
