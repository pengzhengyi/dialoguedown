using System.Collections.Immutable;
using System.Globalization;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Visualization.Playbook;

/// <summary>
/// Writes the one plain line a report shows for a playbook node.
/// </summary>
/// <remarks>
/// The report reads a node without the reader having to parse its JSON, so each kind is reduced to
/// the shortest run of pseudocode that still says what the node holds. Words in capitals are the
/// table's own, a name in angle brackets stands where a value is missing, round brackets always
/// mean a command, and anything else came from the script.
/// </remarks>
internal static class PlaybookNodeSummary
{
    /// <summary>Stands in for a speaker the playbook's speaker list does not hold.</summary>
    private const string UnknownSpeaker = "<unknown>";

    /// <summary>Stands in for a speaker the writer never named.</summary>
    private const string AnonymousSpeaker = "<anonymous>";

    /// <summary>Stands in for a line the writer gave no words to.</summary>
    private const string NoSpeech = "<no speech>";

    /// <summary>The summary of a node where a run stops.</summary>
    private const string EndOfScript = "END";

    /// <summary>How a control node reads when it only moves from one scene to the next.</summary>
    private const string CarriesOn = "CONTINUE";

    /// <summary>How a choice option reads when the writer spelled out no words for it.</summary>
    private const string NoLabel = "<no label>";

    /// <summary>How an arm of a random choice reads when its share is left to the other arms.</summary>
    private const string Evenly = "evenly";

    /// <summary>
    /// Summarizes one node, leading with the node's own condition when it carries one.
    /// </summary>
    /// <param name="node">The node to summarize.</param>
    /// <param name="speakers">The playbook's speakers, which a line addresses by index.</param>
    /// <returns>One line of plain text, or the empty string for a kind not yet covered.</returns>
    public static string Of(Node node, ImmutableArray<PlaybookSpeaker> speakers)
    {
        ArgumentNullException.ThrowIfNull(node);

        var body = BodyOf(node, speakers);
        if (body.Length == 0 || ConditionOf(node) is not { } condition)
        {
            return body;
        }

        return $"IF {condition.Key} THEN {body}";
    }

    private static string BodyOf(Node node, ImmutableArray<PlaybookSpeaker> speakers) =>
        node switch
        {
            LineNode line => Line(line, speakers),
            EndNode => EndOfScript,
            BranchNode branch => Branch(branch),
            ControlNode control => Control(control),
            ChoiceNode choice => Choice(choice),
            RandomChoiceNode random => RandomChoice(random),
            _ => string.Empty,
        };

    private static string Line(LineNode line, ImmutableArray<PlaybookSpeaker> speakers)
    {
        var speech = SpeechText.Of(line.Speech).Trim();
        return $"{NameOf(line.Speaker, speakers)}: {(speech.Length > 0 ? speech : NoSpeech)}";
    }

    // A line addresses its speaker by index, so an index the playbook does not carry means the
    // reference broke rather than that nobody spoke.
    private static string NameOf(int speaker, ImmutableArray<PlaybookSpeaker> speakers) =>
        speaker >= 0 && speaker < speakers.Length
            ? speakers[speaker].Name ?? AnonymousSpeaker
            : UnknownSpeaker;

    private static string Branch(BranchNode branch) =>
        string.Join(
            " ",
            branch.Out
                .OfType<BranchEdge>()
                .OrderBy(arm => arm.Order)
                .Select((arm, index) => Arm(arm, index == 0)));

    // The first arm is read as a plain IF and every arm after it is introduced by ELSE. An
    // unguarded arm has no IF of its own, so that introduction is all it carries.
    private static string Arm(BranchEdge arm, bool isFirst) =>
        (isFirst, arm.Condition) switch
        {
            (true, KeyCondition key) => $"IF {key.Key} THEN {arm.Target}",
            (false, KeyCondition key) => $"ELSE IF {key.Key} THEN {arm.Target}",
            _ => $"ELSE {arm.Target}",
        };

    // A host only performs commands, so the other speech kinds render as nothing and drop out of
    // the joined line.
    private static string Control(ControlNode control) =>
        Commands(control.Effects) is { Length: > 0 } commands
            ? commands
            : WhereItGoes(control);

    private static string Commands(ImmutableArray<SpeechFragment> effects) =>
        string.Join("; ", effects.Select(Effect).Where(text => text.Length > 0));

    private static string Effect(SpeechFragment fragment) =>
        fragment switch
        {
            DefaultCommandFragment command => $"({command.Action})",
            CustomCommandFragment command => $"{command.Name}({string.Join(", ", command.Args)})",
            _ => string.Empty,
        };

    // A control node with nothing to perform moves straight on, and the only words left are the
    // ones a divert was named with.
    private static string WhereItGoes(ControlNode control) =>
        FirstDivertWords(control.Out) is { } words ? $"⇒ {words}" : CarriesOn;

    private static string? FirstDivertWords(ImmutableArray<Edge> edges) =>
        edges
            .OfType<DivertEdge>()
            .Select(divert => SpeechText.Of(divert.Label).Trim())
            .FirstOrDefault(words => words.Length > 0);

    private static string Choice(ChoiceNode choice) =>
        string.Join(" || ", choice.Out.OfType<OptionEdge>().Select(Option));

    private static string Option(OptionEdge option)
    {
        var label = SpeechText.Of(option.Label).Trim();
        return $"{(label.Length > 0 ? label : NoLabel)}{ConditionSuffix(option.Condition)}";
    }

    private static string RandomChoice(RandomChoiceNode random)
    {
        var arms = random.Out.OfType<RandomOptionEdge>().ToImmutableArray();
        return $"DRAW 1 OF {arms.Length}: {string.Join(" || ", arms.Select(Odds))}";
    }

    private static string Odds(RandomOptionEdge arm) =>
        $"{Share(arm.Weight)}{ConditionSuffix(arm.Condition)}";

    private static string Share(ChoiceWeight weight) =>
        weight switch
        {
            NumberWeight number => $"{number.Percentage.ToString(CultureInfo.InvariantCulture)}%",
            QueryWeight query => $"{{{query.Key}}}",
            AutoWeight => Evenly,
            _ => string.Empty,
        };

    // An option and a random arm both carry their condition after their own text, so the shape is
    // written once.
    private static string ConditionSuffix(Condition? condition) =>
        condition is KeyCondition key ? $" IF {key.Key}" : string.Empty;

    // A condition lives on the kinds that can carry one, so it is read here rather than repeated
    // in each kind's own summary.
    private static KeyCondition? ConditionOf(Node node) =>
        node switch
        {
            LineNode line => line.Condition as KeyCondition,
            ControlNode control => control.Condition as KeyCondition,
            _ => null,
        };
}
