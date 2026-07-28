using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// The default <see cref="ILauncherRunner"/>: resolves the launch root, serves the
/// embedded launcher page (pre-filled with the initial selection) from a
/// <see cref="LauncherServer"/>, opens it with the injected browser launcher, and stays
/// up until canceled.
/// </summary>
public sealed class LauncherRunner : ILauncherRunner
{
    private readonly IBrowserLauncher _browser;

    /// <summary>Creates a runner that opens results with <paramref name="browser"/>.</summary>
    public LauncherRunner(IBrowserLauncher browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <inheritdoc />
    public Task<int> RunAsync(
        string root,
        string? source,
        LaunchMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        CancellationToken cancellationToken) =>
        RunAsync(root, source, mode, port, noOpen, configuration, Console.Out, Console.Error, cancellationToken);

    internal async Task<int> RunAsync(
        string root,
        string? source,
        LaunchMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root))
        {
            error.WriteLine($"Launch root is not a directory: {root}");
            return 1;
        }

        var launchRoot = LaunchRoot.At(root);
        var html = LauncherPage.Render(launchRoot.RootDirectory, source, ModeToString(mode));
        await using var server = new LauncherServer(
            launchRoot, html, port ?? 0,
            (path, sessionMode) => new LiveSession(
                path, sessionMode, new CompilationVisualizer(configuration), configuration.File?.Path));
        await server.StartAsync();

        var url = server.BaseUrl;
        output.WriteLine($"Launcher rooted at {launchRoot.RootDirectory}");
        output.WriteLine($"  {url}  (press Ctrl+C to stop)");
        if (!noOpen)
        {
            _browser.Open(url);
        }

        // Keep serving until the web host shuts down — Ctrl+C (a termination signal the host
        // handles) or the command's cancellation token; complete normally rather than throwing.
        await server.WaitForShutdownAsync(cancellationToken);

        return 0;
    }

    private static string ModeToString(LaunchMode mode) => mode switch
    {
        LaunchMode.Edit => VisualizationMode.Edit,
        _ => VisualizationMode.View,
    };
}
