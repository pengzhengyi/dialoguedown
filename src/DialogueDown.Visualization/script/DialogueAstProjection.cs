using System.Globalization;
using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization;

/// <summary>
/// Projects the Dialogue AST for display: it labels every node type, yields each node's
/// children, and attaches the original source text each node was produced from (sliced
/// from the node's span). Like the Markdown AST, its families share no single base usable
/// as the node type (<see cref="ScriptDocument"/> is a plain container; blocks, speakers,
/// inlines, and tags are separate <see cref="ScriptNode"/> families), so the node type is
/// <see cref="object"/> and each node is matched by its runtime type.
/// </summary>
internal sealed class DialogueAstProjection : INodeProjection<object>
{
    // Semantic categories: the same cross-stage vocabulary the Markdown projection uses,
    // so corresponding concepts share a color (a code span and the game call it becomes
    // are both "call"). "tag" is new — the Dialogue AST is the first stage with tags.
    private const string DocumentCategory = "document";
    private const string StructureCategory = "structure";
    private const string SpeechCategory = "speech";
    private const string TextCategory = "text";
    private const string ChoiceCategory = "choice";
    private const string JumpCategory = "jump";
    private const string MediaCategory = "media";
    private const string CallCategory = "call";
    private const string StylingCategory = "styling";
    private const string BreakCategory = "break";
    private const string TagCategory = "tag";

    private const string DialogueTitle = "Dialogue AST";

    private const string DialogueDescription =
        "The dialogue tree the transpiler derives from the Markdown — its lines and " +
        "speakers, choices, styled text, links, game calls, and tags, each tied to the " +
        "text it came from.";

    private readonly string _source;
    private readonly string _title;
    private readonly string _description;

    /// <summary>The Dialogue AST projection — the transpiler's output stage.</summary>
    public DialogueAstProjection(string source)
        : this(source, DialogueTitle, DialogueDescription)
    {
    }

