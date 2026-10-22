using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Serving;
using DialogueDown.Visualization.Live.Tests.Support;
using static DialogueDown.TestSupport.OutputAssert;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class ServedShellRunnerTests
{
    // A deadline, not a wait: reached only when the runner never opens anything, which turns a
    // hang into a failure that says so.
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RunAsync_InvalidRoot_ReturnsOne()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}");
        var error = new StringWriter();
        var runner = new ServedShellRunner(new FakeBrowserLauncher());

        var code = await runner.RunAsync(
            script: null,
            root: missing,
            ReportMode.View,
            port: null,
            noOpen: true,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            new StringWriter(),
            error,
            CancellationToken.None);

        Assert.Equal(1, code);
        AssertReads(error.ToString(), $"Serve root is not a directory: {missing}");
    }

    [Fact]
    public async Task RunAsync_NoScript_ServesTheEmptyShell_AndStopsOnCancellation()
    {
        using var tree = new TempTree();
        tree.File("scene.dialogue.md", "# Scene");
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = Serve(browser, script: null, tree.Root, stop.Token);
        await browser.FirstOpened.WaitAsync(_patience, TestContext.Current.CancellationToken);

        var url = Assert.Single(browser.Opened);
        Assert.StartsWith("http://127.0.0.1:", url);

        // The landing page is the report shell with no script open, carrying the project payload
        // its file explorer lists.
        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var landing = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
        Assert.StartsWith("<!doctype html", landing, StringComparison.OrdinalIgnoreCase);
        var shell = ReportPayload.FromPage(landing);
        Assert.Equal(tree.Root, shell.ProjectRoot);
        Assert.Null(shell.ActivePath);

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    [Fact]
    public async Task RunAsync_WithAScript_OpensItsReportUnderTheReportMount()
    {
        using var tree = new TempTree();
        var scriptPath = tree.File("scene.dialogue.md", """
            # Scene

            Alice: Hi.

            """);
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = Serve(browser, scriptPath, tree.Root, stop.Token);
        await browser.FirstOpened.WaitAsync(_patience, TestContext.Current.CancellationToken);

        // A script opens directly on its report under the /r mount, and that report carries the
        // active document (its project payload names the script).
        var url = Assert.Single(browser.Opened);
        Assert.Contains("/r/", url);
        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var report = ReportPayload.FromPage(await client.GetStringAsync(url, TestContext.Current.CancellationToken));
        Assert.Equal("scene.dialogue.md", report.ActivePath);

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    // Serves `root` in view mode on a free port, opening the browser on `script` when there is one
    // and on the empty shell otherwise, until `stop` is canceled.
    private static Task<int> Serve(FakeBrowserLauncher browser, string? script, string root, CancellationToken stop) =>
        new ServedShellRunner(browser).RunAsync(
            script, root, ReportMode.View, port: 0, noOpen: false,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            new StringWriter(), new StringWriter(), stop);
}
