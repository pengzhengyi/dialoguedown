using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class DocumentWatcherTests
{
    [Fact]
    public async Task WritingTheDocument_InvokesTheCallback()
    {
        using var doc = new TempDocument("# First");
        using var fired = new SemaphoreSlim(0);
        using var watcher = new DocumentWatcher(
            doc.Path,
            () => fired.Release(),
            TimeSpan.FromMilliseconds(100));
        await Task.Delay(200, TestContext.Current.CancellationToken); // let the watcher settle before changing the file

        File.WriteAllText(doc.Path, "# Second");

        Assert.True(await fired.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RapidWrites_AreDebouncedIntoFewerCallbacks()
    {
        using var doc = new TempDocument("# First");
        var count = 0;
        using var watcher = new DocumentWatcher(
            doc.Path,
            () => Interlocked.Increment(ref count),
            TimeSpan.FromMilliseconds(250));
        await Task.Delay(200, TestContext.Current.CancellationToken);

        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(doc.Path, $"# Write {i}");
        }
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Five near-instant writes must not produce five recompiles.
        Assert.InRange(count, 1, 2);
    }

    [Fact]
    public async Task WatchingAResolvedSymlink_FiresWhenTheRealTargetChanges()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# First");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Symlinks.Create(link, real);
        var resolved = SymlinkResolver.Resolve(link);
        using var fired = new SemaphoreSlim(0);
        using var watcher = new DocumentWatcher(
            resolved,
            () => fired.Release(),
            TimeSpan.FromMilliseconds(100));
        await Task.Delay(200, TestContext.Current.CancellationToken); // let the watcher settle before changing the file

        // An external editor writes the real file (what saving through the link also does); the
        // watcher, rooted at the resolved target rather than the link name, must see it.
        File.WriteAllText(real, "# Second");

        Assert.True(await fired.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }
}
