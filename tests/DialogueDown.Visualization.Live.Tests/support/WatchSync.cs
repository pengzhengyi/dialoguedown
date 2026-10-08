using System.Globalization;
using DialogueDown.Visualization.Live.Files;

namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>
/// Synchronizes a test with the operating system's file-change events by writing a sentinel file
/// until the watcher reports it.
/// </summary>
/// <remarks>
/// <para>
/// Registering a watch takes time (over a tenth of a second on macOS), so a test that writes
/// immediately can miss its own change.
/// </para>
/// <para>
/// A sentinel bounds waiting; it does not order it. The operating system may report two paths in
/// either order, so a test that needs its own watch's report waits for that report, and drains
/// only to give a further one time to arrive.
/// </para>
/// </remarks>
internal static class WatchSync
{
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    // The readiness probe pokes until the watcher answers, so its debounce must be shorter than the
    // interval between pokes — a quiet period restarted by every poke would never elapse.
    private static readonly TimeSpan _probeDebounce = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan _pokeInterval = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// Waits until <paramref name="watches"/> is delivering events for <paramref name="folder"/>.
    /// Call this before the change a test is about to make.
    /// </summary>
    public static async Task WaitUntilLiveAsync(TreeWatches watches, string folder)
    {
        ArgumentNullException.ThrowIfNull(watches);

        var sentinel = NewSentinel(folder);
        using var reported = new SemaphoreSlim(0);
        using var watch = watches.Watch(sentinel, () => reported.Release(), _probeDebounce);

        try
        {
            var deadline = DateTime.UtcNow + _patience;
            for (var poke = 1; DateTime.UtcNow < deadline; poke++)
            {
                Write(sentinel, poke);

                if (await reported.WaitAsync(_pokeInterval, TestContext.Current.CancellationToken))
                {
                    return;
                }
            }

            throw new TimeoutException(
                $"The watcher never reported a change to {sentinel}, so it is not delivering.");
        }
        finally
        {
            watch.Dispose();
            File.Delete(sentinel);
        }
    }

    /// <summary>
    /// Waits until the watcher has been seen delivering after a change, giving a report of that
    /// change one full quiet period to arrive. <paramref name="debounce"/> must match the watch
    /// under test, so the window waited is the one that watch would need.
    /// </summary>
    /// <remarks>
    /// Use it before a negative assertion: the sentinel confirms the watcher was delivering during
    /// the window, which a fixed delay cannot.
    /// </remarks>
    public static async Task DrainAsync(TreeWatches watches, string folder, TimeSpan debounce)
    {
        ArgumentNullException.ThrowIfNull(watches);

        var sentinel = NewSentinel(folder);
        using var reported = new SemaphoreSlim(0);
        using var watch = watches.Watch(sentinel, () => reported.Release(), debounce);

        try
        {
            // One write only: the watcher is already delivering, and a second write would restart
            // the quiet period this barrier exists to wait out.
            Write(sentinel, 1);

            if (!await reported.WaitAsync(_patience, TestContext.Current.CancellationToken))
            {
                throw new TimeoutException(
                    $"The watcher never reported a change to {sentinel}, so nothing can be "
                        + "concluded about what came before it.");
            }
        }
        finally
        {
            watch.Dispose();
            File.Delete(sentinel);
        }
    }

    private static string NewSentinel(string folder)
    {
        var sentinel = Path.Combine(folder, $".watch-sync-{Guid.NewGuid():N}");
        Write(sentinel, 0);
        return sentinel;
    }

    private static void Write(string sentinel, int poke) =>
        File.WriteAllText(sentinel, poke.ToString(CultureInfo.InvariantCulture));
}
