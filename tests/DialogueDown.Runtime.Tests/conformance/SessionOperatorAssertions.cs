using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>What a driven session has said, asserted in one line rather than three.</summary>
/// <remarks>
/// Reading an event takes it off the queue, so an unhelped test spends a line proving something
/// came back and another on what kind it was before it can say anything about the event itself.
/// These name the claim instead, and hand the event back for a test that wants to say more.
/// </remarks>
internal static class SessionOperatorAssertions
{
    /// <summary>Asserts the run has fallen silent, and stays silent when read again.</summary>
    /// <param name="op">The session being driven.</param>
    public static void AssertNoUnreadEvents(this SessionOperator op)
    {
        Assert.Equal(0, op.UnreadEventCount);
        Assert.Null(op.NextEvent());
    }

    /// <summary>Reads the next event, asserting what kind it is.</summary>
    /// <typeparam name="TEvent">The kind it should be.</typeparam>
    /// <param name="op">The session being driven.</param>
    /// <returns>The event, for a test that wants to say more about it.</returns>
    public static TEvent AssertNextEvent<TEvent>(this SessionOperator op)
        where TEvent : Event =>
        Assert.IsType<TEvent>(op.NextEvent());

    /// <summary>Reads the one event waiting, asserting its kind and that nothing follows it.</summary>
    /// <typeparam name="TEvent">The kind it should be.</typeparam>
    /// <param name="op">The session being driven.</param>
    /// <returns>The event, for a test that wants to say more about it.</returns>
    public static TEvent AssertOnlyEvent<TEvent>(this SessionOperator op)
        where TEvent : Event
    {
        var only = op.AssertNextEvent<TEvent>();
        op.AssertNoUnreadEvents();

        return only;
    }
}
