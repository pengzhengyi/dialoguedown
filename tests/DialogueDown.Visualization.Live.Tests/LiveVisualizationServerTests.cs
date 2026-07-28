using System.Net;
using System.Net.Http.Json;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveVisualizationServerTests
{
    [Fact]
    public async Task Root_ServesTheLiveReportHtml()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var html = await client.GetStringAsync("/");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"view\"", html);
    }

    [Fact]
    public async Task ServesFilesAlongsideTheDocument_SoRelativeImagesResolve()
    {
        using var doc = new TempDocument("# Scene\n\n![pic](assets/pic.png)");
        var assetsDir = Path.Combine(Path.GetDirectoryName(doc.Path)!, "assets");
        Directory.CreateDirectory(assetsDir);
        var pic = Path.Combine(assetsDir, "pic.png");
        var bytes = new byte[] { 1, 2, 3, 4 };
        await File.WriteAllBytesAsync(pic, bytes);

        try
        {
            await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
            await server.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

            var response = await client.GetAsync("/assets/pic.png");

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            File.Delete(pic);
            Directory.Delete(assetsDir);
        }
    }

    [Fact]
    public async Task DocumentApi_ReturnsThePayloadJson()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var response = await client.GetAsync("/api/document");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"source\":\"# Scene\"", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public async Task Events_StreamsAReloadWhenTheSessionRefreshes()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = timeout.Token;
        using var doc = new TempDocument("# First");
        var session = new LiveSession(doc.Path);
        await using var server = new LiveVisualizationServer(session);
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        using var response = await client.GetAsync(
            "/api/events",
            HttpCompletionOption.ResponseHeadersRead,
            token);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);

        File.WriteAllText(doc.Path, "# Second");
        session.Refresh();

        var sawReload = false;
        var sawContent = false;
        string? line;
        while ((line = await reader.ReadLineAsync(token)) is not null)
        {
            if (line.Contains("event: reload", StringComparison.Ordinal))
            {
                sawReload = true;
            }

            if (line.Contains("# Second", StringComparison.Ordinal))
            {
                sawContent = true;
                break;
            }
        }

        Assert.True(sawReload);
        Assert.True(sawContent);
    }

    [Fact]
    public async Task Report_IsCompressed_WhenTheClientAcceptsEncoding()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("br, gzip");

        using var response = await client.GetAsync("/");

        // The report is a large self-contained page; compress it for the client.
        Assert.Contains(response.Content.Headers.ContentEncoding, e => e is "br" or "gzip");
    }

    [Fact]
    public async Task Events_AreNeverCompressed_SoTheStreamIsNotBuffered()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("br, gzip");

        using var response = await client.GetAsync(
            "/api/events",
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);

        // text/event-stream must pass through uncompressed or the compressor would
        // buffer hot-reload events instead of streaming them.
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task DefaultRoot_ReportUrlIsTheServerRoot()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();

        Assert.Equal(server.BaseUrl.TrimEnd('/') + "/", server.ReportUrl);
    }

    [Fact]
    public async Task BroadenedRoot_ServesTheReportAtTheDocumentSubPath_AndRedirectsRoot()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("proj/scene.dialogue.md", "# Scene");
        var serveRoot = ServeRoot.For(tree.Root, Path.GetDirectoryName(documentPath)!);
        await using var server = new LiveVisualizationServer(
            new LiveSession(documentPath), serveRoot: serveRoot);
        await server.StartAsync();

        Assert.EndsWith("/proj/", server.ReportUrl);

        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        var report = await client.GetStringAsync("/proj/");
        Assert.StartsWith("<!doctype html", report, StringComparison.OrdinalIgnoreCase);

        using var noRedirect = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(server.BaseUrl),
        };
        var rootResponse = await noRedirect.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, rootResponse.StatusCode);
        Assert.Equal("/proj/", rootResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BroadenedRoot_ServesFilesOutsideTheDocumentFolder()
    {
        using var tree = new TempTree();
        var documentPath = tree.File(
            "proj/scene.dialogue.md",
            """
            # Scene

            ![p](../shared/pic.png)
            """);
        var pic = tree.File("shared/pic.png");
        var bytes = new byte[] { 9, 8, 7 };
        await File.WriteAllBytesAsync(pic, bytes);
        var serveRoot = ServeRoot.For(tree.Root, Path.GetDirectoryName(documentPath)!);
        await using var server = new LiveVisualizationServer(
            new LiveSession(documentPath), serveRoot: serveRoot);
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var response = await client.GetAsync("/shared/pic.png");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task CreateConfig_WritesADialogueTomlAtTheServeRoot_AndReturnsTheConfiguredPayload()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        await using var server = new LiveVisualizationServer(
            new LiveSession(documentPath, VisualizationMode.Edit));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var response = await client.PostAsync("/api/create-config", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(File.Exists(Path.Combine(tree.Root, "dialogue.toml"))); // created at the serve root
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("dialogue.toml", json); // the payload now carries the configuration file
    }

    [Fact]
    public async Task CreateConfig_WhenADifferentConfigExists_AdoptsItAndReturnsTheConfiguredPayload()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var existing = tree.File("dialogue.toml", "# hand-written\n");
        await using var server = new LiveVisualizationServer(
            new LiveSession(documentPath, VisualizationMode.Edit));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        var response = await client.PostAsync("/api/create-config", content: null);

        // A config-less session recovers into the existing file: it is adopted (200), left
        // untouched, and the payload carries the configuration so the reloaded page opens it.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("# hand-written\n", File.ReadAllText(existing)); // left untouched
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("dialogue.toml", json);
        Assert.Contains("\"outcome\":\"adopted\"", json);
    }

    [Fact]
    public async Task DocumentApi_AfterSavedInvalidConfig_CarriesTheSavedInvalidState()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        await using var server = new LiveVisualizationServer(
            new LiveSession(documentPath, VisualizationMode.Edit));
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };

        // Adopt a starter config, then persist invalid TOML with an explicit (allow-invalid) save.
        await client.PostAsync("/api/create-config", content: null);
        var broken = "[[speakers]]\nbogus = true\n";
        var save = await client.PostAsJsonAsync(
            "/api/save",
            new
            {
                source = broken,
                target = "config",
                expectedBaseline = ConfigStarter.Template,
                validation = "allow-invalid",
            });
        Assert.Contains("\"outcome\":\"saved-invalid\"", await save.Content.ReadAsStringAsync());

        // A page reload re-fetches the document payload and the initial HTML; both must announce the
        // saved-invalid state so the controller reinstates it instead of the last valid text.
        var json = await client.GetStringAsync("/api/document");
        Assert.Contains("\"configStatus\":\"saved-invalid\"", json);
        Assert.Contains("bogus", json);

        var html = await client.GetStringAsync("/");
        Assert.Contains("\"configStatus\":\"saved-invalid\"", html);
    }

    [Fact]
    public async Task WaitForShutdownAsync_WhenCanceled_StopsTheServer()
    {
        using var doc = new TempDocument("# Scene");
        await using var server = new LiveVisualizationServer(new LiveSession(doc.Path));
        await server.StartAsync();
        var baseUrl = server.BaseUrl;

        using var stop = new CancellationTokenSource();
        var shutdown = server.WaitForShutdownAsync(stop.Token);
        Assert.False(shutdown.IsCompleted); // keeps serving until asked to stop

        stop.Cancel();
        await shutdown.WaitAsync(TimeSpan.FromSeconds(15)); // returns only once the host has stopped

        // The host is stopped, so the loopback port no longer accepts requests.
        using var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(3),
        };
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync("/"));
    }
}
