using DialogueDown.Cli.Tests.Support;
using DialogueDown.Compilation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Tests;

public sealed class CliConfiguratorTests
{
    [Fact]
    public void UnexpectedError_IsReportedWithTheErrorExitCode()
    {
        using var script = new TempScript("# Scene");
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile(Arg.Any<string>()).Throws(new InvalidOperationException("boom"));
        var tester = CliTester.Create(compiler);

        var result = tester.Run("compile", script.Path);

        Assert.Equal(ExitCodes.Error, result.ExitCode);
        Assert.Contains("boom", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void EverythingAPersonReads_GoesToStandardError()
    {
        // Diagnostics, usage errors, and unhandled exceptions all share the app's console. It
        // has to be standard error, or a warning would corrupt the playbook being piped.
        var config = Substitute.For<IConfigurator>();
        var settings = Substitute.For<ICommandAppSettings>();
        config.Settings.Returns(settings);

        CliConfigurator.Configure(config);

        Assert.Same(Console.Error, settings.Console!.Profile.Out.Writer);
    }
}
