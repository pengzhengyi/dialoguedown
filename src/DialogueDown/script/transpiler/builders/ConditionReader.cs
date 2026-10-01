using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads a code span whose content is a key followed by <c>?</c>, such as
/// <c>`Alice.HasKey?`</c>, into a <see cref="Condition"/>. The key is read by
/// <see cref="QueryKeyReader"/>, so it may be quoted or unquoted, exactly as a dynamic weight's
/// key is. Any other code span (a value read, a command, or text with no trailing <c>?</c>) is
/// not a condition and yields null, so the caller falls back to game-call building.
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
    /// Splits a leading condition off a Markdown inline sequence: <c>true</c> with the
    /// <paramref name="condition"/> and the <paramref name="remainder"/> (its leading whitespace
    /// trimmed) when the first inline is a condition code span; <c>false</c> with the sequence
    /// returned unchanged otherwise.
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
