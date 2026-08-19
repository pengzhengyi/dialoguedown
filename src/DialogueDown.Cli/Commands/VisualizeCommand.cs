using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Live.Serving;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>visualize</c> command. Given a script it opens a <b>served session</b> on the unified
/// report shell — read-only <b>View</b> by default, or editable <b>Edit</b> with <c>--edit</c>,
/// toggled in the browser — with the Explorer sidebar alongside it. With no script it lands on
/// that shell's empty state to browse or create one. <c>-o</c> is a non-interactive static export.
/// Every report is compiled with the project's resolved
/// <see cref="CompilerOptions"/>. Static and text exports are delegated to
/// <see cref="IVisualizeRunner"/>; the served shell is driven through <see cref="IServedShellRunner"/>.
/// </summary>
internal sealed class VisualizeCommand : AsyncCommand<VisualizeSettings>
{
    private readonly IVisualizeRunner _runner;
    private readonly IServedShellRunner _shell;
    private readonly ProjectConfiguration _configuration;

    public VisualizeCommand(
        IVisualizeRunner runner, IServedShellRunner shell, ProjectConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(configuration);
        _runner = runner;
        _shell = shell;
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Task<int> ExecuteAsync(
        CommandContext context, VisualizeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var hasScript = !string.IsNullOrWhiteSpace(settings.Script);

        // A non-interactive emit writes DOT stage text to --output or stdout,
        // never a server. Checked before the HTML export so `--emit dot -o x.dot` emits
        // text rather than an HTML report. The format is validated in settings, so
        // parsing here always succeeds.
        if (settings.Emit is not null
            && VisualizeSettings.TryParseEmitFormat(settings.Emit, out var format))
        {
            return Task.FromResult(
                _runner.RunEmit(
                    settings.Script, format, settings.Output, ConfigurationForScript(settings).Options));
        }

        // A non-interactive HTML export never opens a server or the shell.
        if (settings.Output is not null)
        {
            return Task.FromResult(
                _runner.RunStatic(
                    settings.Script, settings.Output, settings.NoOpen, ConfigurationForScript(settings)));
        }

        var mode = settings.Edit ? ReportMode.Edit : ReportMode.View;

        // A script opens directly on its report, with the Explorer sidebar alongside it; no script
        // lands on the empty shell to browse or create one. One unified server serves both.
        if (hasScript)
        {
            return _shell.RunAsync(
                settings.Script, settings.Root, mode, settings.Port, settings.NoOpen,
                ConfigurationForScript(settings), cancellationToken);
        }

        var root = settings.Root ?? Directory.GetCurrentDirectory();
        var configuration = _configuration.ResolveApplied(settings.Config, root, root);
        return _shell.RunAsync(
            null, root, mode, settings.Port, settings.NoOpen, configuration, cancellationToken);
    }

    // Discover the project's applied configuration from the script's folder upward, never
    // above --root, so the report shows the exact dialogue.toml the compile used.
    private AppliedConfiguration ConfigurationForScript(VisualizeSettings settings) =>
        _configuration.ResolveApplied(
            settings.Config, Path.GetDirectoryName(Path.GetFullPath(settings.Script))!, settings.Root);
}
