using System.Reflection;
using DialogueDown.Cli.Commands;
using DialogueDown.ConfigurationLoader.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli;

/// <summary>
/// Configures the command app: application name, version, the shared exception
/// handler, and the subcommands. Shared by the real runner and the tests so both
/// exercise the same command wiring.
/// </summary>
internal static class CliConfigurator
{
    /// <summary>Applies the CLI configuration (name, version, exception handling, commands).</summary>
    public static void Configure(IConfigurator config)
    {
        ArgumentNullException.ThrowIfNull(config);
        // Everything a person reads — diagnostics, usage errors, unhandled exceptions — goes to
        // standard error, so standard output carries only what a compile emits. Set here rather
        // than in the container because Spectre registers this console itself, after ours, and
        // the later registration is the one that resolves.
        config.Settings.Console = ErrorConsole();
        config.SetApplicationName("ddown");
        config.SetApplicationVersion(ResolveVersion());
        config.SetExceptionHandler(HandleException);

        config.AddCommand<CompileCommand>("compile")
            .WithDescription("Compile a DialogueDown script.")
            .WithExample("compile", "scene.dialogue.md")
            .WithExample("compile", "scene.dialogue.md", "--mode", "best-effort")
            .WithExample("compile", "scene.dialogue.md", "--emit", "dot", "-o", "stages.dot");
        config.AddCommand<VisualizeCommand>("visualize")
            .WithDescription("Visualize a DialogueDown script's compilation.")
            .WithExample("visualize", "scene.dialogue.md")
            .WithExample("visualize", "scene.dialogue.md", "--edit")
            .WithExample("visualize", "--root", "scripts")
            .WithExample("visualize", "scene.dialogue.md", "-o", "report.html", "--no-open");
    }

    // Turn framework and command exceptions into a clean message and a meaningful
    // exit code, rather than a stack trace. Writes to the app's console (resolved
    // via DI), which CommandAppTester captures in tests.
    private static int HandleException(Exception exception, ITypeResolver? resolver)
    {
        var console = resolver?.Resolve(typeof(IAnsiConsole)) as IAnsiConsole ?? AnsiConsole.Console;
        switch (exception)
        {
            case NotImplementedException:
                console.MarkupLineInterpolated($"[yellow]{exception.Message}[/]");
                return ExitCodes.NotImplemented;
            case CommandParseException or CommandRuntimeException:
                console.MarkupLineInterpolated($"[red]{exception.Message}[/]");
                return ExitCodes.UsageError;
            case DialogueConfigurationException configError:
                console.MarkupLineInterpolated($"[red]{configError.Location}: {configError.Message}[/]");
                return ExitCodes.DataError;
            default:
                console.WriteException(exception);
                return ExitCodes.Error;
        }
    }

    /// <summary>
    /// The console for everything a person reads: standard error, so a playbook on standard
    /// output is never interrupted by a warning and stays safe to pipe.
    /// </summary>
    private static IAnsiConsole ErrorConsole() =>
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });

    /// <summary>The tool version, read from the assembly (set by <c>&lt;Version&gt;</c>).</summary>
    private static string ResolveVersion()
    {
        var version = typeof(CliConfigurator).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0";

        // Drop any build metadata (e.g. a "+<commit>" suffix from SourceLink).
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? version[..plus] : version;
    }
}
