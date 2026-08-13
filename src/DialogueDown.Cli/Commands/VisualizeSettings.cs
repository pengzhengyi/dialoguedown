using System.ComponentModel;
using DialogueDown.Visualization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>Arguments and options for the <c>visualize</c> command.</summary>
internal sealed class VisualizeSettings : CommandSettings
{
    [CommandArgument(0, "[script]")]
    [Description("The .dialogue.md script to visualize. Omit it to browse for one in the launcher.")]
    public string Script { get; init; } = string.Empty;

    [CommandOption("--root <dir>")]
    [Description("The folder the launcher browses and serves (the security boundary). Default: the current directory.")]
    public string? Root { get; init; }

    [CommandOption("--edit")]
    [Description("Open the report in Edit mode (editable, saves back to the file). Default: View (read-only, auto-updating).")]
    public bool Edit { get; init; }

    [CommandOption("-o|--output <path>")]
    [Description("Write a self-contained report to this path (a non-interactive export; requires a script).")]
    public string? Output { get; init; }

    [CommandOption("--emit <format>")]
    [Description("Emit each stage's graph as Graphviz DOT text instead of a report (requires a script; writes to --output or stdout).")]
    public string? Emit { get; init; }

    [CommandOption("--config <path>")]
    [Description("The dialogue.toml to configure the report. Default: the nearest one found from the script's folder upward, within --root.")]
    public string? Config { get; init; }

    [CommandOption("--port <port>")]
    [Description("The loopback port for the server (default: an ephemeral port).")]
    public int? Port { get; init; }

    [CommandOption("--no-open")]
    [Description("Do not open the report or launcher in the browser.")]
    public bool NoOpen { get; init; }

    /// <summary>
    /// Parses an <c>--emit</c> value (case-insensitively) into an <see cref="EmitFormat"/>.
    /// Returns false for an unknown format.
    /// </summary>
    public static bool TryParseEmitFormat(string value, out EmitFormat format)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "dot":
                format = EmitFormat.Dot;
                return true;
            default:
                format = default;
                return false;
        }
    }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var config = ConfigArgument.Validate(Config);
        if (!config.Successful)
        {
            return config;
        }

        if (Output is not null && string.IsNullOrWhiteSpace(Script))
        {
            return ValidationResult.Error("--output requires a <script> to export.");
        }

        if (Emit is not null)
        {
            if (string.IsNullOrWhiteSpace(Script))
            {
                return ValidationResult.Error("--emit requires a <script>.");
            }

            if (string.Equals(Emit.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Error(
                    "Mermaid stage emission was removed. Use '--emit dot' for compiler graphs; " +
                    "fenced `mermaid` blocks render in the HTML report.");
            }

            if (!TryParseEmitFormat(Emit, out _))
            {
                return ValidationResult.Error($"Unknown --emit format '{Emit}'. Use 'dot'.");
            }
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
