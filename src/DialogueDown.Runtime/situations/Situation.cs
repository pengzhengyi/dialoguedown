namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Which node a run has reached, and what it is doing there.
/// </summary>
/// <remarks>
/// Two facts travel as one value. The first is where the run is. The second is what is happening
/// at that node: the run may have said a line and be waiting for the player to read on, or have
/// asked the host to carry out an effect, or have asked the world a question the node depends on.
/// <para>
/// Both are needed to know what may be sent next, and neither is enough alone. A run at node 4
/// that has just said a line takes <c>Next</c>. The same run at node 4, having asked the world
/// about <c>Alice.HasKey</c>, takes <c>Supply</c> and refuses <c>Next</c>. Carrying the two
/// together is what stops them disagreeing.
/// </para>
/// </remarks>
public abstract record Situation
{
    private protected Situation()
    {
    }
}
