namespace DialogueDown.Script.Ast;

/// <summary>
/// Read-only traversal helpers over the dialogue AST, shared by every stage that walks the
/// tree. The AST records stay plain data; walking their shape lives here, beside the nodes,
/// so the desugar, validation, and semantics passes share one description of the tree.
/// </summary>
internal static class ScriptNodeExtensions
{
    /// <summary>
    /// What a line says: its speech without the jump it may end in.
    /// </summary>
    /// <remarks>
    /// A jump is written inside the line it leaves from:
    /// <code>
    /// Alice: Follow me. =&gt; [the market](#the-market)
    /// </code>
    /// speaks <c>Follow me.</c> and then jumps to <c>#the-market</c>.
    /// </remarks>
    internal static IReadOnlyList<InlineFragment> Spoken(this Line line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return [.. line.Speech.Where(fragment => fragment is not Jump)];
    }

    /// <summary>
    /// The jumps a block leaves by, in the order they are written.
    /// </summary>
    /// <remarks>
    /// A jump is written inside the block it leaves from: a line holds it in its speech, and a
    /// control line among its effects.
    /// </remarks>
    internal static IEnumerable<Jump> Jumps(this ScriptBlock block) => block switch
    {
        Line line => line.Speech.OfType<Jump>(),
        ControlLine control => control.Effects.OfType<Jump>(),
        _ => [],
    };

    /// <summary>
    /// The words a menu shows for an option, or empty when the writer wrote nothing that could
    /// name it.
    /// </summary>
    /// <remarks>
    /// The label comes from the option's own first block: the words its line speaks or, when that
    /// block only jumps, the jump's own text.
    /// <code>
    /// - Alice: I'll wait here.
    /// - =&gt; [Take the east road](#the-market)
    /// </code>
    /// labels the options <c>I'll wait here.</c> and <c>Take the east road</c>. An option with no
    /// body has no label: the block after the choice is not its own.
    /// </remarks>
    internal static IReadOnlyList<InlineFragment> Label(this Choice option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return option.Body.FirstOrDefault() switch
        {
            Line line when line.Spoken() is { Count: > 0 } spoken => spoken,
            { } block => block.Jumps().FirstOrDefault()?.Label ?? [],
            _ => [],
        };
    }

    /// <summary>
    /// The words a node shows the player: a line's spoken words, or a jump's label. Empty for any
    /// other node.
    /// </summary>
    /// <remarks>
    /// These are the lists the playbook carries as speech.
    /// <code>
    /// Alice: Follow me. =&gt; [the market](#the-market)
    /// </code>
    /// shows <c>Follow me.</c> for the line and <c>the market</c> for the jump. An option's label is
    /// the words of its first line or the label of its jump, so reading every node's words reads
    /// each label once. A heading's title and a control line's commands are never shown.
    /// </remarks>
    internal static IReadOnlyList<InlineFragment> Shown(this ScriptNode node) => node switch
    {
        Line line => line.Spoken(),
        Jump jump => jump.Label,
        _ => [],
    };

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
    /// Every <see cref="Text"/> within the fragments, in document order, including the texts held
    /// inside other fragments, such as styled text, a link's label, or an image's alt text.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Take **the ![old](key.png) key**.
    /// </code>
    /// holds the texts <c>Take </c>, <c>the </c>, <c>old</c>, <c> key</c>, and <c>.</c>: the bold
    /// text holds three of them, one inside the image it holds.
    /// </remarks>
    internal static IEnumerable<Text> Texts(this IEnumerable<InlineFragment> fragments) =>
        fragments.SelectMany(fragment => fragment.DescendantsAndSelf()).OfType<Text>();

    /// <summary>
    /// The visible text of a fragment sequence: every <see cref="Text"/> anywhere within the
    /// fragments, concatenated in document order. Styled text or a link label still
    /// contributes its words, so this reads a heading title or label as plain text.
    /// </summary>
    internal static string PlainText(this IEnumerable<InlineFragment> fragments) =>
        string.Concat(fragments.Texts().Select(text => text.Content));

    /// <summary>
    /// The node's direct children in document order. A block, speaker, inline fragment, or other
    /// node of an unknown type throws, so a new node type is not skipped by accident.
    /// </summary>
    internal static IEnumerable<ScriptNode> Children(this ScriptNode node) => node switch
    {
        ScriptBlock block => BlockChildren(block),
        Choice choice => ConditionalBodyChildren(choice.Condition, choice.Body),
        Branch branch => ConditionalBodyChildren(branch.Condition, branch.Body),
        RandomOption option => option.IsConditional()
            ? [option.Condition!, option.Weight, .. option.Body]
            : [option.Weight, .. option.Body],
        ChoiceWeight => [],
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

    private static IEnumerable<ScriptNode> BlockChildren(ScriptBlock block) => block switch
    {
        Line line => LineChildren(line),
        ControlLine control => ControlLineChildren(control),
        Choices choices => choices.Options,
        RandomChoices random => random.Options,
        ControlBlock control => control.Branches,
        SceneHeading heading => heading.Title,

        _ => throw new ArgumentOutOfRangeException(
            nameof(block), block.GetType(), "Unhandled block type in Children()."),
    };

    private static IEnumerable<ScriptNode> SpeakerChildren(Speaker speaker) => speaker switch
    {
        SpeakerDeclaration declaration => declaration.Tags,
        PartialSpeakerDeclaration partial => partial.Tags,
        DefaultSpeaker or SpeakerReference => [],

        _ => throw new ArgumentOutOfRangeException(
            nameof(speaker), speaker.GetType(), "Unhandled speaker type in Children()."),
    };

    private static IEnumerable<ScriptNode> FragmentChildren(InlineFragment fragment) => fragment switch
    {
        StyledText styled => styled.Children,
        Image image => image.Alt,
        Link link => link.Label,
        Jump jump => jump.IsConditional() ? [jump.Condition!, .. jump.Label] : jump.Label,
        _ when IsLeaf(fragment) => [],

        _ => throw new ArgumentOutOfRangeException(
            nameof(fragment), fragment.GetType(), "Unhandled inline fragment type in Children()."),
    };

    // The inline fragments that hold no other fragment.
    private static bool IsLeaf(InlineFragment fragment) =>
        fragment is Text or LineBreak or JumpIndicator or Condition or GameCall or Tag;

    private static IEnumerable<ScriptNode> ConditionalBodyChildren(
        Condition? condition, IReadOnlyList<ScriptBlock> body) =>
        condition is not null ? [condition, .. body] : body;

    // Document order: a line's condition, when present, is written before its speaker and speech,
    // as in `Alice.HasKey?` Alice: I have the key.
    private static IEnumerable<ScriptNode> LineChildren(Line line)
    {
        if (line.Condition is not null)
        {
            yield return line.Condition;
        }

        if (line.Speaker is not null)
        {
            yield return line.Speaker;
        }

        foreach (var fragment in line.Speech)
        {
            yield return fragment;
        }
    }

    // Document order: a control line's condition, when present, is written before its effects.
    private static IEnumerable<ScriptNode> ControlLineChildren(ControlLine control)
    {
        if (control.Condition is not null)
        {
            yield return control.Condition;
        }

        foreach (var effect in control.Effects)
        {
            yield return effect;
        }
    }
}
