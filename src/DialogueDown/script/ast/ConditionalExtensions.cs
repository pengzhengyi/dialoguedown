namespace DialogueDown.Script.Ast;

internal static class ConditionalExtensions
{
    /// <summary>Whether a condition guards this node.</summary>
    public static bool IsConditional(this IConditional node) => node.Condition is not null;
}
