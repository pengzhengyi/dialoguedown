namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Something the runner asks the host for, and waits on.
/// </summary>
/// <remarks>
/// A plain event reports; a request asks, and the run does not go past it until the driver
/// answers. Reading the world and changing it are both asked for this way, so a guard that
/// follows an effect sees a world the effect has already changed. A driver that means to answer
/// at once may, and one that must await a database may too — what the request buys is the choice.
/// <para>
/// Requests travel the same ordered stream as events, because a session is one conversation: what
/// a runner asked and what it reported have to be replayable in the order they happened.
/// </para>
/// </remarks>
public abstract record Request : Event
{
    private protected Request()
    {
    }
}
