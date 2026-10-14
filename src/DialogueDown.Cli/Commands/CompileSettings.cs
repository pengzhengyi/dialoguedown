using System.ComponentModel;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
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

    [CommandOption("--disable <codes>")]
    [Description("Disable an info or warning diagnostic code for this run, in addition to those dialogue.toml disables. Repeat it, or separate codes with commas.")]
    public string[] Disable { get; init; } = [];

    [CommandOption("--fix")]
    [Description("Correct the script in place by applying each diagnostic's preferred fix, then recompile it.")]
    public bool Fix { get; init; }

    /// <summary>The compilation mode from <c>--mode</c>, or null to inherit the resolved options'
    /// mode. Only valid after <see cref="Validate"/> succeeds.</summary>
    public CompilationMode? ResolvedMode => Mode is null ? null : CompilationModes.TryParse(Mode);

    /// <summary>The codes <c>--disable</c> names, split at commas and trimmed, in the case written.
    /// Only valid after <see cref="Validate"/> succeeds.</summary>
    public IReadOnlyList<string> DisabledCodes => [.. Disable.SelectMany(SplitCodes)];

    /// <summary>Whether this compile writes a playbook.</summary>
    /// <remarks>
    /// True when <c>--emit</c> is absent or names <c>playbook</c>, with or without <c>-o</c>:
    /// without a destination the playbook goes to standard output.
    /// </remarks>
    public bool EmitsPlaybook => Emit is null || IsPlaybook(Emit);

    /// <summary>Whether an <c>--emit</c> value names the playbook format.</summary>
    /// <param name="value">The value given to <c>--emit</c>.</param>
    /// <returns>Whether it names a playbook.</returns>
    public static bool IsPlaybook(string value) =>
        string.Equals(value?.Trim(), PlaybookFormat, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses an <c>--emit</c> value (case-insensitively) into a stage-graph
    /// <see cref="EmitFormat"/>. Returns false for any other value, <c>playbook</c> included.
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

        var disable = ValidateDisable();
        if (!disable.Successful)
        {
            return disable;
        }

        var fix = ValidateFix();
        if (!fix.Successful)
        {
            return fix;
        }

        return ValidateEmit();
    }

    private static string? DisableProblem(string code)
    {
        var info = DiagnosticCodes.Find(code);
        if (info is null)
        {
            return $"Unknown diagnostic code '{code}'. Check the code against the error-code reference: "
                + DiagnosticDocumentation.ReferencePage;
        }

        return info.IsOptional
            ? null
            : $"{info.Code} is an error and cannot be disabled; only info and warning codes can.";
    }

    // A trailing or doubled comma leaves an empty entry, which is dropped.
    private static List<string> SplitCodes(string value) =>
        [.. value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    // Each `--disable` value must name at least one code, and every code must be one a project
    // could disable in dialogue.toml, so a typo never silently disables nothing.
    private ValidationResult ValidateDisable()
    {
        foreach (var value in Disable)
        {
            var codes = SplitCodes(value);
            if (codes.Count == 0)
            {
                return ValidationResult.Error("--disable needs a diagnostic code, such as --disable DLG3002.");
            }

            foreach (var code in codes)
            {
                var problem = DisableProblem(code);
                if (problem is not null)
                {
                    return ValidationResult.Error($"--disable {code}: {problem}");
                }
            }
        }

        return ValidationResult.Success();
    }

    // Fix mode writes the corrected script and no emission, so naming a format or a destination
    // is an error.
    private ValidationResult ValidateFix()
    {
        if (!Fix)
        {
            return ValidationResult.Success();
        }

        if (Emit is not null)
        {
            return ValidationResult.Error(
                "--fix corrects the script in place and writes no emission. Remove --emit, or drop --fix to emit instead.");
        }

        return Output is null
            ? ValidationResult.Success()
            : ValidationResult.Error(
                "--fix corrects the script in place and writes no emission. Remove -o, or drop --fix to emit instead.");
    }

    // A destination without `--emit` is valid: it receives the default format, a playbook.
    private ValidationResult ValidateEmit()
    {
        if (Emit is null || IsPlaybook(Emit))
        {
            return ValidationResult.Success();
        }

        if (string.Equals(Emit.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Error(
                "'--emit mermaid' is not a compile format. Use '--emit dot' for compiler graphs; " +
                "fenced `mermaid` blocks render in the HTML report.");
        }

        return TryParseEmitFormat(Emit, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Unknown --emit format '{Emit}'. Use 'playbook' or 'dot'.");
    }
}
