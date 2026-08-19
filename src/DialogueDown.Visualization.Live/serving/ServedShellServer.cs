using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Browsing;
using DialogueDown.Visualization.Live.Configuration;
using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Render;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;

namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// The loopback server behind the served shell. It serves the empty shell at <c>/</c>, browses the
/// launch root for <c>.dialogue.md</c> sources, and on open swaps in a live session for
/// the chosen source — served once (static) or watched — under <c>/r/</c>. Browsing and
/// serving stay confined to the launch root (see <see cref="BrowseRoot"/>); the report is
/// mounted under <c>/r/</c> so it never collides with the empty shell at <c>/</c>.
/// </summary>
internal sealed class ServedShellServer : IAsyncDisposable
{
    private const string ReportMount = "/r";

    private readonly WebApplication _app;
    private readonly BrowseRoot _root;
    private readonly string _emptyShellHtml;
    private readonly Func<string, string, string?, LiveSession> _sessionFactory;
    private readonly object _gate = new();
    private ActiveDocument? _active;

    // True once the run pinned an initial document (a served `visualize <script>`): the landing then
    // redirects to that report. A browse-only run (no initial script) leaves this false, so the
    // landing stays the empty shell even after a script is opened from the Explorer.
    private bool _landingRedirectsToReport;

    /// <summary>
    /// Builds the served-shell server for <paramref name="root"/> on the given loopback port
    /// (0 = ephemeral), serving <paramref name="emptyShellHtml"/> at <c>/</c>.
    /// </summary>
    public ServedShellServer(
        BrowseRoot root,
        string emptyShellHtml,
        int port = 0,
        Func<string, string, string?, LiveSession>? sessionFactory = null)
    {
        _root = root;
        _emptyShellHtml = emptyShellHtml;
        _sessionFactory = sessionFactory ?? ((path, mode, displayPath) => new LiveSession(path, mode, displayPath: displayPath));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders();
        builder.AddLoopbackCompression();
        _app = builder.Build();
        Configure(_app);
    }

    /// <summary>The base URL the server is listening on (valid after <see cref="StartAsync"/>).</summary>
    public string BaseUrl =>
        _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

    /// <summary>Starts listening.</summary>
    public Task StartAsync() => _app.StartAsync();

    /// <summary>
    /// Blocks until the web host shuts down — either a Ctrl+C / termination signal that
    /// the host's own console lifetime handles, or <paramref name="cancellationToken"/>
    /// (the command's token) — then stops the host. Returning is the signal to dispose.
    /// </summary>
    public Task WaitForShutdownAsync(CancellationToken cancellationToken) =>
        _app.WaitForShutdownAsync(cancellationToken);

    /// <summary>
    /// Activates the run's initial document — the script a served <c>visualize &lt;script&gt;</c>
    /// starts on — and returns its report URL path under the <c>/r</c> mount, so the caller can
    /// open the browser straight on the report rather than the empty shell. <paramref name="displayPath"/>
    /// is the launched path shown to the reader (a symlink's link path) when it differs from the
    /// resolved <paramref name="documentPath"/>.
    /// </summary>
    public string StartInitialDocument(string documentPath, string mode, string? displayPath = null)
    {
        _landingRedirectsToReport = true;
        return ReportMount + Activate(documentPath, mode, displayPath);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _active?.Watcher?.Dispose();
            _active?.ConfigWatcher?.Dispose();
        }

