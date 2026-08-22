using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// The <c>kind</c> values that tag an edge on the wire.
/// </summary>
/// <remarks>
/// Separate from <see cref="NodeKinds"/> rather than one flat list, because a branch is both a
/// node and an edge and the two mean different things.
/// </remarks>
public static class EdgeKinds
{
    /// <summary>Reading order: what plays next when nothing branches.</summary>
    public const string Succession = "succession";

    /// <summary>One arm of a player's choice.</summary>
    public const string Option = "option";

    /// <summary>One arm of a random choice, carrying its odds.</summary>
    public const string RandomOption = "random-option";

    /// <summary>One arm of a block condition, carrying its place in the order tried.</summary>
    public const string Branch = "branch";

    /// <summary>A jump that does not return.</summary>
    public const string Divert = "divert";
}
