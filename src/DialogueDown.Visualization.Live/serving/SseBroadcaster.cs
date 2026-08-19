using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// Fans out live events to every connected SSE client. Each subscriber gets its
/// own unbounded channel; <see cref="Broadcast"/> enqueues to all of them, so one
/// document change reaches every open browser tab.
/// </summary>
internal sealed class SseBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<LiveEvent>> _clients = new();

    /// <summary>The number of currently connected clients.</summary>
    public int ClientCount => _clients.Count;

    /// <summary>
    /// Registers a subscriber and hands back its event <paramref name="reader"/>
    /// plus a handle that unregisters the subscriber when disposed.
    /// </summary>
    public IDisposable Subscribe(out ChannelReader<LiveEvent> reader)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<LiveEvent>();
        _clients[id] = channel;
        reader = channel.Reader;
        return new Unsubscriber(() =>
        {
            if (_clients.TryRemove(id, out var removed))
            {
                removed.Writer.TryComplete();
            }
        });
    }

    /// <summary>Pushes an event to every connected client.</summary>
    public void Broadcast(LiveEvent liveEvent)
    {
        foreach (var channel in _clients.Values)
        {
            channel.Writer.TryWrite(liveEvent);
        }
    }

    private sealed class Unsubscriber(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
