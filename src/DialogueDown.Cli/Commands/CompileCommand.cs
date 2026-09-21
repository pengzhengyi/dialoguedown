using System.Text.Json;
using DialogueDown.Cli.Fixing;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Visualization.Live;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Commands;

/// <summary>
/// The <c>compile</c> command: resolve the project's <see cref="CompilerOptions"/>, build a
/// compiler configured with them, compile the script through the facade, render any diagnostics as
/// errata, write what was asked for, and return a data-error exit code when the script has
/// errors. It emits a playbook by default; <c>--emit dot</c> asks instead for each stage's graph
/// as text — a non-interactive export, which is why it lives here rather than on the
/// browser-opening <c>visualize</c>.
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
    protected override int Execute(
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

        _errata.Render(
            settings.Script,
            source,
            [.. result.LocatedDiagnostics.Select(diagnostic => new ReportedDiagnostic(diagnostic, null))]);

        if (settings.EmitsPlaybook && result is CompilationSuccess compiled)
        {
            WritePlaybook(compiled, settings);
        }

        return result.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
    }

    private static string ScriptDirectory(string script) =>
        Path.GetDirectoryName(Path.GetFullPath(script))!;

    // Identity is code plus position, not offset: an applied insertion shifts every offset after
    // it while a surviving diagnostic keeps its line and column, and those point into the text the
    // listing describes — the script as read.
    private static IReadOnlyList<LocatedDiagnostic> NewAfterFixing(
        IReadOnlyList<LocatedDiagnostic> asFound, IReadOnlyList<LocatedDiagnostic> corrected)
    {
        var known = asFound
            .Select(diagnostic => (diagnostic.Code, diagnostic.Start.Line, diagnostic.Start.Column))
            .ToHashSet();
        return
        [
            .. corrected.Where(diagnostic =>
                !known.Contains((diagnostic.Code, diagnostic.Start.Line, diagnostic.Start.Column))),
        ];
    }

    // Fix mode: correct the script in place, then report the diagnostics as found, each with what
    // happened to its fix. A run with nothing applicable prints exactly what a plain compile
    // prints, so silence stays silence.
    private int RunFix(CompileSettings settings, CompilerOptions options)
    {
        var script = ScriptContents.Read(settings.Script);
        var compiler = _compilerFactory(options);
        var asFound = compiler.Compile(script.Text);
        var application = FixApplier.Apply(script.Text, asFound.LocatedDiagnostics);
        if (!application.HasCandidates)
        {
            _errata.Render(settings.Script, script.Text, application.Reported);
            return asFound.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
        }

        string? written = null;
        if (application.AppliedCount > 0)
        {
            (script with { Text = application.Text }).Write(settings.Script);
            written = settings.Script;
        }

        var corrected = compiler.Compile(application.Text);
        var summary = new FixSummary(
            corrected.LocatedDiagnostics.Count,
            written,
            application.Text,
            NewAfterFixing(asFound.LocatedDiagnostics, corrected.LocatedDiagnostics));
        _errata.Render(settings.Script, script.Text, application.Reported, summary);
        return corrected.HasErrors ? ExitCodes.DataError : ExitCodes.Success;
    }

    // Only a successful compile has a graph, so a script with errors leaves the destination as it
    // was. Warnings still write: a warning is a smell the compiler tolerates, and anything it
    // cannot tolerate is an error.
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
