using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Browsing;
using DialogueDown.Visualization.Live.Files;

using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// The default <see cref="IServedShellRunner"/>: serves the report shell — the Explorer sidebar and
/// all — from a single <see cref="ServedShellServer"/> confined to a root, opens it with the injected
/// browser launcher, and stays up until canceled. With a script it hosts the served root resolved
/// from that document and opens its report; with none it serves the empty shell to browse or create.
/// </summary>
public sealed class ServedShellRunner : IServedShellRunner
{
    private readonly IBrowserLauncher _browser;

    /// <summary>Creates a runner that opens results with <paramref name="browser"/>.</summary>
    public ServedShellRunner(IBrowserLauncher browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <inheritdoc />
    public Task<int> RunAsync(
        string? script,
        string? root,
        ReportMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        CancellationToken cancellationToken) =>
        RunAsync(
            script, root, mode, port, noOpen, configuration, Console.Out, Console.Error, cancellationToken);

    internal Task<int> RunAsync(
        string? script,
        string? root,
        ReportMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(script)
            ? RunEmptyShellAsync(root, mode, port, noOpen, configuration, output, error, cancellationToken)
            : RunDocumentAsync(script, root, mode, port, noOpen, configuration, output, error, cancellationToken);

    // Builds the server rooted at browseRoot. Its landing (the empty shell) is rendered from the
    // report bundle; every session it opens is compiled with the applied configuration and carries
    // the reader's launched path for a symlinked document.
    private static ServedShellServer CreateServer(
        BrowseRoot browseRoot, ReportMode mode, int? port, AppliedConfiguration configuration)
    {
        var html = new CompilationVisualizer().RenderEmptyShell(browseRoot.RootDirectory, ModeToString(mode));
        return new ServedShellServer(
            browseRoot,
            html,
            port ?? 0,
            (path, sessionMode, displayPath) => new LiveSession(
                path, sessionMode, new CompilationVisualizer(configuration), configuration.File?.Path, displayPath));
    }

    private static string ModeToString(ReportMode mode) => mode switch
    {
        ReportMode.Edit => VisualizationMode.Edit,
        _ => VisualizationMode.View,
    };

    // The empty shell: no document is open, so serve the Explorer over the root and a create call to
    // action. Opening or creating a script from the tree starts its session and swaps in its report.
    private async Task<int> RunEmptyShellAsync(
        string? root,
        ReportMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var rootDirectory = root ?? Directory.GetCurrentDirectory();
        if (!Directory.Exists(rootDirectory))
        {
            error.WriteLine($"Serve root is not a directory: {rootDirectory}");
            return 1;
        }

        var browseRoot = BrowseRoot.At(rootDirectory);
        await using var server = CreateServer(browseRoot, mode, port, configuration);
        await server.StartAsync();

        output.WriteLine($"Serving {browseRoot.RootDirectory}");
        return await ServeUntilShutdownAsync(server, server.BaseUrl, noOpen, output, cancellationToken);
    }

    // A script opens directly on its report. The served root is resolved from the document — its own
    // folder, an ancestor pinned by renderRoot, or (when the document links images above its folder)
    // the smallest covering folder with the reader's consent — so those images resolve while hosting
    // stays minimal.
    private async Task<int> RunDocumentAsync(
        string script,
        string? renderRoot,
        ReportMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var displayPath = Path.GetFullPath(script);
        string documentPath;
        try
        {
            // Resolve a terminal symlink to its real target so saves replace the real file and the
            // watcher sees its changes; the reader still sees the launched path in the report chrome.
            documentPath = SymlinkResolver.Resolve(displayPath);
        }
        catch (IOException ex)
        {
            error.WriteLine($"Cannot open '{script}': {ex.Message}");
            return 1;
        }

        var references = new CompilationVisualizer().LocalImageReferences(File.ReadAllText(documentPath));
        var consent = new ConsoleHostConsent(!Console.IsInputRedirected, Console.In, Console.Out);
        var serveRoot = ServeRootResolver.Resolve(documentPath, references, renderRoot, consent, error);
        if (serveRoot is null)
        {
            return 1;
        }

        var browseRoot = BrowseRoot.At(serveRoot.Value.RootDirectory);
        await using var server = CreateServer(browseRoot, mode, port, configuration);
        await server.StartAsync();

        var reportPath = server.StartInitialDocument(documentPath, ModeToString(mode), displayPath);
        var url = server.BaseUrl.TrimEnd('/') + reportPath;
        var verb = mode == ReportMode.Edit ? "editing" : "visualization";
        output.WriteLine($"Live {verb} of {displayPath}");
        return await ServeUntilShutdownAsync(server, url, noOpen, output, cancellationToken);
    }

    // Opens the URL (unless noOpen) and keeps serving until the web host shuts down — Ctrl+C (a
    // termination signal the host handles) or the command's cancellation token; completing normally
    // rather than throwing so shutdown is not an exceptional path.
    private async Task<int> ServeUntilShutdownAsync(
        ServedShellServer server, string url, bool noOpen, TextWriter output, CancellationToken cancellationToken)
    {
        output.WriteLine($"  {url}  (press Ctrl+C to stop)");
        if (!noOpen)
        {
            _browser.Open(url);
        }

        await server.WaitForShutdownAsync(cancellationToken);

        return 0;
    }
}
