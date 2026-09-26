namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Which of a node's two readings of the world a run is waiting on.
/// </summary>
/// <remarks>
/// A run reads the world twice at a node — once on the way in and once on the way out — because
/// the node may change the world between them. The moment travels with the wait and says which of
/// the two the answers belong to.
/// </remarks>
public enum Moment
{
    /// <summary>On the way in: whether the node plays at all, and what its words say.</summary>
    ToPlay,

    /// <summary>On the way out: which of the node's ways out the run takes.</summary>
    ToLeave,
}
