using DialogueDown.Cli.Tests.Support;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Live.Serving;
using NSubstitute;
using static DialogueDown.Cli.Tests.Support.CliAssert;
using static DialogueDown.Cli.Tests.Support.ConfigFiles;
using static DialogueDown.Cli.Tests.Support.VisualizeRunnerAssert;

namespace DialogueDown.Cli.Tests;

public sealed class VisualizeCommandTests
{
    [Fact]
    public void Visualize_NoArguments_OpensTheEmptyShellAtCurrentDirectoryInView()
    {
        var shell = ShellRunner();
        var tester = CliTester.Create(shell: shell);

        var result = tester.Run("visualize");

        AssertSucceeded(result);
        AssertServed(shell, script: null, root: Directory.GetCurrentDirectory(), ReportMode.View);
    }

    [Fact]
    public void Visualize_ScriptOnly_OpensTheServedReportInView()
    {
        using var script = new TempScript("# Scene");
        var shell = ShellRunner();
        var tester = CliTester.Create(shell: shell);

        var result = tester.Run("visualize", script.Path);

        AssertSucceeded(result);
        AssertServed(shell, script.Path, root: null, ReportMode.View);
    }

    [Fact]
    public void Visualize_WithADiscoveredConfig_PassesTheConfiguredOptionsToTheRunner()
    {
        using var tree = new TempTree();
        var scriptPath = tree.File("scene.dialogue.md", "# Scene");
        tree.File("dialogue.toml", NarratorByDefault);
        var shell = ShellRunner();
        var tester = CliTester.Create(shell: shell);

        tester.Run("visualize", scriptPath);

        AssertServed(shell, scriptPath, root: null, ReportMode.View);
        Assert.Contains(ConfigurationServedBy(shell).Options.Speakers, speaker => speaker.Name == "Narrator");
    }

    [Fact]
    public void Visualize_ScriptWithEdit_OpensTheServedReportInEdit()
    {
        using var script = new TempScript("# Scene");
        var root = Path.GetDirectoryName(script.Path)!;
        var shell = ShellRunner();
        var tester = CliTester.Create(shell: shell);

        var result = tester.Run("visualize", script.Path, "--edit", "--root", root, "--port", "5199");

        AssertSucceeded(result);
        AssertServed(shell, script.Path, root: root, ReportMode.Edit, port: 5199);
    }

    [Fact]
    public void Visualize_EditWithoutRoot_StillOpensTheServedReportInEdit()
    {
        using var script = new TempScript("# Scene");
        var shell = ShellRunner();
        var tester = CliTester.Create(shell: shell);

        tester.Run("visualize", script.Path, "--edit");

        AssertServed(shell, script.Path, root: null, ReportMode.Edit);
    }

    [Fact]
    public void Visualize_Export_WritesAStaticReport()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var shell = ShellRunner();
        var tester = CliTester.Create(runner: runner, shell: shell);

        tester.Run("visualize", script.Path, "-o", "out.html", "--no-open");

        runner.Received(1).RunStatic(script.Path, "out.html", true, Arg.Any<AppliedConfiguration>());
        AssertNothingServed(shell);
    }

    [Fact]
    public void Visualize_Emit_FailsAndPointsAtCompile()
    {
        // Emitting stage graphs is a compile-and-export step, so it lives on `compile`. Failing
        // loudly matters here: an ignored option would let `-o stages.dot` quietly receive an
        // HTML report instead of the DOT text the caller asked for.
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, shell: ShellRunner());

        var result = tester.Run("visualize", script.Path, "--emit", "dot");

        AssertExited(result, ExitCodes.UsageError, "ddown compile");
        AssertNothingEmitted(runner);
    }

    [Fact]
    public void Visualize_EmitWithOutput_FailsRatherThanWritingAnHtmlReport()
    {
        using var script = new TempScript("# Scene");
        var runner = Substitute.For<IVisualizeRunner>();
        var tester = CliTester.Create(runner: runner, shell: ShellRunner());

        var result = tester.Run("visualize", script.Path, "--emit", "dot", "-o", "stages.dot");

        AssertExited(result, ExitCodes.UsageError);
        AssertNoReportWritten(runner);
    }

    [Fact]
    public void Visualize_MissingConfig_FailsWithUsageError()
    {
        using var script = new TempScript("# Scene");
        var tester = CliTester.Create(shell: ShellRunner());

        var result = tester.Run("visualize", script.Path, "--config", "no-such.toml");

        AssertExited(result, ExitCodes.UsageError);
    }

    [Fact]
    public void Visualize_MissingFile_FailsWithUsageError()
    {
        var tester = CliTester.Create(shell: ShellRunner());

        var result = tester.Run("visualize", "does-not-exist.dialogue.md");

        AssertExited(result, ExitCodes.UsageError);
    }

    [Fact]
    public void Visualize_OutputWithoutScript_FailsWithUsageError()
    {
        var tester = CliTester.Create(shell: ShellRunner());

        var result = tester.Run("visualize", "-o", "out.html");

        AssertExited(result, ExitCodes.UsageError);
    }

    private static IServedShellRunner ShellRunner()
    {
        var shell = Substitute.For<IServedShellRunner>();
        shell
            .RunAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ReportMode>(), Arg.Any<int?>(),
                Arg.Any<bool>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return shell;
    }

    /// <summary>Asserts that the command served the report once, as described.</summary>
    /// <param name="shell">The substitute the command was given.</param>
    /// <param name="script">The script it opened, or <c>null</c> for the empty shell.</param>
    /// <param name="root">The folder the Explorer shows, or <c>null</c> when none was named.</param>
    /// <param name="mode">Whether the report opened to view or to edit.</param>
    /// <param name="port">The port asked for, or <c>null</c> to pick a free one.</param>
    /// <param name="noOpen">Whether the command was told not to open a browser.</param>
    private static void AssertServed(
        IServedShellRunner shell,
        string? script,
        string? root,
        ReportMode mode,
        int? port = null,
        bool noOpen = false) =>
        shell.Received(1).RunAsync(
            script, root, mode, port, noOpen, Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());

    private static void AssertNothingServed(IServedShellRunner shell) =>
        shell.DidNotReceive().RunAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ReportMode>(), Arg.Any<int?>(),
            Arg.Any<bool>(), Arg.Any<AppliedConfiguration>(), Arg.Any<CancellationToken>());

    /// <summary>The configuration the report was served with.</summary>
    /// <param name="shell">The substitute the command was given.</param>
    /// <returns>The configuration passed to the shell's only call.</returns>
    private static AppliedConfiguration ConfigurationServedBy(IServedShellRunner shell) =>
        Assert.Single(Assert.Single(shell.ReceivedCalls()).GetArguments().OfType<AppliedConfiguration>());
}
