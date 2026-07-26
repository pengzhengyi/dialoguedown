using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads a code span whose content is a query followed by <c>?</c> into a
/// <see cref="Condition"/> — the query grammar plus the sign that marks a boolean read. The
/// query is recognized by the shared <see cref="GameCallParser.Query"/> grammar rather than a
/// re-derived quoted string, so it stays in step with a value query. Any other code span (a
/// plain query, a command, or text) is not a condition and yields null, so the caller falls
/// back to game-call building.
/// </summary>
internal static class ConditionReader
{
    // Null unless the whole code span is a query followed by a trailing '?'. Whitespace around
    // the query and the sign is insignificant, matching a value query and a choice weight.
    public static Condition? Read(string content, SourceSpan span)
    {
        var value = content.Trim();
        if (value.Length == 0 || value[^1] != '?')
        {
            return null;
        }

        var key = value[..^1].Trim();
        return GameCallParser.Query.TryParseAll(key, out var query)
            ? new Condition(query.Key, span)
            : null;
    }
}
