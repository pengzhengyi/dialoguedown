namespace DialogueDown.Visualization.Live.Files;

/// <summary>
/// Watches a single document for on-disk changes and invokes a callback, debounced
/// so an editor's multi-write save produces one notification. Watches the parent
/// directory (filtered to the file name) so create/rename/delete are all caught.
/// </summary>
internal sealed class DocumentWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Debouncer _debouncer;

    /// <summary>Starts watching <paramref name="path"/>; calls <paramref name="onChanged"/> after each debounced change.</summary>
    public DocumentWatcher(string path, Action onChanged, TimeSpan? debounce = null)
    {
        _debouncer = new Debouncer(debounce ?? TimeSpan.FromMilliseconds(150), onChanged);

        var fullPath = Path.GetFullPath(path);
        _watcher = new FileSystemWatcher(
            Path.GetDirectoryName(fullPath)!,
            Path.GetFileName(fullPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += OnFileSystemEvent;
        _watcher.Created += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Renamed += OnFileSystemEvent;
        _watcher.EnableRaisingEvents = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _watcher.Dispose();
        _debouncer.Dispose();
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e) => _debouncer.Trigger();
}
