namespace DialogueDown.Script.Ast;

/// <summary>
/// Read-only traversal helpers over the dialogue AST, shared by every stage that walks the
/// tree. The AST records stay plain data; walking their shape lives here, beside the nodes,
/// so the desugar, validation, and semantics passes share one description of the tree.
/// </summary>
internal static class ScriptNodeExtensions
{
    /// <summary>
    /// Yields <paramref name="node"/> and then each descendant, depth-first in document
    /// order (a node before its children). Returning a sequence lets callers compose with
    /// LINQ; the script's nesting is shallow, so recursion is safe.
    /// </summary>
    internal static IEnumerable<ScriptNode> DescendantsAndSelf(this ScriptNode node)
    {
        yield return node;
        foreach (var child in node.Children())
        {
            foreach (var descendant in child.DescendantsAndSelf())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// The visible text of a fragment sequence: every <see cref="Text"/> anywhere within the
    /// fragments, concatenated in document order. A styled run or a link label still
    /// contributes its words, so this reads a heading title or label as plain text.
    /// </summary>
    internal static string PlainText(this IEnumerable<InlineFragment> fragments) =>
        string.Concat(fragments
            .SelectMany(fragment => fragment.DescendantsAndSelf())
            .OfType<Text>()
            .Select(text => text.Content));

    /// <summary>
    /// The node's direct children in document order. Dispatch is split by node category
    /// (block, speaker, inline fragment) so each switch stays small; every concrete type is
    /// handled, and an unhandled one throws rather than being silently skipped as the AST
    /// grows.
    /// </summary>
    internal static IEnumerable<ScriptNode> Children(this ScriptNode node) => node switch
    {
        ScriptBlock block => BlockChildren(block),
        Choice choice => choice.Body,
        RandomOption option => option.Body,
        Speaker speaker => SpeakerChildren(speaker),
        InlineFragment fragment => FragmentChildren(fragment),

        _ => throw new ArgumentOutOfRangeException(
            nameof(node), node.GetType(), "Unhandled script node type in Children()."),
    };

    /// <summary>
    /// The node's concrete type and each base up to and including <see cref="ScriptNode"/>
    /// (object is excluded). Filing a node under every type in this chain lets a base-type
    /// query such as <c>OfType&lt;Speaker&gt;()</c> find it.
    /// </summary>
    internal static IEnumerable<Type> TypeChainToScriptNode(this ScriptNode node)
    {
        // The walk stops at ScriptNode — object fails the assignability check — so
        // BaseType is never null while looping.
        for (Type type = node.GetType();
            typeof(ScriptNode).IsAssignableFrom(type);
            type = type.BaseType!)
        {
            yield return type;
        }
    }

    // Blocks own the content that follows them: a line's speaker and speech, a choice
    // set's options, or a heading's title fragments.
    private static IEnumerable<ScriptNode> BlockChildren(ScriptBlock block) => block switch
    {
        Line line => LineChildren(line),
        Choices choices => choices.Options,
        RandomChoices random => random.Options,
        SceneHeading heading => heading.Title,

        _ => throw new ArgumentOutOfRangeException(
            nameof(block), block.GetType(), "Unhandled block type in Children()."),
    };

    // A declaration (full or partial) carries tags; every other speaker shape is a leaf.
    private static IEnumerable<ScriptNode> SpeakerChildren(Speaker speaker) => speaker switch
    {
        SpeakerDeclaration declaration => declaration.Tags,
        PartialSpeakerDeclaration partial => partial.Tags,
        DefaultSpeaker or SpeakerReference => [],

        _ => throw new ArgumentOutOfRangeException(
            nameof(speaker), speaker.GetType(), "Unhandled speaker type in Children()."),
    };

    // Only the four inline containers expose nested fragments; every other fragment (text,
    // breaks, game calls, tags) is a leaf, so an unrecognized one defaults to no children.
    private static IEnumerable<ScriptNode> FragmentChildren(InlineFragment fragment) => fragment switch
    {
        StyledText styled => styled.Children,
        Image image => image.Alt,
        Link link => link.Label,
        Jump jump => jump.Label,

        _ => [],
    };

    // A line's speaker (when present) comes before its speech, so traversal keeps
    // document order.
    private static IEnumerable<ScriptNode> LineChildren(Line line)
    {
        if (line.Speaker is not null)
        {
            yield return line.Speaker;
        }

        foreach (var fragment in line.Speech)
        {
            yield return fragment;
        }
    }
}
