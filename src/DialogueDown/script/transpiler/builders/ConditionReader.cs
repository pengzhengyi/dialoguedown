using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads a code span whose content is a key followed by <c>?</c> into a
/// <see cref="Condition"/> — the key plus the sign that marks a boolean read. The key is
/// resolved by <see cref="QueryKeyReader"/>, so it may be quoted or unquoted, exactly as a
/// dynamic weight's key is. Any other code span (a value read, a command, or text with no
/// trailing <c>?</c>) is not a condition and yields null, so the caller falls back to game-call
/// building. <see cref="TryPeel"/> lifts this recognition to a Markdown inline sequence, the
/// shared move a conditional line and a conditional choice both make on a leading guard.
/// </summary>
internal static class ConditionReader
{
    // Null unless the whole code span is a key followed by a trailing '?'. Whitespace around
    // the key and the sign is insignificant, matching a value query and a dynamic weight.
    public static Condition? Read(string content, SourceSpan span)
    {
        var value = content.Trim();
        if (value.Length == 0 || value[^1] != '?')
        {
            return null;
        }

        var key = QueryKeyReader.Read(value[..^1].Trim());
        return key is null ? null : new Condition(key, span);
    }

    /// <summary>
    /// Peels a leading condition off a Markdown inline sequence: <c>true</c> with the
    /// <paramref name="condition"/> and the <paramref name="remainder"/> (its leading whitespace
    /// trimmed) when the first inline is a <c>`"key"?`</c> code span; <c>false</c> with the
    /// sequence returned unchanged otherwise. Each caller applies its own binding policy — the
    /// conditional line declines a guard a jump should claim, while a conditional choice always
    /// takes it.
    /// </summary>
    public static bool TryPeel(
        IReadOnlyList<MarkdownInline> inlines,
        out Condition condition,
        out IReadOnlyList<MarkdownInline> remainder)
    {
        if (inlines is [CodeSpanInline code, ..] && Read(code.Content, code.Span) is { } found)
        {
            condition = found;
            remainder = inlines.Skip(1).TrimLeadingWhitespace();
            return true;
        }

        condition = null!;
        remainder = inlines;
        return false;
    }
}
