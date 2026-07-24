using System.Globalization;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reads a choice option's leading code span into a <see cref="ChoiceWeight"/>. A weight is a
/// code span whose content ends with a percent sign — the signal that separates it from a game
/// call, none of which ends in <c>%</c>. The value before the sign is a non-negative number
/// (<see cref="NumberWeight"/>), empty (<see cref="AutoWeight"/>), or invalid, in which case
/// the caller reports <see cref="DialogueDown.Diagnostics.DiagnosticCatalog.InvalidChoiceWeight"/>.
/// A dynamic, game-state weight is deferred, so a quoted value is invalid for now.
/// </summary>
internal static class ChoiceWeightReader
{
    private const NumberStyles WeightNumberStyles =
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

    public static bool IsWeight(string content) => content.Trim().EndsWith('%');

    // Reads the weight value; null when it is neither a non-negative number nor a bare percent,
    // so the caller can report the invalid weight and recover.
    public static ChoiceWeight? Read(string content)
    {
        var value = content.Trim();
        value = value[..^1].Trim();
        if (value.Length == 0)
        {
            return new AutoWeight();
        }

        if (double.TryParse(value, WeightNumberStyles, CultureInfo.InvariantCulture, out var percentage)
            && percentage >= 0)
        {
            return new NumberWeight(percentage);
        }

        return null;
    }
}
