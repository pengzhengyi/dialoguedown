using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>Arguments and options for the <c>visualize</c> command.</summary>
internal sealed class VisualizeSettings : CommandSettings
{
    [CommandArgument(0, "[script]")]
    [Description("The .dialogue.md script to visualize. Omit it to browse for one in the report's Explorer.")]
    public string Script { get; init; } = string.Empty;

    [CommandOption("--root <dir>")]
    [Description("The folder the report's Explorer browses and serves (the security boundary). Default: the current directory.")]
    public string? Root { get; init; }

    [CommandOption("--edit")]
    [Description("Open the report in Edit mode (editable, saves back to the file). Default: View (read-only, auto-updating).")]
    public bool Edit { get; init; }

    [CommandOption("-o|--output <path>")]
    [Description("Write a self-contained report to this path (a non-interactive export; requires a script).")]
    public string? Output { get; init; }

    [CommandOption("--config <path>")]
    [Description("The dialogue.toml to configure the report. Default: the nearest one found from the script's folder upward, within --root.")]
    public string? Config { get; init; }

    [CommandOption("--port <port>")]
    [Description("The loopback port for the server (default: an ephemeral port).")]
    public int? Port { get; init; }

    [CommandOption("--no-open")]
    [Description("Do not open the report in the browser.")]
    public bool NoOpen { get; init; }

    // Kept only to fail loudly: `--emit` moved to `compile`. Without it Spectre silently ignores
    // the option, so `visualize x --emit dot -o stages.dot` would quietly write an HTML report
    // into stages.dot instead of the DOT text the caller asked for.
    [CommandOption("--emit <format>", IsHidden = true)]
    [Description("Moved to 'ddown compile --emit'.")]
    public string? Emit { get; init; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        if (Emit is not null)
        {
            return ValidationResult.Error(
                "--emit moved to 'compile'. Use: ddown compile <script> --emit dot -o <path>.");
        }

        var config = ConfigArgument.Validate(Config);
        if (!config.Successful)
        {
            return config;
        }

        if (Output is not null && string.IsNullOrWhiteSpace(Script))
        {
            return ValidationResult.Error("--output requires a <script> to export.");
        }

        if (Root is not null && !Directory.Exists(Root))
        {
            return ValidationResult.Error($"Root is not a directory: {Root}");
        }

        return string.IsNullOrWhiteSpace(Script)
            ? ValidationResult.Success()
            : ScriptArgument.Validate(Script);
    }
}
