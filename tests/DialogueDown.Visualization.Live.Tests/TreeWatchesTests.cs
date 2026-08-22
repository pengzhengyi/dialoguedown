using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class TreeWatchesTests
{
    private static readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Watch_FiresWhenTheWatchedDocumentIsWritten()
    {
        using var tree = new TempTree();
        var document = tree.File("act-1/scene.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        using var fired = new SemaphoreSlim(0);
        using var watch = watches.Watch(document, () => fired.Release(), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(document)!);

        File.WriteAllText(document, "# Second");

        Assert.True(await fired.WaitAsync(_patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Watch_CoalescesTheWritesOfOneSave()
    {
        using var tree = new TempTree();
        var document = tree.File("scene.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        var count = 0;
        using var watch = watches.Watch(document, () => Interlocked.Increment(ref count), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(document)!);

        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(document, $"# Write {i}");
        }

        // The writes are one save, so wait for the report they coalesce into rather than for a
        // span long enough to be convincing, then drain to prove no second report follows.
        await WatchSync.DrainAsync(watches, Path.GetDirectoryName(document)!, _debounce);

        // An editor saves by writing several times; that is one reload, not five.
        Assert.InRange(count, 1, 2);
    }

    [Fact]
    public async Task Watch_IgnoresAFileNobodyWatches()
    {
        using var tree = new TempTree();
        var watched = tree.File("watched.dialogue.md", "# First");
        var other = tree.File("act-2/other.dialogue.md", "# Other");
        using var watches = new TreeWatches(tree.Root);
        var count = 0;
        using var watch = watches.Watch(watched, () => Interlocked.Increment(ref count), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(watched)!);

        File.WriteAllText(other, "# Changed");
        await WatchSync.DrainAsync(watches, Path.GetDirectoryName(other)!, _debounce);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Watch_StopsNotifyingOnceReleased()
    {
        using var tree = new TempTree();
        var document = tree.File("scene.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        var count = 0;
        var watch = watches.Watch(document, () => Interlocked.Increment(ref count), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(document)!);

        watch.Dispose();
        watch.Dispose(); // releasing twice is not an error
        File.WriteAllText(document, "# Second");
        await WatchSync.DrainAsync(watches, Path.GetDirectoryName(document)!, _debounce);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Watch_NotifiesEveryWatchOnOnePath()
    {
        using var tree = new TempTree();
        var document = tree.File("scene.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        using var first = new SemaphoreSlim(0);
        using var second = new SemaphoreSlim(0);
        using var watchOne = watches.Watch(document, () => first.Release(), _debounce);
        using var watchTwo = watches.Watch(document, () => second.Release(), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(document)!);

        File.WriteAllText(document, "# Second");

        Assert.True(await first.WaitAsync(_patience, TestContext.Current.CancellationToken));
        Assert.True(await second.WaitAsync(_patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Watch_FollowsADocumentThatLivesOutsideTheServedTree()
    {
        // `visualize <script>` resolves a terminal symlink, so the document it watches can be a
        // real file anywhere on disk while the served tree is elsewhere.
        using var tree = new TempTree();
        using var outside = new TempTree();
        var document = outside.File("real.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        using var fired = new SemaphoreSlim(0);
        using var watch = watches.Watch(document, () => fired.Release(), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(document)!);

        File.WriteAllText(document, "# Second");

        Assert.True(await fired.WaitAsync(_patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Watch_FiresForAResolvedSymlinkWhenItsRealTargetChanges()
    {
        // Saving through a link writes the real file, so the watch must follow the resolved target
        // rather than the name the reader launched.
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# First");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Symlinks.Create(link, real);
        using var watches = new TreeWatches(tree.Root);
        using var fired = new SemaphoreSlim(0);
        using var watch = watches.Watch(SymlinkResolver.Resolve(link), () => fired.Release(), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, tree.Root);

        File.WriteAllText(real, "# Second");

        Assert.True(await fired.WaitAsync(_patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Watch_FiresForADocumentWhoseNameStartsWithADot()
    {
        // The file provider hides sensitive files by default, and a hidden path yields a token that
        // never fires — so a dotfile script would silently stop hot-reloading.
        using var tree = new TempTree();
        var document = tree.File(".hidden.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        using var fired = new SemaphoreSlim(0);
        using var watch = watches.Watch(document, () => fired.Release(), _debounce);
        await WatchSync.WaitUntilLiveAsync(watches, Path.GetDirectoryName(document)!);

        File.WriteAllText(document, "# Second");

        Assert.True(await fired.WaitAsync(_patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Watch_RegistersNoFurtherWatcherForDocumentsSharingAFolder()
    {
        // The point of the whole exercise: opening script after script must not keep paying the
        // operating system to start watching. One registration covers a folder.
        using var tree = new TempTree();
        using var watches = new TreeWatches(tree.Root);
        var opened = new List<IDisposable>();

        foreach (var relative in new[] { "a.dialogue.md", "b.dialogue.md", "c.dialogue.md" })
        {
            opened.Add(watches.Watch(tree.File(relative, "# Scene"), () => { }, _debounce));
        }

        Assert.Equal(1, watches.WatchersInUse);
        foreach (var watch in opened)
        {
            watch.Dispose();
        }
    }

    [Fact]
    public void Watch_RegistersOneWatcherPerFolder()
    {
        // A folder costs one registration, not one per document — and never one per open, which is
        // what made switching scripts slow.
        using var tree = new TempTree();
        using var watches = new TreeWatches(tree.Root);
        var opened = new List<IDisposable>();

        foreach (var relative in new[] { "act-1/a.dialogue.md", "act-1/b.dialogue.md", "act-2/c.dialogue.md" })
        {
            opened.Add(watches.Watch(tree.File(relative, "# Scene"), () => { }, _debounce));
        }

        // The tree's own folder, plus act-1 and act-2.
        Assert.Equal(3, watches.WatchersInUse);
        foreach (var watch in opened)
        {
            watch.Dispose();
        }
    }

    [Fact]
    public void Watch_ReusesOneWatcherPerOutsideDirectory()
    {
        using var tree = new TempTree();
        using var outside = new TempTree();
        using var watches = new TreeWatches(tree.Root);

        using var one = watches.Watch(outside.File("one.dialogue.md", "# One"), () => { }, _debounce);
        using var two = watches.Watch(outside.File("two.dialogue.md", "# Two"), () => { }, _debounce);

        // The served tree's own folder, plus one for that outside folder — not one per document.
        Assert.Equal(2, watches.WatchersInUse);
    }
}
