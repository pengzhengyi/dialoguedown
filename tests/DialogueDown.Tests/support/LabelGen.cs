using CsCheck;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Generates scripts whose one label holds words, emphasis, and at least one code span, for
/// property tests about game calls in labels.
/// </summary>
/// <remarks>
/// <see cref="ScriptGen"/> draws a game call in a label rarely, if at all, so these properties have
/// a generator of their own. Each script is a fixed scene around one label, and the generator
/// returns a model of what it placed, so a property states its expectation from the model rather
/// than from the compiler's own reading. A query is drawn twice as often as a command or a
/// condition, so a fair share of labels hold only queries and compile.
/// </remarks>
internal static class LabelGen
{
    private static readonly Gen<string> _word = Gen.OneOfConst("dawn", "map", "gate", "coin", "road");

    private static readonly Gen<string> _key =
        Gen.OneOfConst("PlaceName", "CompanionName", "Alice.Mood");

    private static readonly Gen<LabelCall> _query =
        _key.Select(key => new LabelCall(LabelCallKind.Query, $"`\"{key}\"`", key));

    private static readonly Gen<LabelCall> _command =
        Gen.OneOf(
            _word.Select(word => new LabelCall(LabelCallKind.Command, $"`(\"{word}\")`", word)),
            _word.Select(word => new LabelCall(LabelCallKind.Command, $"`GiveGold(\"{word}\")`", "GiveGold")));

    private static readonly Gen<LabelCall> _condition =
        _key.Select(key => new LabelCall(LabelCallKind.Condition, $"`\"{key}\"?`", key));

    private static readonly Gen<LabelCall> _call =
        Gen.Frequency((2, _query), (1, _command), (1, _condition));

    // One code span after a word, sometimes with emphasis around both.
    private static readonly Gen<(string Text, LabelCall Call)> _segment =
        Gen.Select(
            _word, _call, Gen.Bool,
            (word, call, emphasized) =>
                (emphasized ? $"*{word} {call.Written}*" : $"{word} {call.Written}", call));

    private static readonly Gen<LabelScript> _script =
        Gen.Select(
            Gen.Enum<LabelPlacement>(),
            _segment.Array[1, 3],
            (placement, segments) =>
            {
                var label = string.Join(" ", segments.Select(segment => segment.Text));

                return new LabelScript(
                    Scene(Line(label, placement)),
                    placement,
                    label,
                    [.. segments.Select(segment => segment.Call)]);
            });

    /// <summary>A script holding one label in one of the four placements.</summary>
    public static Gen<LabelScript> Script() => _script;

    private static string Line(string label, LabelPlacement placement) => placement switch
    {
        LabelPlacement.Link => $"Alice: See [{label}](#the-inn).",
        LabelPlacement.AltText => $"Alice: ![{label}](a.png)",
        LabelPlacement.MenuOption => $"- => [{label}](#the-inn)",
        LabelPlacement.Divert => $"Alice: This way. => [{label}](#the-inn)",
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unknown placement."),
    };

    private static string Scene(string line) =>
        $"""
        # The Square

        Alice: Where to?

        {line}

        # The Inn

        Alice: Here we are.

        """;
}
