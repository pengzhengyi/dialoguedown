namespace DialogueDown.Runtime.Positions;

/// <summary>
/// Where a run stands, and at what stage.
/// </summary>
/// <remarks>
/// A run is not always simply <em>at</em> a node: later it will pause at a line it has already
/// reached, waiting for the world to answer a question that line asks. One value carries both
/// where the run is and what stage it is at, so the two can never disagree.
/// </remarks>
public abstract record Position
{
    private protected Position()
    {
    }
}
