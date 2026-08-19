namespace DialogueDown.Visualization.Live.Files;

/// <summary>
/// Coalesces a burst of rapid triggers into a single action, fired once the triggers go quiet for
/// the debounce delay. Editors save by writing several times (or writing a temp file and
/// renaming), so the watcher debounces those bursts into one recompile.
/// </summary>
/// <remarks>
/// Runs are serialized: the action never runs concurrently with itself. A trigger (or a timer that
/// fires) while a run is in progress is coalesced into a single follow-up run scheduled after the
/// current one finishes, so a slow compile can never be overtaken by a newer one that then
/// broadcasts a stale result before it — the newest run always broadcasts last.
/// </remarks>
internal sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly Action _action;
    private readonly ITimer _timer;
    private readonly object _gate = new();

    // Whether the action is running now, whether a trigger arrived during that run and still needs
    // one follow-up run once it finishes, and whether the debouncer has been disposed. All three are
    // read and written only under _gate, so Trigger, Fire, and Dispose never race the timer.
    private bool _running;
    private bool _pending;
    private bool _disposed;

    /// <summary>
    /// Fires <paramref name="action"/> once triggers stay quiet for <paramref name="delay"/>.
    /// <paramref name="timeProvider"/> supplies the clock (defaults to the system clock); a
    /// test injects a fake one to drive the debounce deterministically, without real waiting.
    /// </summary>
    public Debouncer(TimeSpan delay, Action action, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delay = delay;
        _action = action;
        _timer = (timeProvider ?? TimeProvider.System).CreateTimer(
            _ => Fire(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Registers activity; restarts the quiet-period countdown, or coalesces into an active run.</summary>
    public void Trigger()
    {
        lock (_gate)
        {
            if (_disposed || _running)
            {
                // After disposal, drop the trigger entirely; during a run, remember it and let the
                // current run finish, then run once more — never a second concurrent run and never
                // a rearm of a disposed timer.
                _pending = _running && !_disposed;
                return;
            }

            // Arm under the gate so a concurrent Dispose cannot slip in and dispose the timer
            // between the disposed check and the Change call.
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _timer.Dispose();
    }

    private void Fire()
    {
        lock (_gate)
        {
            if (_disposed || _running)
            {
                // Disposed, or the timer fired while a run is still in progress (a race with
                // Trigger): coalesce into one follow-up rather than running concurrently, and never
                // start a run after disposal.
                _pending = _running && !_disposed;
                return;
            }

            _running = true;
        }

        bool followUp;
        try
        {
            _action();
        }
        finally
        {
            // Always clear _running — even if the action threw — so a failing refresh can never
            // wedge the debouncer and stop every later on-disk change from reloading.
            lock (_gate)
            {
                _running = false;
                followUp = _pending;
                _pending = false;
            }
        }

        if (followUp)
        {
            // Re-arm the debounce for the change that arrived during the run, but only under the
            // gate and only if a concurrent Dispose has not already retired the timer.
            lock (_gate)
            {
                if (!_disposed)
                {
                    _timer.Change(_delay, Timeout.InfiniteTimeSpan);
                }
            }
        }
    }
}
