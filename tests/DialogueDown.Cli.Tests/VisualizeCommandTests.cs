using DialogueDown.Cli.Tests.Support;
using DialogueDown.Configuration;
using DialogueDown.Visualization;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live;
using NSubstitute;

namespace DialogueDown.Cli.Tests;

public sealed class VisualizeCommandTests
{
    private const string NarratorConfig = """
        [[speakers]]
        name = "Narrator"
        default = true
        """;

    [Fact]
    public void Visualize_NoArguments_OpensLauncherAtCurrentDirectoryInView()
    {
        var launcher = Launcher();
        var tester = CliTester.Create(launcher: launcher);

        var result = tester.Run("visualize");

        Assert.Equal(0, result.ExitCode);
        launcher.Received(1).RunAsync(
            Directory.GetCurrentDirectory(), null, LaunchMode.View,
            null, false, Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_ScriptOnly_OpensAServedViewSession()
    {
        using var script = new TempScript("# Scene");
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", script.Path);

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunServedAsync(
            script.Path, null, false, null, VisualizationMode.View,
            Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_WithADiscoveredConfig_PassesTheConfiguredOptionsToTheRunner()
    {
        using var dir = new TempDir();
        var scriptPath = dir.Write("scene.dialogue.md", "# Scene");
        dir.Write("dialogue.toml", NarratorConfig);
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", scriptPath);

        runner.Received(1).RunServedAsync(
            scriptPath, null, false, null, VisualizationMode.View,
            Arg.Is<AppliedConfiguration>(c => c!.Options.Speakers.Any(s => s.Name == "Narrator")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_ScriptWithEdit_OpensAServedEditSession()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(script.Path)!;
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", script.Path, "--edit", "--root", root, "--port", "5199");

        Assert.Equal(0, result.ExitCode);
        runner.Received(1).RunServedAsync(
            script.Path, 5199, false, root, VisualizationMode.Edit,
            Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_EditWithoutRoot_StillOpensAServedEditSession()
    {
        using var script = new TempScript("# Scene");
        var runner = Runner();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "--edit");

        runner.Received(1).RunServedAsync(
            script.Path, null, false, null, VisualizationMode.Edit,
            Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_Export_WritesAStaticReport()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "-o", "out.html", "--no-open");

        runner.Received(1).RunStatic(script.Path, "out.html", true, Arg.Any<AppliedConfiguration>());
        runner.DidNotReceive().RunServedAsync(
            Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_EmitMermaid_RoutesToRunEmitWithTheFormat()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "--emit", "mermaid");

        runner.Received(1).RunEmit(script.Path, EmitFormat.Mermaid, null, Arg.Any<CompilerOptions>());
        runner.DidNotReceive().RunServedAsync(
            Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_EmitDotWithOutput_RoutesToRunEmitWithTheFile()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        tester.Run("visualize", script.Path, "--emit", "dot", "-o", "scene.dot");

        runner.Received(1).RunEmit(script.Path, EmitFormat.Dot, "scene.dot", Arg.Any<CompilerOptions>());
        runner.DidNotReceive().RunStatic(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<AppliedConfiguration>());
    }

    [Fact]
    public void Visualize_EmitUnknownFormat_FailsValidationWithoutRunning()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", script.Path, "--emit", "yaml");

        Assert.NotEqual(0, result.ExitCode);
        runner.DidNotReceive().RunEmit(
            Arg.Any<string>(), Arg.Any<EmitFormat>(), Arg.Any<string?>(), Arg.Any<CompilerOptions>());
    }

    [Fact]
    public void Visualize_EmitWithoutScript_FailsValidation()
    {
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, launcher: Launcher());

        var result = tester.Run("visualize", "--emit", "mermaid");

        Assert.NotEqual(0, result.ExitCode);
        runner.DidNotReceive().RunEmit(
            Arg.Any<string>(), Arg.Any<EmitFormat>(), Arg.Any<string?>(), Arg.Any<CompilerOptions>());
    }

    [Fact]
    public void Visualize_MissingConfig_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create(runner: Runner(), launcher: Launcher());

        var result = tester.Run("visualize", script.Path, "--config", "no-such.toml");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }

    [Fact]
    public void Visualize_Pick_OpensTheLauncherEvenWithAScript()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(Path.GetFullPath(script.Path))!;
        var runner = Runner();
        var launcher = Launcher();
        var tester = CliTester.Create(runner: runner, launcher: launcher);

        tester.Run("visualize", script.Path, "--pick");

        runner.DidNotReceive().RunServedAsync(
            Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
        launcher.Received(1).RunAsync(
            root, Path.GetFileName(script.Path), LaunchMode.View,
            null, false, Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Visualize_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create(launcher: Launcher());

        var result = tester.Run("visualize", "does-not-exist.dialogue.md");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }

    [Fact]
    public void Visualize_OutputWithoutScript_FailsWithUsageError()
    {
        var tester = CliTester.Create(launcher: Launcher());

        var result = tester.Run("visualize", "-o", "out.html");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }

    private static IVisualizeRunner Runner()
    {
        var runner = Substitute.For<IVisualizeRunner>();
        runner
            .RunServedAsync(
                Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return runner;
    }

    private static ILauncherRunner Launcher()
    {
        var launcher = Substitute.For<ILauncherRunner>();
        launcher
            .RunAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<LaunchMode>(), Arg.Any<int?>(),
                Arg.Any<bool>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return launcher;
    }
}
