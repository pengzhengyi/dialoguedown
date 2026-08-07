using DialogueDown.Compilation;
using DialogueDown.Configuration;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: resolve the project's <see cref="CompilerOptions"/>, build a
/// compiler configured with them, compile the script through the facade, render any diagnostics as
/// errata, and return a data-error exit code when the script has errors. The compiler does not emit
/// a serializable artifact yet; an output option will return once the later stages produce one.
/// </summary>
internal sealed class CompileCommand : Command<CompileSettings>
{
    private readonly ProjectConfiguration _configuration;
    private readonly Func<CompilerOptions, IScriptCompiler> _compilerFactory;
    private readonly IErrataRenderer _errata;

    public CompileCommand(
        ProjectConfiguration configuration,
        Func<CompilerOptions, IScriptCompiler> compilerFactory,
        IErrataRenderer errata)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(compilerFactory);
        ArgumentNullException.ThrowIfNull(errata);
        _configuration = configuration;
        _compilerFactory = compilerFactory;
        _errata = errata;
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

        var compiler = _compilerFactory(options);
        var source = File.ReadAllText(settings.Script);
        var result = compiler.Compile(source);

        _errata.Render(settings.Script, source, result.LocatedDiagnostics);

        // TODO(compiler): add an output option that writes a serializable result once
        // the later stages produce one.
        return result.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
    }

    private static string ScriptDirectory(string script) =>
        Path.GetDirectoryName(Path.GetFullPath(script))!;
}