    /// <summary>
    /// Projects the same Dialogue AST node vocabulary under a given tab
    /// <paramref name="title"/> and <paramref name="description"/>, so one class renders
    /// both the Dialogue AST and the Desugared AST tabs (the desugared tree is the same
    /// vocabulary with a default speaker filled and jumps assembled).
    /// </summary>
    public DialogueAstProjection(string source, string title, string description)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(description);
        _source = source;
        _title = title;
        _description = description;
    }

    public string Title => _title;

    public string Description => _description;

    public NodeDescription Describe(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        NodeDescription description = node switch
        {
            ScriptDocument => new("Document", source: _source, category: DocumentCategory),
            SceneHeading heading => new(
                $"Scene heading (H{heading.Level})",
                [new("level", $"{heading.Level}"), SpanAttribute(heading.Span)],
                Slice(heading.Span),
                StructureCategory),
            Line line => new("Line", [SpanAttribute(line.Span)], Slice(line.Span), SpeechCategory),
            Choices choices => new(
                choices.IsOrdered ? "Choices (ordered)" : "Choices (unordered)",
                [SpanAttribute(choices.Span)],
                Slice(choices.Span),
                ChoiceCategory),
            Choice choice => new("Choice", [SpanAttribute(choice.Span)], Slice(choice.Span), ChoiceCategory),
            RandomChoices random => new(
                "Random choices",
                [SpanAttribute(random.Span)],
                Slice(random.Span),
                ChoiceCategory),
            RandomOption option => new(
                "Random option",
                [new("weight", WeightText(option.Weight)), SpanAttribute(option.Span)],
                Slice(option.Span),
                ChoiceCategory),
            SpeakerDeclaration speaker => new(
                "Speaker (declaration)",
                [new("name", speaker.Name), .. Optional("id", speaker.Id), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            PartialSpeakerDeclaration speaker => new(
                "Speaker (partial)",
                [new("id", speaker.Id), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            SpeakerNameReference speaker => new(
                "Speaker (by name)",
                [new("name", speaker.Name), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            SpeakerIdReference speaker => new(
                "Speaker (by id)",
                [new("id", speaker.Id), SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            DefaultSpeaker speaker => new(
                "Speaker (default)",
                [SpanAttribute(speaker.Span)],
                Slice(speaker.Span),
                SpeechCategory),
            Text text => new(
                "Text", [new("text", text.Content), SpanAttribute(text.Span)], Slice(text.Span), TextCategory),
            StyledText styled => new(
                $"Styled text ({styled.Style})", [SpanAttribute(styled.Span)], Slice(styled.Span), StylingCategory),
            Link link => new(
                "Link",
                [new("target", link.Target), new("label", InlineText(link.Label)), SpanAttribute(link.Span)],
                Slice(link.Span),
                JumpCategory),
            Image image => new(
                "Image",
                [new("source", image.Source), new("alt", InlineText(image.Alt)), SpanAttribute(image.Span)],
                Slice(image.Span),
                MediaCategory),
            DefaultCommand command => new(
                "Command (default)",
                [new("action", command.Action), SpanAttribute(command.Span)],
                Slice(command.Span),
                CallCategory),
            CustomCommand command => new(
                $"Command ({command.Name})",
                [new("name", command.Name), new("args", string.Join(", ", command.Args)), SpanAttribute(command.Span)],
                Slice(command.Span),
                CallCategory),
            Query query => new(
                "Query", [new("key", query.Key), SpanAttribute(query.Span)], Slice(query.Span), CallCategory),
            JumpIndicator jump => new(
                "Jump indicator", [SpanAttribute(jump.Span)], Slice(jump.Span), JumpCategory),
            Jump jump => new(
                "Jump",
                [new("target", jump.Target), new("label", InlineText(jump.Label)), SpanAttribute(jump.Span)],
                Slice(jump.Span),
                JumpCategory),
            LineBreak lineBreak => new(
                "Line break", [SpanAttribute(lineBreak.Span)], Slice(lineBreak.Span), BreakCategory),
            ReservedTag tag => new(
                "Tag (reserved)",
                [new("name", tag.Name), .. Optional("value", tag.Value), SpanAttribute(tag.Span)],
                Slice(tag.Span),
                TagCategory),
            CustomTag tag => new(
                "Tag (custom)",
                [new("name", tag.Name), .. Optional("value", tag.Value), SpanAttribute(tag.Span)],
                Slice(tag.Span),
                TagCategory),
            _ => throw new ArgumentException(
                $"Unsupported Dialogue AST node type '{node.GetType().Name}'.", nameof(node)),
        };

        // Attach the structured span centrally so every arm above stays focused on its
        // label/attributes: every real node is a spanned ScriptNode, the root spans the
        // whole document, and a synthetic node's empty span yields none.
        return description with { Span = SpanOf(node) };
    }

    public IEnumerable<object> Neighbors(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            ScriptDocument document => document.Body,
            SceneHeading heading => heading.Title,
            Line line => LineChildren(line.Speaker, line.Speech),
            Choices choices => choices.Options,
            Choice choice => choice.Body,
            RandomChoices random => random.Options,
            RandomOption option => option.Body,
            SpeakerDeclaration speaker => speaker.Tags,
            PartialSpeakerDeclaration speaker => speaker.Tags,
            StyledText styled => styled.Children,
            Link link => link.Label,
            Jump jump => jump.Label,
            Image image => image.Alt,
            _ => [],
        };
    }

    private static DisplayAttribute SpanAttribute(SourceSpan span) =>
        new("span", $"[{span.Start}, {span.End})");

    // A random option's weight, as the author would write it: a percentage or a bare "%".
    private static string WeightText(ChoiceWeight weight) => weight switch
    {
        NumberWeight number => $"{number.Percentage.ToString("0.##", CultureInfo.InvariantCulture)}%",
        AutoWeight => "%",
        _ => throw new ArgumentException(
            $"Unsupported choice weight type '{weight.GetType().Name}'.", nameof(weight)),
    };

    // A nullable attribute (a speaker's id, a tag's value) is shown only when present.
    private static IEnumerable<DisplayAttribute> Optional(string name, string? value) =>
        value is null ? [] : [new DisplayAttribute(name, value)];

    // A line's children are its optional speaker followed by its speech fragments.
    private static IEnumerable<object> LineChildren(Speaker? speaker, IReadOnlyList<InlineFragment> speech)
    {
        if (speaker is not null)
        {
            yield return speaker;
        }

        foreach (var fragment in speech)
        {
            yield return fragment;
        }
    }

    // A link label or image alt is a run of inline fragments; flatten it to plain text for
    // the attribute display (the node's own span still points at the exact source).
    private static string InlineText(IReadOnlyList<InlineFragment> fragments) =>
        Visualization.InlineText.Of(fragments);

    // Spans come from the Markdown source locations; clamp defensively so a diagnostics
    // view never throws on a stray span. An empty span yields no source: the node marks a
    // position rather than a range of text — it was inserted by a stage (a filled default
    // speaker), so it has nothing to slice.
    private string? Slice(SourceSpan span)
    {
        if (span.IsEmpty)
        {
            return null;
        }

        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return _source[start..end];
    }

    // The node's editable source range: the whole document for the root, the clamped span
    // for a spanned node, and none for a synthetic (empty-span) node.
    private DisplaySpan? SpanOf(object node) => node switch
    {
        ScriptDocument => new DisplaySpan(0, _source.Length),
        ScriptNode scriptNode => ToSpan(scriptNode.Span),
        _ => null,
    };

    // The structured, clamped span a client splices with — the same clamping as Slice, so a
    // node's span and its sliced source always agree. An empty span yields none (synthetic).
    private DisplaySpan? ToSpan(SourceSpan span)
    {
        if (span.IsEmpty)
        {
            return null;
        }

        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return new DisplaySpan(start, end);
    }
}
