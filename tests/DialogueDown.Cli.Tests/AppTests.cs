using System.Reflection;
using DialogueDown.Cli.Tests.Support;

namespace DialogueDown.Cli.Tests;

public sealed class AppTests
{
    [Fact]
    public void Version_PrintsTheVersionTheProjectDeclares()
    {
        // Read from the assembly rather than repeated here, so cutting a release does not mean
        // remembering to edit a test. What this pins is the wiring: the number a person sees is
        // the one `<Version>` sets, not the SDK's 1.0.0 default for a project that declares none.
        var declared = ReleaseVersion();
        var tester = CliTester.Create();

        var result = tester.Run("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual("1.0.0", declared);
        Assert.Contains(declared, result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Help_ListsBothCommands()
    {
        var tester = CliTester.Create();

        var result = tester.Run("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("compile", result.Output, StringComparison.Ordinal);
        Assert.Contains("visualize", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void NoArguments_ShowsHelp()
    {
        var tester = CliTester.Create();

        var result = tester.Run();

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("compile", result.Output, StringComparison.Ordinal);
        Assert.Contains("visualize", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownCommand_FailsWithUsageError()
    {
        var tester = CliTester.Create();

        var result = tester.Run("nonsense");

        Assert.Equal(ExitCodes.UsageError, result.ExitCode);
    }

    private static string ReleaseVersion()
    {
        var version = typeof(ExitCodes).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? version[..plus] : version;
    }
}
