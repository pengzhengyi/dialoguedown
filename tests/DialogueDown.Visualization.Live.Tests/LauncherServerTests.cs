using System.Net;
using System.Net.Http.Json;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LauncherServerTests
{
    private const string LandingHtml = "<!doctype html><title>Shell</title>";

    [Fact]
    public async Task Root_ServesTheLandingHtml()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        var html = await client.GetStringAsync("/");

        Assert.Equal(LandingHtml, html);
    }

    [Fact]
    public async Task Browse_ListsSubdirectoriesAndDialogueSources()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md", "# A");
        tree.File("root/notes.md");
        tree.Dir("root/proj");
        await using var server = await Started(tree);
        using var client = Client(server);

        var json = await client.GetStringAsync("/api/browse?path=");

        Assert.Contains("\"directories\":[\"proj\"]", json);
        Assert.Contains("\"sources\":[\"a.dialogue.md\"]", json);
        Assert.DoesNotContain("notes.md", json);
    }

    [Fact]
    public async Task Browse_OutsideRoot_NotFound()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.GetAsync("/api/browse?path=../");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Open_ValidSource_RedirectsToReportAndServesIt()
    {
        using var tree = new TempTree();
        tree.File("root/proj/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await client.PostAsJsonAsync(
            "/api/open", new { source = "proj/scene.dialogue.md", mode = "view" });

        Assert.Equal(HttpStatusCode.SeeOther, open.StatusCode);
        Assert.Equal("/r/proj/", open.Headers.Location!.ToString());

        var html = await client.GetStringAsync("/r/proj/");
        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"view\"", html);
    }

    [Fact]
    public async Task Open_ServesReportCarryingTheProjectContext()
    {
        using var tree = new TempTree();
        tree.File("root/proj/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        await client.PostAsJsonAsync(
            "/api/open", new { source = "proj/scene.dialogue.md", mode = "view" });

        // The launcher always serves within a root, so the report carries the project context the
        // Explorer sidebar renders: the active script's root-relative path (and the root itself).
        var html = await client.GetStringAsync("/r/proj/");
        Assert.Contains("\"project\":{", html);
        Assert.Contains("\"activePath\":\"proj/scene.dialogue.md\"", html);
    }

    [Fact]
    public async Task CreateFolder_MakesADirectoryVisibleInBrowse()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md", "# A");
        await using var server = await Started(tree);
        using var client = Client(server);

        var created = await client.PostAsJsonAsync("/api/create-folder", new { path = "act-2" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var json = await client.GetStringAsync("/api/browse?path=");
        Assert.Contains("\"directories\":[\"act-2\"]", json);
    }

    [Fact]
    public async Task CreateFolder_OutsideRoot_BadRequest()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.PostAsJsonAsync("/api/create-folder", new { path = "../escape" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_IsServedWithNoStore()
    {
        using var tree = new TempTree();
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        await client.PostAsJsonAsync("/api/open", new { source = "scene.dialogue.md", mode = "view" });
        var report = await client.GetAsync("/r/");

        // Per-session HTML is rebuilt each launch, so it must not be cached (no stale reports).
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(report.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task Rename_MovesAScriptToItsNewName()
    {
        using var tree = new TempTree();
        tree.File("root/act-1/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server);

        var renamed = await client.PostAsJsonAsync(
            "/api/rename",
            new { from = "act-1/scene.dialogue.md", to = "act-1/prologue.dialogue.md" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        var json = await client.GetStringAsync("/api/browse?path=act-1");
        Assert.Contains("prologue.dialogue.md", json);
        Assert.DoesNotContain("scene.dialogue.md", json);
    }

    [Fact]
    public async Task Rename_ToAnExistingName_Conflict()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md", "# A");
        tree.File("root/b.dialogue.md", "# B");
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.PostAsJsonAsync(
            "/api/rename", new { from = "a.dialogue.md", to = "b.dialogue.md" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Rename_OutsideRoot_BadRequest()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md", "# A");
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.PostAsJsonAsync(
            "/api/rename", new { from = "a.dialogue.md", to = "../escape.dialogue.md" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rename_MovesAFolderAndItsContents()
    {
        using var tree = new TempTree();
        tree.File("root/act-1/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server);

        var renamed = await client.PostAsJsonAsync("/api/rename", new { from = "act-1", to = "act-one" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        Assert.Contains("\"directories\":[\"act-one\"]", await client.GetStringAsync("/api/browse?path="));
        Assert.Contains(
            "act-one/scene.dialogue.md", await client.GetStringAsync("/api/browse?path=act-one"));
    }

    [Fact]
    public async Task Rename_FolderOntoAnExistingName_Conflict()
    {
        using var tree = new TempTree();
        tree.Dir("root/act-1");
        tree.Dir("root/act-2");
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.PostAsJsonAsync("/api/rename", new { from = "act-1", to = "act-2" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Open_ViewMode_ServesReportInViewMode()
    {
        using var tree = new TempTree();
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await client.PostAsJsonAsync(
            "/api/open", new { source = "scene.dialogue.md", mode = "view" });

        Assert.Equal("/r/", open.Headers.Location!.ToString());
        Assert.Contains("\"mode\":\"view\"", await client.GetStringAsync("/r/"));
    }

    [Fact]
    public async Task Save_AfterOpeningEdit_WritesTheDocumentAndReturnsStages()
    {
        using var tree = new TempTree();
        var path = tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);
        await client.PostAsJsonAsync("/api/open", new { source = "scene.dialogue.md", mode = "edit" });

        var save = await client.PostAsJsonAsync(
            "/api/save",
            new { source = "# Edited\n", expectedBaseline = "# Scene" });

        Assert.True(save.IsSuccessStatusCode);
        var json = await save.Content.ReadAsStringAsync();
        Assert.Contains("\"stages\":", json);
        Assert.Equal("# Edited\n", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Save_BeforeOpeningAnything_NotFound()
    {
        // A served session always exposes /api/save, but there is nothing to write to
        // until a script is opened.
        using var tree = new TempTree();
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var save = await client.PostAsJsonAsync("/api/save", new { source = "# Nope\n" });

        Assert.Equal(HttpStatusCode.NotFound, save.StatusCode);
    }

    [Fact]
    public async Task Open_NonDialogueSource_NotFound()
    {
        using var tree = new TempTree();
        tree.File("root/notes.md");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await client.PostAsJsonAsync("/api/open", new { source = "notes.md", mode = "view" });

        Assert.Equal(HttpStatusCode.NotFound, open.StatusCode);
    }

    [Fact]
    public async Task Report_BeforeOpen_NotFound()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/r/")).StatusCode);
    }

    [Fact]
    public async Task OpenedSource_ServesRelativeAssetsUnderReportMount()
    {
        using var tree = new TempTree();
        tree.File("root/proj/scene.dialogue.md", "# Scene\n\n![pic](art/pic.png)");
        await File.WriteAllBytesAsync(tree.File("root/proj/art/pic.png"), [1, 2, 3, 4]);
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        await client.PostAsJsonAsync("/api/open", new { source = "proj/scene.dialogue.md", mode = "view" });
        var asset = await client.GetAsync("/r/proj/art/pic.png");

        Assert.True(asset.IsSuccessStatusCode);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await asset.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Create_NewName_WritesAnEmptyScriptAndOpensItInEdit()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var create = await client.PostAsJsonAsync("/api/create", new { path = "draft.dialogue.md" });

        Assert.Equal(HttpStatusCode.SeeOther, create.StatusCode);
        Assert.Equal("/r/", create.Headers.Location!.ToString());
        var created = Path.Combine(tree.Dir("root"), "draft.dialogue.md");
        Assert.True(File.Exists(created));
        Assert.Equal(string.Empty, await File.ReadAllTextAsync(created));

        var html = await client.GetStringAsync("/r/");
        Assert.Contains("\"mode\":\"edit\"", html);
    }

    [Fact]
    public async Task Create_ExistingName_ConflictsAndLeavesTheFileUntouched()
    {
        using var tree = new TempTree();
        tree.File("root/scene.dialogue.md", "# Keep me");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var create = await client.PostAsJsonAsync("/api/create", new { path = "scene.dialogue.md" });

        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
        Assert.Contains("scene.dialogue.md", await create.Content.ReadAsStringAsync());
        Assert.Equal(
            "# Keep me",
            await File.ReadAllTextAsync(Path.Combine(tree.Dir("root"), "scene.dialogue.md")));
    }

    [Fact]
    public async Task Create_NonDialogueName_BadRequestAndWritesNothing()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var create = await client.PostAsJsonAsync("/api/create", new { path = "notes.md" });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.False(File.Exists(Path.Combine(tree.Dir("root"), "notes.md")));
    }

    [Fact]
    public async Task Create_OutsideRoot_BadRequest()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var create = await client.PostAsJsonAsync("/api/create", new { path = "../escape.dialogue.md" });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Create_InAMissingFolder_BadRequest()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var create = await client.PostAsJsonAsync(
            "/api/create", new { path = "nope/draft.dialogue.md" });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task CreateConfig_AfterOpeningEdit_WritesADialogueTomlAtTheLaunchRoot()
    {
        using var tree = new TempTree();
        var root = tree.Dir("root");
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);
        await client.PostAsJsonAsync("/api/open", new { source = "scene.dialogue.md", mode = "edit" });

        var create = await client.PostAsync("/api/create-config", content: null);

        Assert.True(create.IsSuccessStatusCode);
        Assert.True(File.Exists(Path.Combine(root, "dialogue.toml"))); // created at the launch root
        Assert.Contains("dialogue.toml", await create.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Save_WithConfigTarget_AfterCreatingConfig_WritesTheDialogueToml()
    {
        using var tree = new TempTree();
        var root = tree.Dir("root");
        tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);
        await client.PostAsJsonAsync("/api/open", new { source = "scene.dialogue.md", mode = "edit" });
        await client.PostAsync("/api/create-config", content: null); // adopt a config

        var save = await client.PostAsJsonAsync(
            "/api/save",
            new
            {
                source = "[[speakers]]\nname = \"Bob\"\nid = \"B\"\n",
                target = "config",
                conflict = "overwrite",
            });

        Assert.True(save.IsSuccessStatusCode);
        Assert.Contains("Bob", await File.ReadAllTextAsync(Path.Combine(root, "dialogue.toml")));
        Assert.Contains("\"name\":\"Bob\"", await save.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StartInitialDocument_RootRedirectsToTheReport()
    {
        using var tree = new TempTree();
        var scriptPath = tree.File("root/proj/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var reportPath = server.StartInitialDocument(scriptPath, "view");

        // The initial document (a served visualize <script>) is hosted under the /r mount, and the
        // landing redirects to it rather than showing the empty shell.
        Assert.Equal("/r/proj/", reportPath);
        var landing = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.SeeOther, landing.StatusCode);
        Assert.Equal("/r/proj/", landing.Headers.Location!.ToString());

        var html = await client.GetStringAsync("/r/proj/");
        Assert.Contains("\"activePath\":\"proj/scene.dialogue.md\"", html);
    }

    [Fact]
    public async Task WaitForShutdownAsync_WhenCanceled_StopsTheServer()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        var baseUrl = server.BaseUrl;

        using var stop = new CancellationTokenSource();
        var shutdown = server.WaitForShutdownAsync(stop.Token);
        Assert.False(shutdown.IsCompleted); // keeps serving until asked to stop

        stop.Cancel();
        await shutdown.WaitAsync(TimeSpan.FromSeconds(15)); // returns only once the host has stopped

        using var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(3),
        };
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync("/"));
    }

    private static async Task<LauncherServer> Started(TempTree tree)
    {
        var server = new LauncherServer(LaunchRoot.At(tree.Dir("root")), LandingHtml);
        await server.StartAsync();
        return server;
    }

    private static HttpClient Client(LauncherServer server, bool followRedirects = true)
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = followRedirects };
        return new HttpClient(handler) { BaseAddress = new Uri(server.BaseUrl) };
    }
}
