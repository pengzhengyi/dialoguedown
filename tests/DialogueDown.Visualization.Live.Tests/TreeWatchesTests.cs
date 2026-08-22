using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class TreeWatchesTests
{
    private static readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Watch_FiresWhenTheWatchedDocumentIsWritten()
    {
        using var tree = new TempTree();
        var document = tree.File("act-1/scene.dialogue.md", "# First");
        using var watches = new TreeWatches(tree.Root);
        using var fired = new SemaphoreSlim(0);
        using var watch = watches.Watch(document, () => fired.Release(), _debounce);
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

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
        using var watch = watches.Watch(document, () => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(250));
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(document, $"# Write {i}");
        }

        await Task.Delay(1500, TestContext.Current.CancellationToken);

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
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

        File.WriteAllText(other, "# Changed");
        await Task.Delay(800, TestContext.Current.CancellationToken);

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
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

        watch.Dispose();
        watch.Dispose(); // releasing twice is not an error
        File.WriteAllText(document, "# Second");
        await Task.Delay(800, TestContext.Current.CancellationToken);

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
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

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
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

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
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

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
        await Task.Delay(_settle, TestContext.Current.CancellationToken);

        File.WriteAllText(document, "# Second");

        Assert.True(await fired.WaitAsync(_patience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Watch_RegistersNoFurtherWatcherForDocumentsInTheTree()
    {
        // The point of the whole exercise: opening script after script must not keep paying the
        // operating system to start watching. One provider covers the tree, however deep.
        using var tree = new TempTree();
        using var watches = new TreeWatches(tree.Root);
        var opened = new List<IDisposable>();

        foreach (var relative in new[] { "a.dialogue.md", "act-1/b.dialogue.md", "act-2/deep/c.dialogue.md" })
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
    public void Watch_ReusesOneWatcherPerOutsideDirectory()
    {
        using var tree = new TempTree();
        using var outside = new TempTree();
        using var watches = new TreeWatches(tree.Root);

        using var one = watches.Watch(outside.File("one.dialogue.md", "# One"), () => { }, _debounce);
        using var two = watches.Watch(outside.File("two.dialogue.md", "# Two"), () => { }, _debounce);

        // The served tree's own provider, plus one for that outside folder — not one per document.
        Assert.Equal(2, watches.WatchersInUse);
    }
}
