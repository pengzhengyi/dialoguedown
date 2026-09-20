using DialogueDown.Markdown;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler.Parsers;

/// <summary>
/// Re-tokenizes a plain string into inline leaves: pieces of plain text
/// (<see cref="TextLeaf"/>), a tag (<see cref="TagLeaf"/>) where the writer embedded
/// <c>#tag</c>, and a jump (<see cref="JumpLeaf"/>) where they wrote <c>=&gt;</c>.
/// Markdown treats these as ordinary text, so this is where they are recognized. Tags
/// are recognized in every context; jumps only where the context allows (they are
/// dropped inside a label). Each leaf keeps the range it covered, and neighboring text
/// is joined into one piece.
/// </summary>
internal static class InlineLeafTokenizer
{
    // The arrow the writer types; shared by the parser and StartsWithJumpIndicator so the
    // arrow's spelling and its escape rule cannot drift apart.
    private const string Arrow = "=>";

    // As many characters as possible that start neither a tag ('#') nor a jump ('=').
    private static readonly IParser<InlineLeaf> _text = SuperpowerParser.Wrap(
        Character.ExceptIn('#', '=').AtLeastOnce()
            .Select(chars => (InlineLeaf)new TextLeaf(new string(chars))));

    private static readonly IParser<InlineLeaf> _jump = SuperpowerParser.Wrap(
        Span.EqualTo(Arrow).Value((InlineLeaf)new JumpLeaf()));

    private static readonly IParser<InlineLeaf> _tag =
        TagParser.Token.Select(tag => (InlineLeaf)new TagLeaf(tag));

    // A single '#' or '=' that began neither a tag nor a jump survives as plain text.
    private static readonly IParser<InlineLeaf> _stray = SuperpowerParser.Wrap(
        Character.AnyChar.Select(c => (InlineLeaf)new TextLeaf(c.ToString())));

    public static IReadOnlyList<Spanned<InlineLeaf>> Tokenize(
        ParseInput input, bool allowJumps, bool escapedFirstCharacter = false)
    {
        // Tags are recognized in every context; jumps only where the context allows.
        var recognized = allowJumps ? _jump.Or(_tag) : _tag;

        var leaves = new List<Spanned<InlineLeaf>>();
        var rest = input;
        if (escapedFirstCharacter)
        {
            // An escaped leading character is text: the whole sigil that begins there
            // goes literal, and a character that begins no sigil is literal alone.
            leaves.Add(LiteralizeLeadingCharacter(recognized, input, out rest));
        }

        leaves.AddRange(TokenizeRest(recognized, rest));
        return Coalesce(leaves);
    }

    /// <summary>
    /// Whether this text opens with the jump indicator (<c>=&gt;</c>) written unescaped. A
    /// stage that must decide before tokenizing — whether a condition guards a jump, say —
    /// asks here, so the arrow's spelling and its escape rule stay in one place.
    /// </summary>
    public static bool StartsWithJumpIndicator(this TextInline text) =>
        !text.IsFirstCharacterEscaped && text.Text.StartsWith(Arrow, StringComparison.Ordinal);

    private static IReadOnlyList<Spanned<InlineLeaf>> TokenizeRest(
        IParser<InlineLeaf> recognized, ParseInput input)
    {
        var leaves = recognized.Or(_text).Or(_stray).Located().Repeated().ConsumeAll(input);
        return leaves.MatchedValue;
    }

    // An escaped leading character is literal. When a sigil begins at that character,
    // the whole sigil goes literal with it (`\##default` writes "##default"); when no
    // sigil begins there, only the character itself is (`\=#tag` keeps "#tag" a tag).
    private static Spanned<InlineLeaf> LiteralizeLeadingCharacter(
        IParser<InlineLeaf> recognized, ParseInput input, out ParseInput rest)
    {
        // Try the sigil parser at the escaped character: on a match it reports how far
        // the sigil reaches, and on a miss it consumes nothing.
        var match = recognized.Consume(input);

        // A miss literalizes exactly the one escaped character — the backslash escaped
        // that character and nothing else. A match literalizes the sigil's whole length,
        // so a reserved `##default` cannot split into a literal `#` and a custom tag.
        var length = match.Success ? match.MatchedLength : 1;

        rest = input.Advance(length);
        return new Spanned<InlineLeaf>(
            new TextLeaf(input.Text[..length]), new TextRange(input.Position, length));
    }

    private static IReadOnlyList<Spanned<InlineLeaf>> Coalesce(
        IReadOnlyList<Spanned<InlineLeaf>> leaves)
    {
        var merged = new List<Spanned<InlineLeaf>>();
        foreach (var leaf in leaves)
        {
            if (leaf.Value is TextLeaf text
                && merged.Count > 0
                && merged[^1].Value is TextLeaf previous)
            {
                // The tokenizer emits contiguous ranges, so joining them is safe; the
                // '+' operator enforces that contiguity.
                var joined = merged[^1].Range + leaf.Range;
                merged[^1] = new Spanned<InlineLeaf>(new TextLeaf(previous.Content + text.Content), joined);
            }
            else
            {
                merged.Add(leaf);
            }
        }

        return merged;
    }
}
