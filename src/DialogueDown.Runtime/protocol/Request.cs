namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Something the runner asks the host for, and waits on.
/// </summary>
/// <remarks>
/// The run does not go past a request until the driver answers: <see cref="Supply"/> for a
/// <see cref="Resolve"/>, <see cref="Done"/> or <see cref="Failed"/> for a
/// <see cref="Perform"/>. Reading the world and changing it are both asked for this way, so a
/// condition after an effect reads the world the effect already changed. The driver may answer at
/// once or after awaiting, say, a database.
/// <para>
/// Requests come in the same ordered list as other events, so a session replays in the order it
/// happened.
/// </para>
/// </remarks>
public abstract record Request : Event
{
    private protected Request()
    {
    }
}
