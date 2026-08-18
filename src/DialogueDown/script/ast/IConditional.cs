namespace DialogueDown.Script.Ast;

/// <summary>
/// A node an optional <see cref="Ast.Condition"/> can condition — a <see cref="Line"/>, a
/// <see cref="Choice"/>, a <see cref="RandomOption"/>, or a <see cref="Jump"/>. The condition is
/// read uniformly through <see cref="ConditionalExtensions.IsConditional"/>, so the "is
/// conditional" test lives in one place instead of on each node.
/// </summary>
internal interface IConditional
{
    /// <summary>The condition guarding this node, or null when it is unconditional.</summary>
    Condition? Condition { get; }
}

internal static class ConditionalExtensions
{
    /// <summary>Whether a condition guards this node.</summary>
    public static bool IsConditional(this IConditional node) => node.Condition is not null;
}
