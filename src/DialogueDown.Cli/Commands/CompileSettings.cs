using System.ComponentModel;
using DialogueDown.Configuration;
using DialogueDown.Visualization.Render;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>Arguments and options for the <c>compile</c> command.</summary>
internal sealed class CompileSettings : CommandSettings
{
    /// <summary>The <c>--emit</c> value naming a playbook, and the format assumed without one.</summary>
    public const string PlaybookFormat = "playbook";

    [CommandArgument(0, "<script>")]
    [Description("The .dialogue.md script to compile.")]
    public string Script { get; init; } = string.Empty;

    [CommandOption("--config <path>")]
    [Description("The dialogue.toml to configure the compile. Default: the nearest one found from the script's folder upward.")]
    public string? Config { get; init; }

    [CommandOption("--mode <mode>")]
    [Description("How far to compile after an error: stage-boundary (default) or best-effort.")]
    public string? Mode { get; init; }

    [CommandOption("--emit <format>")]
    [Description("What to write: 'playbook' (the default) for a compiled playbook, or 'dot' for each stage's graph as Graphviz text.")]
    public string? Emit { get; init; }

    [CommandOption("-o|--output <path>")]
    [Description("Where the emitted text goes, by convention <script>.playbook.json. Default: standard output.")]
    public string? Output { get; init; }

    /// <summary>The compilation mode from <c>--mode</c>, or null to inherit the resolved options'
    /// mode. Only valid after <see cref="Validate"/> succeeds.</summary>
    public CompilationMode? ResolvedMode => Mode is null ? null : CompilationModes.TryParse(Mode);

    /// <summary>Whether this compile writes a playbook.</summary>
    /// <remarks>
    /// A playbook is the compiler's own artifact, so it is what <c>compile</c> emits unless told
    /// otherwise — with or without a destination, since standard output is a destination. The
    /// stage graphs are the export that has to ask.
    /// </remarks>
    public bool EmitsPlaybook => Emit is null || IsPlaybook(Emit);

    /// <summary>
    /// Parses an <c>--emit</c> value (case-insensitively) into an <see cref="EmitFormat"/>.
    /// Returns false for an unknown format.
    /// </summary>
    /// <summary>Whether an <c>--emit</c> value names the playbook format.</summary>
    /// <param name="value">The value given to <c>--emit</c>.</param>
    /// <returns>Whether it names a playbook.</returns>
    public static bool IsPlaybook(string value) =>
        string.Equals(value?.Trim(), PlaybookFormat, StringComparison.OrdinalIgnoreCase);

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
        var script = ScriptArgument.Validate(Script);
        if (!script.Successful)
        {
            return script;
        }

        var config = ConfigArgument.Validate(Config);
        if (!config.Successful)
        {
            return config;
        }

        if (Mode is not null && CompilationModes.TryParse(Mode) is null)
        {
            return ValidationResult.Error(
                $"Unknown --mode '{Mode}'. Use {CompilationModes.SettableNamesDescription}.");
        }

        return ValidateEmit();
    }

    // `--output` names where the emitted text goes, so the two are validated together. A
    // destination on its own is not an error: it asks for the default format.
    private ValidationResult ValidateEmit()
    {
        if (Emit is null || IsPlaybook(Emit))
        {
            return ValidationResult.Success();
        }

        if (string.Equals(Emit.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Error(
                "Mermaid stage emission was removed. Use '--emit dot' for compiler graphs; " +
                "fenced `mermaid` blocks render in the HTML report.");
        }

        return TryParseEmitFormat(Emit, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Unknown --emit format '{Emit}'. Use 'playbook' or 'dot'.");
    }
}
