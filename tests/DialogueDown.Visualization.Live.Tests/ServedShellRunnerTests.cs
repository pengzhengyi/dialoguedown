using System.Diagnostics;
using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class ServedShellRunnerTests
{
    [Fact]
    public async Task RunAsync_InvalidRoot_ReturnsOne()
    {
        var error = new StringWriter();
        var runner = new ServedShellRunner(new FakeBrowserLauncher());

        var code = await runner.RunAsync(
            script: null,
            root: Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"),
            ReportMode.View,
            port: null,
            noOpen: true,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            new StringWriter(),
            error,
            CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("not a directory", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_NoScript_ServesTheEmptyShell_AndStopsOnCancellation()
    {
        using var tree = new TempTree();
        tree.File("scene.dialogue.md", "# Scene");
        var browser = new FakeBrowserLauncher();
        var runner = new ServedShellRunner(browser);
        using var stop = new CancellationTokenSource();

        var task = runner.RunAsync(
            script: null, root: tree.Root, ReportMode.View, port: 0, noOpen: false,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        var url = Assert.Single(browser.Opened);
        Assert.StartsWith("http://127.0.0.1:", url);

        // The landing is the report shell's empty state (the Explorer over the project), not a
        // separate picker page: it carries the project payload and no active document.
        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var landing = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
        Assert.StartsWith("<!doctype html", landing, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"project\":", landing);

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    [Fact]
    public async Task RunAsync_WithAScript_OpensItsReportUnderTheReportMount()
    {
        using var tree = new TempTree();
        var scriptPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.\n");
        var browser = new FakeBrowserLauncher();
        var runner = new ServedShellRunner(browser);
        using var stop = new CancellationTokenSource();

        var task = runner.RunAsync(
            scriptPath, tree.Root, ReportMode.View, port: 0, noOpen: false,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        // A script opens directly on its report under the /r mount, and that report carries the
        // active document (its project payload names the script).
        var url = Assert.Single(browser.Opened);
        Assert.Contains("/r/", url);
        using var client = new HttpClient { BaseAddress = new Uri(url) };
        Assert.Contains("scene.dialogue.md", await client.GetStringAsync(url, TestContext.Current.CancellationToken));

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not met in time.");
            }

            await Task.Delay(25, TestContext.Current.CancellationToken);
        }
    }
}
