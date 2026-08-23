namespace DialogueDown.Playbook.Edges;

/// <summary>
/// Reading order: what plays next when nothing branches.
/// </summary>
/// <remarks>
/// The only edge without a condition — falling through to the next block is not a decision.
/// </remarks>
/// <param name="Target">The node reading order continues into.</param>
public sealed record SuccessionEdge(int Target) : Edge(Target);
