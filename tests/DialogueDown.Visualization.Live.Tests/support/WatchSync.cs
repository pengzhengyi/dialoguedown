using System.Globalization;
using DialogueDown.Visualization.Live.Files;

namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>
/// Synchronizes a test with the operating system's file-change events, so it waits for the watcher
/// to actually be delivering rather than sleeping for a guess at how long that takes.
/// </summary>
/// <remarks>
/// <para>
/// Registering a watch is not instant — on macOS it costs over a tenth of a second — so a test that
/// writes immediately can miss its own change. A fixed settle covers that, but it is a guess in
/// both directions: long enough to be wasted on every test that did not need it, short enough to
/// miss registration on a loaded machine. Poking a sentinel file until the watcher reports it turns
/// the wait into an observation, which ends as soon as delivery starts and cannot be too short.
/// </para>
/// <para>
/// A sentinel bounds waiting; it does not order it. Which of two paths the watcher reports first is
/// the operating system's business, and inotify in particular promises nothing there. So a test
/// waits for its own watch's report when it needs that report, and drains only to give a further
/// one time to arrive.
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
    /// This is a bound on waiting, not a proof of ordering: the operating system makes no promise
    /// that events for one path are delivered before another's, so a test that needs its own
    /// watch's report should wait for that report and use this only to let a further one arrive.
    /// What it does give a negative assertion is better than a sleep — the sentinel confirms the
    /// watcher is delivering during the window, where a fixed delay may pass while it is stalled.
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
