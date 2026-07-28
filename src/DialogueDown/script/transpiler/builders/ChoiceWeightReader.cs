using System.Globalization;
using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads a choice option's leading code span into a <see cref="ChoiceWeight"/>. A weight is a
/// code span whose content ends with a percent sign — the signal that separates it from a game
/// call, none of which ends in <c>%</c>. The value before the sign is a non-negative number
/// (<see cref="NumberWeight"/>), a key the runtime computes into a weight
/// (<see cref="QueryWeight"/>, written quoted or unquoted), empty (<see cref="AutoWeight"/>), or
/// an invalid number such as a negative one, in which case the caller reports
/// <see cref="DialogueDown.Diagnostics.DiagnosticCatalog.InvalidChoiceWeight"/>.
/// </summary>
internal static class ChoiceWeightReader
{
    private const NumberStyles WeightNumberStyles =
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

    public static bool IsWeight(string content) => content.Trim().EndsWith('%');

    // Reads the weight into a spanned node; null when the value is a number the weight cannot
    // use (a negative one), so the caller can report the invalid weight and recover.
    public static ChoiceWeight? Read(string content, SourceSpan span)
    {
        var value = content.Trim();
        value = value[..^1].Trim();
        if (value.Length == 0)
        {
            return new AutoWeight(span);
        }

        // A number is a static weight; only a non-negative one is valid. Trying the number first
        // keeps `50%` the number 50 rather than a key named "50".
        if (double.TryParse(value, WeightNumberStyles, CultureInfo.InvariantCulture, out var percentage))
        {
            return percentage >= 0 ? new NumberWeight(percentage, span) : null;
        }

        // Any other non-empty text is a key — quoted or unquoted — the runtime computes.
        var key = QueryKeyReader.Read(value);
        return key is null ? null : new QueryWeight(key, span);
    }
}
