namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// One instruction that moves a run.
/// </summary>
/// <remarks>
/// The whole surface through which a run is manipulated. Whatever is at the other end -- a test,
/// a terminal, a replay, a remote client -- speaks this same closed set, and the runner refuses
/// anything outside it plainly.
/// </remarks>
public abstract record Command
{
    private protected Command()
    {
    }
}
