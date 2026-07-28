namespace DialogueDown.Script.Ast;

/// <summary>
/// A node an optional <see cref="Ast.Condition"/> can guard — a <see cref="Line"/>, a
/// <see cref="Choice"/>, a <see cref="RandomOption"/>, or a <see cref="Jump"/>. The guard is
/// read uniformly through <see cref="ConditionalExtensions.IsConditional"/>, so the "is
/// guarded" test lives in one place instead of on each node.
/// </summary>
internal interface IConditional
{
    /// <summary>The condition guarding this node, or null when it is unguarded.</summary>
    Condition? Condition { get; }
}

internal static class ConditionalExtensions
{
    /// <summary>Whether a condition guards this node.</summary>
    public static bool IsConditional(this IConditional node) => node.Condition is not null;
}
