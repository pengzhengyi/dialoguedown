using System.Net.Http.Json;
using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class ServeModeTests
{
    [Fact]
    public async Task RunAsync_BadDocument_ReturnsOne()
    {
        var error = new StringWriter();

        var code = await ServeMode.RunAsync(
            "/tmp/missing.dialogue.md",
            port: null,
            noOpen: true,
            renderRoot: null,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            new FakeBrowserLauncher(),
            new FakeHostConsent(allow: false),
            new StringWriter(),
            error,
            CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_OpensBrowserWithLoopbackUrl_AndStopsOnCancellation()
    {
        using var doc = new TempDocument();
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        var url = Assert.Single(browser.Opened);
        Assert.StartsWith("http://127.0.0.1:", url);

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    [Fact]
    public async Task RunAsync_HotReloadsWhenTheDocumentChanges()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var token = timeout.Token;
        using var doc = new TempDocument("# First");
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        using var response = await client.GetAsync(
            "/api/events", HttpCompletionOption.ResponseHeadersRead, token);
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);

        await Task.Delay(300, token); // let the SSE subscription settle
        File.WriteAllText(doc.Path, "# Changed");

        var sawChange = false;
        string? line;
        while ((line = await reader.ReadLineAsync(token)) is not null)
        {
            if (line.Contains("# Changed", StringComparison.Ordinal))
            {
                sawChange = true;
                break;
            }
        }

        Assert.True(sawChange);

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_ImageOutsideFolder_WithRenderRoot_ServesReportAtSubPathWithoutPrompting()
    {
        using var tree = new TempTree();
        var documentPath = tree.File(
            "proj/scene.dialogue.md",
            """
            # Scene

            ![p](../shared/pic.png)
            """);
        var pic = tree.File("shared/pic.png");
        await File.WriteAllBytesAsync(pic, new byte[] { 4, 2 });
        var browser = new FakeBrowserLauncher();
        var consent = new FakeHostConsent(allow: false);
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            documentPath, port: 0, noOpen: false, renderRoot: tree.Root, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, consent,
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        Assert.False(consent.WasAsked); // explicit render root skips the prompt
        Assert.EndsWith("/proj/", browser.Opened[0]);
        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        var response = await client.GetAsync("/shared/pic.png");
        Assert.True(response.IsSuccessStatusCode);

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_RenderRootNotContainingDocument_ReturnsOneWithoutServing()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("proj/scene.dialogue.md", "# Scene");
        var elsewhere = tree.Dir("elsewhere");
        var browser = new FakeBrowserLauncher();
        var error = new StringWriter();

        var code = await ServeMode.RunAsync(
            documentPath, port: 0, noOpen: false, renderRoot: elsewhere, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser,
            new FakeHostConsent(allow: false), new StringWriter(), error, CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("render root", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(browser.Opened);
    }

    [Fact]
    public async Task RunAsync_ImageOutsideFolder_Declined_ServesDocumentFolderOnly()
    {
        using var tree = new TempTree();
        var documentPath = tree.File(
            "proj/scene.dialogue.md",
            """
            # Scene

            ![p](../shared/pic.png)
            """);
        tree.File("shared/pic.png");
        var consent = new FakeHostConsent(allow: false);
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            documentPath, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, consent,
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        Assert.True(consent.WasAsked);
        Assert.EndsWith("/", browser.Opened[0]);
        Assert.DoesNotContain("/proj/", browser.Opened[0]);
        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        var response = await client.GetAsync("/shared/pic.png");
        Assert.False(response.IsSuccessStatusCode);

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_EditMode_SaveWritesTheBufferAndReturnsStages()
    {
        using var doc = new TempDocument("# First");
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token, mode: VisualizationMode.Edit);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        using var response = await client.PostAsJsonAsync("/api/save", new { source = "# Saved\n\nAlice: Hi", expectedBaseline = "# First" });

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("# Saved\n\nAlice: Hi", await File.ReadAllTextAsync(doc.Path));
        Assert.Contains("\"stages\":[", await response.Content.ReadAsStringAsync());

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_ViewMode_AlsoServesTheSaveRoute()
    {
        // A served session always exposes /api/save (one server for View and Edit); the
        // client just never calls it in View. Starting in View, a save still succeeds.
        using var doc = new TempDocument("# First");
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token, mode: VisualizationMode.View);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        using var response = await client.PostAsJsonAsync("/api/save", new { source = "# Saved", expectedBaseline = "# First" });

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("# Saved", await File.ReadAllTextAsync(doc.Path));

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_EditMode_NonexistentScript_CreatesItEmptyAndServesEdit()
    {
        using var tree = new TempTree();
        var newPath = Path.Combine(tree.Dir("proj"), "draft.dialogue.md");
        Assert.False(File.Exists(newPath));
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            newPath, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token, mode: VisualizationMode.Edit);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        Assert.True(File.Exists(newPath));
        Assert.Equal(string.Empty, await File.ReadAllTextAsync(newPath));
        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        Assert.Contains("\"mode\":\"edit\"", await client.GetStringAsync("/"));

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_ViewMode_NonexistentScript_DoesNotCreateAndReturnsOne()
    {
        using var tree = new TempTree();
        var newPath = Path.Combine(tree.Dir("proj"), "draft.dialogue.md");
        var error = new StringWriter();

        var code = await ServeMode.RunAsync(
            newPath, port: 0, noOpen: true, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), new FakeBrowserLauncher(),
            new FakeHostConsent(allow: false), new StringWriter(), error, CancellationToken.None,
            mode: VisualizationMode.View);

        Assert.Equal(1, code);
        Assert.False(File.Exists(newPath));
        Assert.Contains("not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_EditMode_SaveThroughSymlink_WritesTheRealTargetAndKeepsTheLink()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# First");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Symlinks.Create(link, real);
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        // Launch the session on the link; saves and the watcher must target the real file.
        var task = ServeMode.RunAsync(
            link, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token, mode: VisualizationMode.Edit);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        using var response = await client.PostAsJsonAsync(
            "/api/save", new { source = "# Saved\n\nAlice: Hi", expectedBaseline = "# First" });

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("# Saved\n\nAlice: Hi", await File.ReadAllTextAsync(real)); // the real target
        Assert.NotNull(new FileInfo(link).LinkTarget); // the link entry is preserved

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_BrokenSymlink_ReturnsOne()
    {
        using var tree = new TempTree();
        var link = Path.Combine(tree.Root, "broken.dialogue.md");
        Symlinks.Create(link, Path.Combine(tree.Root, "gone.dialogue.md"));
        var error = new StringWriter();

        var code = await ServeMode.RunAsync(
            link, port: 0, noOpen: true, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), new FakeBrowserLauncher(),
            new FakeHostConsent(allow: false), new StringWriter(), error, CancellationToken.None);

        Assert.Equal(1, code);
        Assert.NotEmpty(error.ToString()); // a broken link is refused with an explanation, never served
    }

    [Fact]
    public async Task RunAsync_WithAnOpenEventStream_StillStopsPromptlyOnCancellation()
    {
        // A held-open hot-reload stream is a long-lived in-flight request. Graceful shutdown
        // must not wait on it until the host's timeout — the stream observes the server
        // shutting down and ends, so Ctrl+C stops the session promptly even with a live client.
        using var doc = new TempDocument("# Scene");
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = ServeMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        using var response = await client.GetAsync(
            "/api/events", HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        stop.Cancel();
        var code = await task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(0, code);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        if (!condition())
        {
            throw new TimeoutException("Condition was not met in time.");
        }
    }
}
