using DialogueDown.Play.Tests.Support;
using DialogueDown.Play.Tui;

namespace DialogueDown.Play.Tests.Tui;

/// <summary>
/// The key read: it answers what the console gives, stops the moment the caller's token is
/// cancelled, and never holds a wait open for a console that does not answer.
/// </summary>
public sealed class KeyPressTests
{
    [Fact]
    public async Task ReadAsync_CancelledWhileWaiting_StopsTheWait()
    {
        // A test console faults rather than waiting, so a real wait is stubbed here; this is the
        // path a driver's cancellation takes.
        var console = StubConsoles.ThatNeverAnswers();
        using var cancelled = new CancellationTokenSource();

        var waiting = KeyPress.ReadAsync(console, cancelled.Token);
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }

    [Fact]
    public async Task ReadAsync_WithoutAKey_AnswersNothing()
    {
        var console = StubConsoles.WithNoKey();

        var key = await KeyPress.ReadAsync(console, CancellationToken.None);

        Assert.Null(key);
    }
}
