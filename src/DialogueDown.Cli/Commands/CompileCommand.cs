using System.Text.Json;
using DialogueDown.Cli.Fixing;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Visualization.Live;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: resolve the project's <see cref="CompilerOptions"/>, compile the
/// script with them, render any diagnostics as errata, write what was asked for, and return a
/// data-error exit code when the script has errors. It writes a playbook by default;
/// <c>--emit dot</c> writes each stage's graph as Graphviz text instead, and <c>--fix</c>
/// corrects the script in place.
/// </summary>
internal sealed class CompileCommand : Command<CompileSettings>
{
    private readonly ProjectConfiguration _configuration;
    private readonly Func<CompilerOptions, IScriptCompiler> _compilerFactory;
    private readonly IErrataRenderer _errata;
    private readonly IVisualizeRunner _runner;
    private readonly IPlaybookWriter _playbooks;
    private readonly TextWriter _standardOutput;

    public CompileCommand(
        ProjectConfiguration configuration,
        Func<CompilerOptions, IScriptCompiler> compilerFactory,
        IErrataRenderer errata,
        IVisualizeRunner runner,
        IPlaybookWriter playbooks,
        TextWriter standardOutput)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(compilerFactory);
        ArgumentNullException.ThrowIfNull(errata);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(playbooks);
        ArgumentNullException.ThrowIfNull(standardOutput);
        _configuration = configuration;
        _compilerFactory = compilerFactory;
        _errata = errata;
        _runner = runner;
        _playbooks = playbooks;
        _standardOutput = standardOutput;
    }

    /// <inheritdoc />
    public override int Execute(
        CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var options = _configuration.Resolve(settings.Config, ScriptDirectory(settings.Script));
        if (settings.ResolvedMode is { } mode)
        {
            options = options with { Mode = mode };
        }

        // A stage-graph emit renders text instead of reporting diagnostics. The format is
        // validated in settings, so parsing here always succeeds.
        if (!settings.EmitsPlaybook
            && settings.Emit is not null
            && CompileSettings.TryParseEmitFormat(settings.Emit, out var format))
        {
            return _runner.RunEmit(settings.Script, format, settings.Output, options);
        }

        if (settings.Fix)
        {
            return RunFix(settings, options);
        }

        var compiler = _compilerFactory(options);
        var source = File.ReadAllText(settings.Script);
        var result = compiler.Compile(source);

        _errata.Render(settings.Script, source, result.LocatedDiagnostics);

        if (settings.EmitsPlaybook && result is CompilationSuccess compiled)
        {
            WritePlaybook(compiled, settings);
        }

        return result.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
    }

    private static string ScriptDirectory(string script) =>
        Path.GetDirectoryName(Path.GetFullPath(script))!;

    // Fix mode: correct the script in place, then report the diagnostics as found and, after them,
    // what the run did. A run with no fix to apply reports the diagnostics exactly as a plain
    // compile does, and nothing more.
    private int RunFix(CompileSettings settings, CompilerOptions options)
    {
        var script = ScriptContents.Read(settings.Script);
        var compiler = _compilerFactory(options);
        var asFound = compiler.Compile(script.Text);
        var application = FixApplier.Apply(script.Text, asFound.LocatedDiagnostics);
        if (!application.HasCandidates)
        {
            _errata.Render(settings.Script, script.Text, asFound.LocatedDiagnostics);
            return asFound.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
        }

        string? written = null;
        if (application.AppliedCount > 0)
        {
            (script with { Text = application.Text }).Write(settings.Script);
            written = settings.Script;
        }

        var corrected = compiler.Compile(application.Text);
        var run = new FixRun(
            application.Outcomes,
            written,
            corrected.LocatedDiagnostics,
            application.NewDiagnostics(asFound.LocatedDiagnostics, corrected.LocatedDiagnostics),
            application.Text);
        _errata.Render(settings.Script, script.Text, asFound.LocatedDiagnostics, run);
        return corrected.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
    }

    // Only a successful compile has a graph, so a script with errors leaves the destination as it
    // was. A script with warnings but no errors still writes.
    private void WritePlaybook(CompilationSuccess compiled, CompileSettings settings)
    {
        var playbook = _playbooks.Write(compiled, Path.GetFileName(settings.Script));
        var json = JsonSerializer.Serialize(playbook, PlaybookJson.Options);

        if (settings.Output is { } destination)
        {
            File.WriteAllText(destination, json);
        }
        else
        {
            _standardOutput.WriteLine(json);
        }
    }

}
