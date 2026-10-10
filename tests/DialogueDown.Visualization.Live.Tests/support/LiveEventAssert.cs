using System.Threading.Channels;
using DialogueDown.Visualization.Live.Serving;

namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>Assertions about the events a live session broadcasts to the pages watching it.</summary>
internal static class LiveEventAssert
{
    /// <summary>Asserts that the next event a subscriber reads is <paramref name="name"/>.</summary>
    /// <param name="reader">The subscriber's reader.</param>
    /// <param name="name">The event's name, such as <c>reload</c> or <c>problem</c>.</param>
    /// <returns>The payload the event carries.</returns>
    public static LivePayload AssertBroadcast(ChannelReader<LiveEvent> reader, string name)
    {
        Assert.True(reader.TryRead(out var received), $"Expected a {name} event, but nothing was broadcast.");
        Assert.Equal(name, received.Event);

        return LivePayload.Parse(received.Data);
    }

    /// <summary>Asserts that a subscriber has nothing to read.</summary>
    /// <param name="reader">The subscriber's reader.</param>
    public static void AssertNothingBroadcast(ChannelReader<LiveEvent> reader)
    {
        var read = reader.TryRead(out var received);

        Assert.False(read, $"Expected no event, but {received?.Event} was broadcast.");
    }
}
