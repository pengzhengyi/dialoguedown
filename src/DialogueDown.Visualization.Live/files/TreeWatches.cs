using System.Collections.Concurrent;

namespace DialogueDown.Visualization.Live.Files;

/// <summary>
/// The watches a served run holds over its tree. Registering with the operating system is the
/// expensive part of watching a file — on macOS it costs over a tenth of a second — so this does it
/// once for the whole tree and hands out a watch per path for nothing, however deep the path or
/// however often the active document changes.
/// </summary>
/// <remarks>
/// <para>
/// A launched script is resolved through its symlinks, so the file being watched can be a real file
/// anywhere on disk while the served tree is elsewhere. Such a document gets a watcher for its own
/// folder, kept and reused like the tree's own.
/// </para>
/// <para>
/// <c>PhysicalFileProvider</c> offers the same cheap registration and was tried first. Its change
/// tokens report one save as several notifications up to 0.8 seconds apart, which no debounce
/// window gathers back together, and the live session's suppression of its own writes expects
/// exactly one. Owning the watcher keeps the event stream this repository already relies on.
/// </para>
/// </remarks>
internal sealed class TreeWatches : IDisposable
{
    private static readonly TimeSpan _defaultDebounce = TimeSpan.FromMilliseconds(150);

    private readonly string _root;
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new(PathComparison.Comparer);
    private readonly ConcurrentDictionary<string, WatchList> _byPath = new(PathComparison.Comparer);

    /// <summary>Starts watching the tree rooted at <paramref name="root"/>.</summary>
    public TreeWatches(string root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = PathComparison.Normalize(root);
        WatcherFor(_root);
    }

    /// <summary>How many watchers are open. One means every watch is covered by the tree's own.</summary>
    public int WatchersInUse => _watchers.Count;

    /// <summary>
    /// Calls <paramref name="onChanged"/> after changes to <paramref name="path"/> go quiet for
    /// <paramref name="debounce"/>. Dispose the result to stop watching.
    /// </summary>
    public IDisposable Watch(string path, Action onChanged, TimeSpan? debounce = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(onChanged);

        var full = PathComparison.Normalize(Path.GetFullPath(path));
        WatcherFor(RootFor(full));
        return new PathWatch(this, full, onChanged, debounce ?? _defaultDebounce);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
        _byPath.Clear();
    }

    // Files under the tree share its watcher; a document reached through a symlink out of the tree
    // gets one for its own folder, so its neighbours there are free too.
    private string RootFor(string fullPath) =>
        PathComparison.IsUnder(_root, fullPath) ? _root : PathComparison.Normalize(Path.GetDirectoryName(fullPath)!);

    private void WatcherFor(string root) =>
        _watchers.GetOrAdd(root, key =>
        {
            var watcher = new FileSystemWatcher(key)
            {
                // The filter the per-document watcher used, so one save still reports once.
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = true,
            };
            watcher.Changed += OnEntryChanged;
            watcher.Created += OnEntryChanged;
            watcher.Deleted += OnEntryChanged;
            watcher.Renamed += OnEntryRenamed;
            watcher.Error += OnWatcherError;
            watcher.EnableRaisingEvents = true;
            return watcher;
        });

    private void OnEntryChanged(object sender, FileSystemEventArgs e) => Notify(e.FullPath);

    // A rename changes both names: the file that left and the file that arrived. An editor saving
    // atomically renames a temporary file over the document, which arrives as the latter.
    private void OnEntryRenamed(object sender, RenamedEventArgs e)
    {
        Notify(e.OldFullPath);
        Notify(e.FullPath);
    }

    // The operating system's event buffer overflowed, so some change went unseen. Telling every
    // watch costs a reload of something that may not have changed; staying quiet risks a report
    // that never updates again.
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        foreach (var watches in _byPath.Values)
        {
            watches.TriggerAll();
        }
    }

    private void Notify(string fullPath)
    {
        if (_byPath.TryGetValue(PathComparison.Normalize(fullPath), out var watches))
        {
            watches.TriggerAll();
        }
    }

    private void Add(string path, PathWatch watch) =>
        _byPath.AddOrUpdate(path, _ => new WatchList(watch), (_, existing) => existing.Add(watch));

    private void Remove(string path, PathWatch watch)
    {
        if (!_byPath.TryGetValue(path, out var existing))
        {
            return;
        }

        var remaining = existing.Remove(watch);
        if (remaining.IsEmpty)
        {
            _byPath.TryRemove(path, out _);
        }
        else
        {
            _byPath[path] = remaining;
        }
    }

    /// <summary>The watches on one path, replaced whole so a notification never reads a torn list.</summary>
    private sealed class WatchList
    {
        private readonly PathWatch[] _watches;

        public WatchList(PathWatch watch) => _watches = [watch];

        private WatchList(PathWatch[] watches) => _watches = watches;

        public bool IsEmpty => _watches.Length == 0;

        public WatchList Add(PathWatch watch) => new([.. _watches, watch]);

        public WatchList Remove(PathWatch watch) =>
            new([.. _watches.Where(existing => !ReferenceEquals(existing, watch))]);

        public void TriggerAll()
        {
            foreach (var watch in _watches)
            {
                watch.Trigger();
            }
        }
    }

    /// <summary>One path's registration: every change to it, gathered into one call.</summary>
    private sealed class PathWatch : IDisposable
    {
        private readonly TreeWatches _owner;
        private readonly string _path;
        private readonly Debouncer _debouncer;
        private bool _disposed;

        public PathWatch(TreeWatches owner, string path, Action onChanged, TimeSpan debounce)
        {
            _owner = owner;
            _path = path;
            _debouncer = new Debouncer(debounce, onChanged);
            owner.Add(path, this);
        }

        public void Trigger() => _debouncer.Trigger();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.Remove(_path, this);
            _debouncer.Dispose();
        }
    }
}
