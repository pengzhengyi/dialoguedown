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

    private static readonly Gen<LabelScript> _script =
        Gen.Select(
            Gen.Enum<LabelPlacement>(),
            Label(_call),
            (placement, label) =>
                new LabelScript(Scene(Line(label.Text, placement)), placement, label.Text, label.Calls));

    private static readonly Gen<string> _queryLabel = Label(_query).Select(label => label.Text);

    /// <summary>A script holding one label in one of the four placements.</summary>
    public static Gen<LabelScript> Script() => _script;

    /// <summary>
    /// The text of a label that holds only words, emphasis, and queries, such as
    /// <c>dawn `"PlaceName"` *map `"Alice.Mood"`*</c>, which reads the same as speech.
    /// </summary>
    public static Gen<string> QueryLabel() => _queryLabel;

    // One to three segments, each a word and then a code span drawn from call, sometimes with
    // emphasis around both.
    private static Gen<(string Text, IReadOnlyList<LabelCall> Calls)> Label(Gen<LabelCall> call) =>
        Gen.Select(_word, call, Gen.Bool, Segment)
            .Array[1, 3]
            .Select(segments => (
                string.Join(" ", segments.Select(segment => segment.Text)),
                (IReadOnlyList<LabelCall>)[.. segments.Select(segment => segment.Call)]));

    private static (string Text, LabelCall Call) Segment(string word, LabelCall call, bool emphasized) =>
        (emphasized ? $"*{word} {call.Written}*" : $"{word} {call.Written}", call);

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
