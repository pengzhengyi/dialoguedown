namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>Records the targets it is asked to open, without launching anything.</summary>
/// <remarks>
/// Opening happens on the runner's own thread once its server is listening, so a test cannot know
/// when to look. Announcing the first open lets the test await it instead of asking repeatedly:
/// the wait ends the moment it happens, and there is no polling interval to pick — an interval
/// being both a delay on every run and a guess that a slower machine can outrun.
/// </remarks>
internal sealed class FakeBrowserLauncher : IBrowserLauncher
{
    private readonly TaskCompletionSource _opened =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Lock _gate = new();
    private readonly List<string> _targets = [];

    /// <summary>Completes when <see cref="Open"/> is first called.</summary>
    public Task FirstOpened => _opened.Task;

    /// <summary>Targets passed to <see cref="Open"/>, in order.</summary>
    public IReadOnlyList<string> Opened
    {
        get
        {
            lock (_gate)
            {
                return [.. _targets];
            }
        }
    }

    public void Open(string target)
    {
        lock (_gate)
        {
            _targets.Add(target);
        }

        // After recording, so a test woken by this sees the target that woke it.
        _opened.TrySetResult();
    }
}
