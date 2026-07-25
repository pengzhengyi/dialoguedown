using System.Globalization;
using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads a choice option's leading code span into a <see cref="ChoiceWeight"/>. A weight is a
/// code span whose content ends with a percent sign — the signal that separates it from a game
/// call, none of which ends in <c>%</c>. The value before the sign is a non-negative number
/// (<see cref="NumberWeight"/>), a quoted query the runtime computes (<see cref="QueryWeight"/>),
/// empty (<see cref="AutoWeight"/>), or invalid, in which case the caller reports
/// <see cref="DialogueDown.Diagnostics.DiagnosticCatalog.InvalidChoiceWeight"/>.
/// </summary>
internal static class ChoiceWeightReader
{
    private const NumberStyles WeightNumberStyles =
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

    public static bool IsWeight(string content) => content.Trim().EndsWith('%');

    // Reads the weight into a spanned node; null when the value is neither a non-negative number
    // nor a bare percent, so the caller can report the invalid weight and recover.
    public static ChoiceWeight? Read(string content, SourceSpan span)
    {
        var value = content.Trim();
        value = value[..^1].Trim();
        if (value.Length == 0)
        {
            return new AutoWeight(span);
        }

        if (TryReadQueryKey(value, out var key))
        {
            return new QueryWeight(key, span);
        }

        if (double.TryParse(value, WeightNumberStyles, CultureInfo.InvariantCulture, out var percentage)
            && percentage >= 0)
        {
            return new NumberWeight(percentage, span);
        }

        return null;
    }

    // A dynamic weight is a query, recognized by the shared query grammar rather than a
    // re-derived quoted string. ConsumeAll requires the whole value to be that query, so
    // trailing text is not mistaken for one.
    private static bool TryReadQueryKey(string value, out string key)
    {
        var result = GameCallParser.Query.ConsumeAll(new ParseInput(value, 0));
        if (result.Success)
        {
            key = result.MatchedValue.Key;
            return true;
        }

        key = string.Empty;
        return false;
    }
}
