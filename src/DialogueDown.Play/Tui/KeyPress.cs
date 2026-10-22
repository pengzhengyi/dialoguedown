using Spectre.Console;

namespace DialogueDown.Play.Tui;

/// <summary>Reads one key at a wait, stopping the moment the caller's token is cancelled.</summary>
internal static class KeyPress
{
    /// <summary>
    /// Reads the next key, or stops with <see cref="OperationCanceledException"/> when
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="console">The console to read from.</param>
    /// <param name="cancellationToken">Stops the wait.</param>
    /// <returns>
    /// The key read, or <see langword="null"/> when the console has no key to give.
    /// </returns>
    /// <remarks>
    /// The console is asked to stop too. But a console only sees the token if it honors it, and a
    /// test console need not, so the wait also races the token itself: the contract belongs to this
    /// player, not to whichever console it was handed.
    /// </remarks>
    public static async Task<ConsoleKeyInfo?> ReadAsync(
        IAnsiConsole console, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var read = console.Input.ReadKeyAsync(intercept: true, stop.Token);
        var stopped = Task.Delay(Timeout.InfiniteTimeSpan, stop.Token);
        if (await Task.WhenAny(read, stopped) == read)
        {
            // Release the race's timer; the cancellation token it held is no longer needed either.
            await stop.CancelAsync();
            return await read;
        }

        // The token fired first: the wait stops, taking the console's own read with it.
        throw new OperationCanceledException(cancellationToken);
    }
}