        await _app.DisposeAsync();
    }

    private static bool TryParseMode(string? mode, out string parsed)
    {
        parsed = mode?.ToLowerInvariant() switch
        {
            null or "" or VisualizationMode.View => VisualizationMode.View,
            VisualizationMode.Edit => VisualizationMode.Edit,
            _ => string.Empty,
        };
        return parsed.Length != 0;
    }

    // Served HTML is per-session and rebuilt on every launch, so it must never be cached: a stale
    // report.html would render an old bundle against the live filesystem (wrong toggles, missing
    // features). The empty shell follows the same rule.
    private static IResult NoStoreHtml(HttpContext context, string html)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private void Configure(WebApplication app)
    {
        // Compress the large report pages; text/event-stream is not compressible, so the
        // SSE hot-reload stream passes through untouched.
        app.UseResponseCompression();

        // Assets for the active source resolve under the launch root at /r/... . Static
        // files runs before routing (explicit UseRouting below) so it serves an existing
        // asset even though the catch-all report route would also match its path.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(_root.RootDirectory),
            RequestPath = ReportMount,
        });
        app.UseRouting();

        app.MapGet("/", Root);
        app.MapGet("/api/browse", (string? path) => Browse(path ?? string.Empty));
        app.MapPost("/api/open", (OpenRequest request, HttpContext context) => Open(request, context));
        app.MapPost("/api/create", (CreateRequest request, HttpContext context) => Create(request, context));
        app.MapPost("/api/create-folder", (CreateFolderRequest request) => CreateFolder(request));
        app.MapPost("/api/rename", (RenameRequest request) => Rename(request));
        app.MapPost("/api/create-config", CreateConfig);
        app.MapGet("/api/document", Document);
        app.MapPost("/api/save", (SaveRequest request) => Save(request));
        app.MapPost("/api/reload", (ReloadRequest request) => Reload(request));
        app.MapGet("/api/events", HandleEventsAsync);
        app.MapGet(ReportMount, (HttpContext context) => Report(context, string.Empty));
        app.MapGet(ReportMount + "/{**path}", (HttpContext context, string? path) => Report(context, path ?? string.Empty));
    }

    private IResult Browse(string path)
    {
        var listing = _root.Browse(path);
        return listing is null ? Results.NotFound() : Results.Json(listing.Value);
    }

    // The landing. A run that pinned an initial document (served `visualize <script>`) redirects to
    // its report under the /r mount; a browse-only run stays on the empty shell — the Explorer over
    // the root and a create call to action — even after a script is opened from the tree.
    private IResult Root(HttpContext context)
    {
        if (_landingRedirectsToReport && Active() is { } active)
        {
            context.Response.Headers.Location = ReportMount + active.ReportPath;
            return Results.StatusCode(StatusCodes.Status303SeeOther);
        }

        return NoStoreHtml(context, _emptyShellHtml);
    }

    private IResult Open(OpenRequest request, HttpContext context)
    {
        var source = _root.ResolveSource(request.Source ?? string.Empty);
        if (source is null)
        {
            return Results.NotFound();
        }

        if (!TryParseMode(request.Mode, out var mode))
        {
            return Results.BadRequest(new { message = $"Unsupported mode: {request.Mode}" });
        }

        return StartSession(source, mode, context);
    }

    // Creates a new, empty script at a root-confined path and opens it in Edit. A name that
    // already exists is a conflict (409) — the file is left untouched, so the client can offer
    // to open it instead — and is never overwritten.
    private IResult Create(CreateRequest request, HttpContext context)
    {
        var relativePath = request.Path ?? string.Empty;
        if (!relativePath.EndsWith(DocumentValidation.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new { message = $"A script name must end in '{DocumentValidation.Extension}'." });
        }

        var target = _root.Resolve(relativePath);
        if (target is null)
        {
            return Results.BadRequest(new { message = "The path is outside the launch root." });
        }

        if (!Directory.Exists(Path.GetDirectoryName(target)!))
        {
            return Results.BadRequest(new { message = "The containing folder does not exist." });
        }

        if (File.Exists(target))
        {
            return Results.Conflict(
                new { message = "A file with that name already exists.", path = relativePath });
        }

        File.WriteAllText(target, string.Empty);
        return StartSession(target, VisualizationMode.Edit, context);
    }

    // Creates a new, empty folder at a root-confined path. A name that already exists (as a file or
    // folder) is a conflict (409); an escape or a missing parent is a 400.
    private IResult CreateFolder(CreateFolderRequest request)
    {
        var relativePath = request.Path ?? string.Empty;
        var target = _root.Resolve(relativePath);
        if (target is null || relativePath.Length == 0)
        {
            return Results.BadRequest(new { message = "The folder path is outside the launch root." });
        }

        if (!Directory.Exists(Path.GetDirectoryName(target)!))
        {
            return Results.BadRequest(new { message = "The containing folder does not exist." });
        }

        if (Directory.Exists(target) || File.Exists(target))
        {
            return Results.Conflict(new { message = "A file or folder with that name already exists." });
        }

        Directory.CreateDirectory(target);
        return Results.Ok(new { path = relativePath });
    }

    // Starts a served session for an existing source path and redirects to its report. Shared
    // by Open (an existing script) and Create (a freshly written one).
    private IResult StartSession(string source, string mode, HttpContext context)
    {
        var reportPath = Activate(source, mode);
        context.Response.Headers.Location = ReportMount + reportPath;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

    // Replaces the active document with a fresh session for a root-confined absolute path and
    // returns its report sub-path. Shared by the HTTP open/create routes and the initial document
    // a served run may start on.
    private string Activate(string documentPath, string mode, string? displayPath = null)
    {
        var sourceDirectory = Path.GetDirectoryName(documentPath)!;
        var reportPath = ServeRoot.For(_root.RootDirectory, sourceDirectory).ReportPath;
        var session = _sessionFactory(documentPath, mode, displayPath);
        // The Explorer sidebar needs the project root and this script's place in it; the shell
        // always serves within a root, so every served report is project-aware.
        session.Project = new ReportProject(_root.RootDirectory, _root.Relativize(documentPath));
        // A served session always watches the file: View hot-reloads the report, Edit
        // surfaces a passive "changed on disk" chip.
        var watcher = new DocumentWatcher(documentPath, session.Refresh);
        // A session that already applies a config watches it too, so external config edits reload.
        var configWatcher = session.ConfigPath is { } configPath
            ? new DocumentWatcher(configPath, session.RefreshConfig)
            : null;

        lock (_gate)
        {
            _active?.Watcher?.Dispose();
            _active?.ConfigWatcher?.Dispose();
            _active = new ActiveDocument(session, reportPath, watcher)
            {
                ConfigWatcher = configWatcher,
            };
        }

        return reportPath;
    }

    private IResult Report(HttpContext context, string path)
    {
        var active = Active();
        if (active is null || path.Trim('/') != active.ReportRelative)
        {
            return Results.NotFound();
        }

        return NoStoreHtml(context, active.Session.RenderInitialHtml());
    }

    // Renames (moves) a root-confined script or folder to a new root-relative path. A script keeps
    // its `.dialogue.md` extension; a folder has no extension rule. A name already in use is a
    // conflict (409). When the move carries the document on screen — the file itself, or a file
    // inside a renamed folder — its watcher is dropped (so it does not fire a spurious "deleted")
    // and the reply carries the document's new path so the client reopens it.
    private IResult Rename(RenameRequest request)
    {
        var fromRelative = request.From ?? string.Empty;
        var toRelative = request.To ?? string.Empty;
        var from = _root.Resolve(fromRelative);
        var to = _root.Resolve(toRelative);
        if (from is null || to is null || fromRelative.Length == 0 || toRelative.Length == 0)
        {
            return Results.BadRequest(new { message = "The path is outside the launch root." });
        }

        var isFolder = Directory.Exists(from);
        if (!isFolder && !File.Exists(from))
        {
            return Results.BadRequest(new { message = "The file or folder no longer exists." });
        }

        if (!isFolder
            && !toRelative.EndsWith(DocumentValidation.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new { message = $"A script name must end in '{DocumentValidation.Extension}'." });
        }

        if (!Directory.Exists(Path.GetDirectoryName(to)!))
        {
            return Results.BadRequest(new { message = "The containing folder does not exist." });
        }

        if (File.Exists(to) || Directory.Exists(to))
        {
            return Results.Conflict(
                new { message = "A file or folder with that name already exists.", path = toRelative });
        }

        string? activePath = null;
        lock (_gate)
        {
            if (_active is { } current)
            {
                var activeRelative = _root.Relativize(current.Session.DocumentPath);
                if (string.Equals(activeRelative, fromRelative, StringComparison.Ordinal))
                {
                    activePath = toRelative; // the active file itself was renamed
                }
                else if (activeRelative.StartsWith(fromRelative + "/", StringComparison.Ordinal))
                {
                    // the active file lives inside the renamed folder
                    activePath = toRelative + activeRelative[fromRelative.Length..];
                }

                if (activePath is not null)
                {
                    current.Watcher?.Dispose();
                }
            }
        }

        if (isFolder)
        {
            Directory.Move(from, to);
        }
        else
        {
            File.Move(from, to);
        }

        return Results.Ok(new { path = toRelative, active = activePath is not null, activePath });
    }

    private IResult Document()
    {
        var active = Active();
        return active is null
            ? Results.NotFound()
            : Results.Content(active.Session.CurrentDocumentJson(), "application/json; charset=utf-8");
    }

    // Applies the posted save request to the active document (dialogue or its dialogue.toml)
    // and returns the typed-outcome payload. A served session always accepts this (the client
    // only calls it in Edit); there is just nothing active before a script is opened.
    private IResult Save(SaveRequest request)
    {
        var active = Active();
        if (active is null)
        {
            return Results.NotFound();
        }

        try
        {
            var json = active.Session.Save(
                new SaveInput(
                    request.Source,
                    request.Target,
                    request.ExpectedBaseline,
                    request.Validation,
                    request.Conflict));
            return Results.Content(json, "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // Reloads the active document or its configuration from disk (a conflict/uncertain recovery).
    private IResult Reload(ReloadRequest request)
    {
        var active = Active();
        if (active is null)
        {
            return Results.NotFound();
        }

        try
        {
            return Results.Content(
                active.Session.Reload(request.Target), "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // Creates a dialogue.toml at the launch root for an active session that has none, then
    // returns the recompiled payload. The path is composed server-side from the launch root —
    // never from the request — so no request value reaches the filesystem. An existing file is
    // a conflict (409), left untouched; a write failure is 400.
    private IResult CreateConfig()
    {
        var active = Active();
        if (active is null)
        {
            return Results.NotFound();
        }

        var configPath = Path.Combine(_root.RootDirectory, ConfigurationFile.DefaultName);
        try
        {
            // The exclusive create in LiveSession decides create/adopt/conflict atomically, so
            // there is no File.Exists check to race here. A create, an idempotent adoption, or a
            // differing pre-existing file adopted as recovery (AdoptedExisting) starts the config
            // watcher and returns 200; only a retry of an already-adopted file that diverged is a
            // conflict (409), left untouched.
            var result = active.Session.CreateConfig(configPath);
            if (result.Status == CreateConfigStatus.Conflict)
            {
                return Results.Conflict(new { message = result.Payload });
            }

            StartConfigWatcher(active, configPath);
            return Results.Content(result.Payload, "application/json; charset=utf-8");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // Starts (or replaces) the watcher for the active document's newly created config so external
    // edits to it hot-reload, unless the active document has since been swapped out.
    private void StartConfigWatcher(ActiveDocument active, string configPath)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_active, active))
            {
                return;
            }

            active.ConfigWatcher?.Dispose();
            active.ConfigWatcher = new DocumentWatcher(configPath, active.Session.RefreshConfig);
        }
    }

    private async Task HandleEventsAsync(
        HttpContext context, IHostApplicationLifetime lifetime, CancellationToken cancellationToken)
    {
        var active = Active();
        if (active is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        using var subscription = active.Session.Broadcaster.Subscribe(out var reader);
        await context.Response.Body.FlushAsync(cancellationToken);

        // End the stream on a client disconnect (`cancellationToken` is RequestAborted) OR
        // when the host begins shutting down (Ctrl+C). Observing ApplicationStopping keeps a
        // live stream from holding graceful shutdown open until the host's timeout.
        using var streaming = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, lifetime.ApplicationStopping);
        var streamToken = streaming.Token;

        try
        {
            await foreach (var liveEvent in reader.ReadAllAsync(streamToken))
            {
                await context.Response.WriteAsync($"event: {liveEvent.Event}\n", streamToken);
                await context.Response.WriteAsync($"data: {liveEvent.Data}\n\n", streamToken);
                await context.Response.Body.FlushAsync(streamToken);
            }
        }
        catch (OperationCanceledException)
        {
            // A normal client disconnect or a server shutdown; the `using` above cleans up.
        }
    }

    private ActiveDocument? Active()
    {
        lock (_gate)
        {
            return _active;
        }
    }

    private sealed record ActiveDocument(LiveSession Session, string ReportPath, DocumentWatcher? Watcher)
    {
        /// <summary>The watcher for this document's <c>dialogue.toml</c>, once one is created.</summary>
        public DocumentWatcher? ConfigWatcher { get; set; }

        /// <summary>The report path with its surrounding slashes stripped, to match the <c>/r/{**path}</c> route.</summary>
        public string ReportRelative => ReportPath.Trim('/');
    }

    private sealed record OpenRequest(string? Source, string? Mode);

    private sealed record CreateRequest(string? Path);

    private sealed record CreateFolderRequest(string? Path);

    private sealed record RenameRequest(string? From, string? To);

    private sealed record SaveRequest(
        string? Source,
        string? Target = null,
        string? ExpectedBaseline = null,
        string? Validation = null,
        string? Conflict = null);

    private sealed record ReloadRequest(string? Target = null);
}
