using DialogueDown.Visualization.Live.Serving;
namespace DialogueDown.Visualization.Live.Tests;

public sealed class SseBroadcasterTests
{
    [Fact]
    public void Broadcast_DeliversToASubscriber()
    {
        var broadcaster = new SseBroadcaster();
        using var subscription = broadcaster.Subscribe(out var reader);

        broadcaster.Broadcast(new LiveEvent("reload", "{}"));

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
    }

    [Fact]
    public void Broadcast_DeliversToEverySubscriber()
    {
        var broadcaster = new SseBroadcaster();
        using var first = broadcaster.Subscribe(out var readerA);
        using var second = broadcaster.Subscribe(out var readerB);

        broadcaster.Broadcast(new LiveEvent("reload", "{}"));

        Assert.True(readerA.TryRead(out _));
        Assert.True(readerB.TryRead(out _));
        Assert.Equal(2, broadcaster.ClientCount);
    }

    [Fact]
    public void Dispose_UnsubscribesAndCompletesTheChannel()
    {
        var broadcaster = new SseBroadcaster();
        var subscription = broadcaster.Subscribe(out var reader);

        subscription.Dispose();

        Assert.Equal(0, broadcaster.ClientCount);
        Assert.True(reader.Completion.IsCompleted);
    }

    [Fact]
    public void Broadcast_WithNoSubscribers_IsANoOp()
    {
        var broadcaster = new SseBroadcaster();

        broadcaster.Broadcast(new LiveEvent("reload", "{}"));

        Assert.Equal(0, broadcaster.ClientCount);
    }
}
