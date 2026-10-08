using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Render;
using NSubstitute;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>Assertions about what a substituted <see cref="IVisualizeRunner"/> was asked to write.</summary>
internal static class VisualizeRunnerAssert
{
    /// <summary>Asserts that no stage graphs were emitted, in any format or to any destination.</summary>
    /// <param name="runner">The substitute the command was given.</param>
    public static void AssertNothingEmitted(IVisualizeRunner runner) =>
        runner.DidNotReceive().RunEmit(
            Arg.Any<string>(), Arg.Any<EmitFormat>(), Arg.Any<string?>(), Arg.Any<CompilerOptions>());

    /// <summary>Asserts that no static HTML report was written.</summary>
    /// <param name="runner">The substitute the command was given.</param>
    public static void AssertNoReportWritten(IVisualizeRunner runner) =>
        runner.DidNotReceive().RunStatic(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<AppliedConfiguration>());
}
