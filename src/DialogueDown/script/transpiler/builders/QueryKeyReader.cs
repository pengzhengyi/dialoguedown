using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads the key shared by a condition and a dynamic weight — the text left once a trailing
/// sigil (<c>?</c> or <c>%</c>) is stripped. A quoted key (<c>"..."</c>) yields its inner text
/// through the same query grammar a value read uses; any other non-empty text is an unquoted
/// key taken verbatim, so a key may hold spaces. Empty text is not a key and yields
/// <c>null</c>. Only a sigil-terminated form calls this — the sigil marks where the key ends,
/// so no quotes are needed to delimit it; a value read has no sigil and keeps its quoted-only
/// grammar.
/// </summary>
internal static class QueryKeyReader
{
    public static string? Read(string text)
    {
        if (GameCallParser.Query.TryParseAll(text, out var query))
        {
            return query.Key;
        }

        return text.Length > 0 ? text : null;
    }
}
