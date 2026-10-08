namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// One instruction that moves a run.
/// </summary>
/// <remarks>
/// The closed set every driver sends, whether a test, a terminal, a replay, or a remote client:
/// <see cref="Start"/>, <see cref="Next"/>, <see cref="Done"/>, <see cref="Failed"/>, and
/// <see cref="Supply"/>. The runner refuses any other command as
/// <see cref="RefusalReason.UnknownCommand"/>.
/// </remarks>
public abstract record Command
{
    private protected Command()
    {
    }
}
