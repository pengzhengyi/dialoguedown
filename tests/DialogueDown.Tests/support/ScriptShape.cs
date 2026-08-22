using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Renders a <see cref="ScriptDocument"/> as text, so two trees can be compared for sameness.
/// </summary>
/// <remarks>
/// The AST's records hold their children in lists, which compare by reference, so two separately
/// built trees are never equal however alike they are. This rendering stands in for the structural
/// equality they lack: the walk reaches every node, including those inside those lists, and each
/// node prints its own kind, position, and scalar members — so a speaker filled in or a jump
/// indicator consumed reads as a difference.
/// </remarks>
internal static class ScriptShape
{
    /// <summary>The document's shape: every node it holds, in document order.</summary>
    public static string Of(ScriptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return string.Join(
            "\n",
            document.Body.SelectMany(block => block.DescendantsAndSelf()).Select(Describe));
    }

    private static string Describe(ScriptNode node) =>
        $"{node.GetType().Name} [{node.Span.Start},{node.Span.End}) {node}";
}
