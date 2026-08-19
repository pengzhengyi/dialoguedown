using DialogueDown.Visualization.Live.Files;
using Microsoft.Extensions.Time.Testing;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class DebouncerTests
{
    [Fact]
    public void Trigger_MultipleTimesWithinTheDelay_FiresOnce()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(150), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(50));
        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(50));
        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(150)); // quiet for the full delay after the last trigger

        Assert.Equal(1, count);
    }

    [Fact]
    public void Trigger_AfterFiring_FiresAgain()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(100), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // fires once
        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // fires again

        Assert.Equal(2, count);
    }

    [Fact]
    public void Fire_WhenTheActionThrows_ClearsRunningSoLaterTriggersStillFire()
    {
        // A refresh that throws (e.g. an unreadable file) must not wedge the debouncer: clearing
        // the running flag in a finally keeps every later on-disk change producing a reload. A
        // manual timer drives the callback so the test does not depend on a fake timer re-firing
        // after a throw.
        var provider = new ManualTimeProvider();
        var runs = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            runs++;
            if (runs == 1)
            {
                throw new InvalidOperationException("boom");
            }
        }, provider);
        var timer = provider.Timer!;

        Assert.Throws<InvalidOperationException>(timer.Fire); // the throwing run propagates
        Assert.Equal(1, runs);

        // Not wedged: a later change re-arms the timer (a no-op if _running were stuck true)...
        var armed = timer.ChangeCount;
        debouncer.Trigger();
        Assert.True(timer.ChangeCount > armed);

        // ...and that re-armed fire actually runs the action again.
        timer.Fire();
        Assert.Equal(2, runs);
    }

    [Fact]
    public void Trigger_ThenQuiet_DoesNotFireEarly()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(300), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // still within the debounce window

        Assert.Equal(0, count);
    }

    [Fact]
    public void Trigger_DuringTheAction_CoalescesIntoExactlyOneSerializedFollowUpRun()
    {
        // A change that lands while the debounced action is still running (a slow compile) must
        // not run concurrently with it; it is coalesced into exactly one follow-up run after the
        // current one finishes, so an older run can never broadcast after a newer one.
        var time = new FakeTimeProvider();
        var runs = 0;
        var maxConcurrent = 0;
        var concurrent = 0;
        Debouncer? debouncer = null;
        debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            var now = Interlocked.Increment(ref concurrent);
            maxConcurrent = Math.Max(maxConcurrent, now);
            runs++;
            if (runs == 1)
            {
                debouncer!.Trigger(); // a file change arrives mid-run
            }

            Interlocked.Decrement(ref concurrent);
        }, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(10)); // fires run #1, which re-triggers once

        Assert.Equal(1, runs); // the coalesced change is deferred, not run inline or concurrently
        Assert.Equal(1, maxConcurrent);

        time.Advance(TimeSpan.FromMilliseconds(10)); // the coalesced follow-up fires
        Assert.Equal(2, runs);

        time.Advance(TimeSpan.FromMilliseconds(100)); // nothing more is pending
        Assert.Equal(2, runs);

        debouncer.Dispose();
    }

    [Fact]
    public void Trigger_ManyTimesDuringOneRun_CoalescesToASingleFollowUp()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        Debouncer? debouncer = null;
        debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            runs++;
            if (runs == 1)
            {
                debouncer!.Trigger();
                debouncer!.Trigger();
                debouncer!.Trigger();
            }
        }, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(10));
        time.Advance(TimeSpan.FromMilliseconds(10));
        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(2, runs); // three mid-run triggers collapse into one follow-up run

        debouncer.Dispose();
    }

    [Fact]
    public void Trigger_AfterDispose_DoesNotRearmTheDisposedTimer()
    {
        var provider = new ManualTimeProvider();
        var runs = 0;
        var debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () => runs++, provider);
        var timer = provider.Timer!;

        debouncer.Dispose();
        var changesBefore = timer.ChangeCount;
        debouncer.Trigger(); // a change that lands after disposal

        Assert.Equal(changesBefore, timer.ChangeCount); // the disposed timer is never re-armed
        Assert.False(timer.ChangedWhileDisposed);
        Assert.Equal(0, runs);
    }

    [Fact]
    public void Fire_AfterDispose_DoesNotRunTheAction()
    {
        // A timer callback already queued when Dispose lands must not run the action afterward.
        var provider = new ManualTimeProvider();
        var runs = 0;
        var debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () => runs++, provider);
        var timer = provider.Timer!;

        debouncer.Dispose();
        timer.Fire(); // a late callback races the disposal

        Assert.Equal(0, runs);
    }

    [Fact]
    public void Dispose_DuringARun_SkipsTheCoalescedRearm()
    {
        // A change arrives mid-run (coalesced into a follow-up), but Dispose lands before the run
        // finishes: the follow-up rearm must be skipped rather than call Change on a disposed timer.
        var provider = new ManualTimeProvider();
        var runs = 0;
        Debouncer? debouncer = null;
        debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            runs++;
            if (runs == 1)
            {
                debouncer!.Trigger(); // coalesced into a pending follow-up
                debouncer!.Dispose(); // ...but disposal lands before the run returns
            }
        }, provider);
        var timer = provider.Timer!;

        var changesBefore = timer.ChangeCount;
        timer.Fire();

        Assert.Equal(1, runs); // no follow-up run
        Assert.Equal(changesBefore, timer.ChangeCount); // the pending follow-up never re-arms
        Assert.False(timer.ChangedWhileDisposed);
    }

    [Fact]
    public async Task Dispose_RacingConcurrentTriggers_NeverThrows()
    {
        // The production race: many watcher events call Trigger while the host disposes the
        // debouncer. Arming under the gate with a disposed flag keeps a trigger from ever calling
        // Change on a disposed timer (which would throw ObjectDisposedException).
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var debouncer = new Debouncer(TimeSpan.FromMilliseconds(1), () => { });
            using var start = new ManualResetEventSlim();
            var triggers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                start.Wait();
                for (var i = 0; i < 200; i++)
                {
                    debouncer.Trigger();
                }
            })).ToArray();
            var disposal = Task.Run(
                () =>
                {
                    start.Wait();
                    debouncer.Dispose();
                },
                TestContext.Current.CancellationToken);

            start.Set();
            await Task.WhenAll(triggers.Append(disposal)); // no ObjectDisposedException escapes
        }
    }

    // A time provider whose single timer fires only when the test calls Fire(), and that records
    // how many times it was (re-)armed — enough to drive the debounce deterministically, including
    // a callback that throws, without depending on a fake clock's post-throw behavior.
    private sealed class ManualTimeProvider : TimeProvider
    {
        public ManualTimer? Timer { get; private set; }

        public override ITimer CreateTimer(
            TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            Timer = new ManualTimer(callback, state);
    }

    private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        public int ChangeCount { get; private set; }

        public bool Disposed { get; private set; }

        // Set if Change is ever called after Dispose — the exact bug the gate must prevent.
        public bool ChangedWhileDisposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            ChangeCount++;
            if (Disposed)
            {
                ChangedWhileDisposed = true;
            }

            return true;
        }

        public void Fire() => callback(state);

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
