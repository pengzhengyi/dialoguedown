using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Visualization.Live;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: resolve the project's <see cref="CompilerOptions"/>, build a
/// compiler configured with them, compile the script through the facade, render any diagnostics as
/// errata, and return a data-error exit code when the script has errors. With <c>--emit</c> it
/// instead writes each stage's graph as text — a non-interactive export, which is why it lives
/// here rather than on the browser-opening <c>visualize</c>.
/// </summary>
internal sealed class CompileCommand : Command<CompileSettings>
{
    private readonly ProjectConfiguration _configuration;
    private readonly Func<CompilerOptions, IScriptCompiler> _compilerFactory;
    private readonly IErrataRenderer _errata;
    private readonly IVisualizeRunner _runner;

    public CompileCommand(
        ProjectConfiguration configuration,
        Func<CompilerOptions, IScriptCompiler> compilerFactory,
        IErrataRenderer errata,
        IVisualizeRunner runner)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(compilerFactory);
        ArgumentNullException.ThrowIfNull(errata);
        ArgumentNullException.ThrowIfNull(runner);
        _configuration = configuration;
        _compilerFactory = compilerFactory;
        _errata = errata;
        _runner = runner;
    }

    /// <inheritdoc />
    protected override int Execute(
        CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var options = _configuration.Resolve(settings.Config, ScriptDirectory(settings.Script));
        if (settings.ResolvedMode is { } mode)
        {
            options = options with { Mode = mode };
        }

        // An emit renders the stage graphs as text instead of reporting diagnostics. The format is
        // validated in settings, so parsing here always succeeds.
        if (settings.Emit is not null
            && CompileSettings.TryParseEmitFormat(settings.Emit, out var format))
        {
            return _runner.RunEmit(settings.Script, format, settings.Output, options);
        }

        var compiler = _compilerFactory(options);
        var source = File.ReadAllText(settings.Script);
        var result = compiler.Compile(source);

        _errata.Render(settings.Script, source, result.LocatedDiagnostics);

        // TODO(compiler): emit the compiled result through --emit once the later stages produce
        // something serializable.
        return result.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
    }

    private static string ScriptDirectory(string script) =>
        Path.GetDirectoryName(Path.GetFullPath(script))!;
}
