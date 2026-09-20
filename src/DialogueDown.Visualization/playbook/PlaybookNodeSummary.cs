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
/// Writes the one plain line a report shows for a playbook node, as the labeled pieces the table
/// draws.
/// </summary>
/// <remarks>
/// The report reads a node without the reader having to parse its JSON, so each kind is reduced to
/// the shortest run of pseudocode that still says what the node holds. Every piece also says what
/// it is — a speaker, a writer's words, the table's own grammar, a command, a query, or a marker
/// standing where a value is missing — so the client draws the piece by the role it was given
/// rather than re-reading the line and guessing where the writer's words stop.
/// </remarks>
internal static class PlaybookNodeSummary
{
    /// <summary>Marks a query as the boolean member of the family, as the script writes it.</summary>
    private const string BooleanQuery = "?";

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

    /// <summary>What a summary cut at the cap trails.</summary>
    private const string Ellipsis = "…";

    // A separator carries the spaces that join it to its neighbors, so the pieces partition the
    // line exactly and the table's grammar is never a space that came from the script.
    private const string SpeakerDivider = ": ";
    private const string NodeCondition = "IF ";
    private const string NodeConsequence = " THEN ";
    private const string OptionSeparator = " || ";
    private const string Guard = " IF ";
    private const string CommandSeparator = "; ";
    private const string DivertArrow = "⇒ ";
    private const string BranchElse = " ELSE ";
    private const string BranchElseIf = " ELSE IF ";
    private const string DrawPrefix = "DRAW 1 FROM ";
    private const string OddsDivider = ": ";

    /// <summary>How many characters a finished summary keeps before it is cut.</summary>
    /// <remarks>
    /// One enormous paragraph would otherwise travel in the report payload and wrap a single row
    /// into a wall of text. The cap is generous enough that most rows never meet it.
    /// </remarks>
    private const int SummaryCap = 200;


    /// <summary>
    /// Writes one node's line as the labeled pieces the table draws, leading with the node's own
    /// condition when it carries one.
    /// </summary>
    /// <param name="node">The node to summarize.</param>
    /// <param name="speakers">The playbook's speakers, which a line addresses by index.</param>
    /// <returns>The pieces, in the order they read, or none for a kind not yet covered.</returns>
    public static ImmutableArray<PlaybookSegmentView> SegmentsOf(
        Node node, ImmutableArray<PlaybookSpeaker> speakers)
    {
        ArgumentNullException.ThrowIfNull(node);

        var body = BodyOf(node, speakers);
        if (body.Length == 0 || ConditionOf(node) is not { } condition)
        {
            return Capped(body);
        }

        return Capped(
        [
            PlaybookSegmentView.Keyword(NodeCondition),
            Condition(condition.Key),
            PlaybookSegmentView.Keyword(NodeConsequence),
            .. body,
        ]);
    }

    private static ImmutableArray<PlaybookSegmentView> BodyOf(
        Node node, ImmutableArray<PlaybookSpeaker> speakers) =>
        node switch
        {
            LineNode line => Line(line, speakers),
            EndNode => [PlaybookSegmentView.Keyword(EndOfScript)],
            BranchNode branch => Branch(branch),
            ControlNode control => Control(control),
            ChoiceNode choice => Choice(choice),
            RandomChoiceNode random => RandomChoice(random),
            _ => [],
        };

    private static ImmutableArray<PlaybookSegmentView> Line(
        LineNode line, ImmutableArray<PlaybookSpeaker> speakers)
    {
        var speech = Speech(line.Speech);
        ImmutableArray<PlaybookSegmentView> said = speech.Length > 0 ? speech : [PlaybookSegmentView.Absent(NoSpeech)];

        return [PlaybookSegmentView.Speaker(NameOf(line.Speaker, speakers)), PlaybookSegmentView.Separator(SpeakerDivider), .. said];
    }

    // A line addresses its speaker by index, so an index the playbook does not carry means the
    // reference broke rather than that nobody spoke.
    private static string NameOf(int speaker, ImmutableArray<PlaybookSpeaker> speakers) =>
        speaker >= 0 && speaker < speakers.Length
            ? speakers[speaker].Name ?? AnonymousSpeaker
            : UnknownSpeaker;

    private static ImmutableArray<PlaybookSegmentView> Branch(BranchNode branch)
    {
        var arms = ImmutableArray.CreateBuilder<PlaybookSegmentView>();
        var ordered = branch.Out.OfType<BranchEdge>().OrderBy(arm => arm.Order);

        foreach (var (arm, index) in ordered.Select((arm, index) => (arm, index)))
        {
            arms.AddRange(Arm(arm, index == 0));
        }

        return arms.ToImmutable();
    }

    // The first arm is read as a plain IF and every arm after it is introduced by ELSE. An
    // unguarded arm has no IF of its own, so that introduction is all it carries.
    private static ImmutableArray<PlaybookSegmentView> Arm(BranchEdge arm, bool isFirst) =>
        (isFirst, arm.Condition) switch
        {
            (true, KeyCondition key) =>
            [
                PlaybookSegmentView.Keyword(NodeCondition),
                Condition(key.Key),
                PlaybookSegmentView.Keyword(NodeConsequence),
                Reaches(arm.Target),
            ],
            (false, KeyCondition key) =>
            [
                PlaybookSegmentView.Keyword(BranchElseIf),
                Condition(key.Key),
                PlaybookSegmentView.Keyword(NodeConsequence),
                Reaches(arm.Target),
            ],
            _ => [PlaybookSegmentView.Keyword(BranchElse), Reaches(arm.Target)],
        };

    // A host only performs commands, so the other speech kinds render as nothing and drop out of
    // the joined line.
    private static ImmutableArray<PlaybookSegmentView> Control(ControlNode control)
    {
        var commands = Commands(control.Effects);
        return commands.Length > 0 ? commands : WhereItGoes(control);
    }

    private static ImmutableArray<PlaybookSegmentView> Commands(ImmutableArray<SpeechFragment> effects) =>
        Items(
            [
                .. effects
                    .Select(CommandText)
                    .Where(text => text.Length > 0)
                    .Select(text => ImmutableArray.Create(PlaybookSegmentView.Command(text))),
            ],
            CommandSeparator);

    private static string CommandText(SpeechFragment fragment) =>
        fragment switch
        {
            DefaultCommandFragment command => $"({command.Action})",
            CustomCommandFragment command => $"{command.Name}({string.Join(", ", command.Args)})",
            _ => string.Empty,
        };

    // An arm's words are what the reader picks, or what the dice fall on, so every piece of them
    // names the node the arm leads to: where a label goes is part of what the label says. The
    // arm's guard is left alone — it is a question about the arm, not a way to its target.
    private static ImmutableArray<PlaybookSegmentView> Reaching(
        ImmutableArray<PlaybookSegmentView> pieces, int target) =>
        [.. pieces.Select(piece => piece with { Target = target })];

    // A control node with nothing to perform moves straight on, and the only words left are the
    // ones a divert was named with. The words and the node they lead to are read from the same
    // edge, so what the reader follows is what they read.
    private static ImmutableArray<PlaybookSegmentView> WhereItGoes(ControlNode control) =>
        NamedDivert(control.Out) is { } divert
            ? [
                PlaybookSegmentView.Separator(DivertArrow),
                PlaybookSegmentView.LinkedTo(SpeechText.Of(divert.Label).Trim(), divert.Target),
            ]
            : [PlaybookSegmentView.Keyword(CarriesOn)];

    private static DivertEdge? NamedDivert(ImmutableArray<Edge> edges) =>
        edges
            .OfType<DivertEdge>()
            .FirstOrDefault(divert => SpeechText.Of(divert.Label).Trim().Length > 0);

    private static ImmutableArray<PlaybookSegmentView> Choice(ChoiceNode choice) =>
        Items([.. choice.Out.OfType<OptionEdge>().Select(Option)], OptionSeparator);

    // A list reads as its items, one to a boundary. The boundary opening the list carries nothing:
    // what comes before it introduces the list — a condition, a draw's header — rather than being
    // an item of it. A lone item is not a list, so no boundary is written and it reads as the line
    // it is.
    private static ImmutableArray<PlaybookSegmentView> Items(
        IReadOnlyList<ImmutableArray<PlaybookSegmentView>> items, string between)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var pieces = ImmutableArray.CreateBuilder<PlaybookSegmentView>();
        if (items.Count > 1)
        {
            pieces.Add(PlaybookSegmentView.Opening());
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (index > 0)
            {
                pieces.Add(PlaybookSegmentView.Boundary(between));
            }

            pieces.AddRange(items[index]);
        }

        return pieces.ToImmutable();
    }

    private static ImmutableArray<PlaybookSegmentView> Option(OptionEdge option)
    {
        var label = Speech(option.Label);
        ImmutableArray<PlaybookSegmentView> words = label.Length > 0 ? label : [PlaybookSegmentView.Absent(NoLabel)];

        return [.. Reaching(words, option.Target), .. Guarded(option.Condition)];
    }

    private static ImmutableArray<PlaybookSegmentView> RandomChoice(RandomChoiceNode random)
    {
        var arms = random.Out.OfType<RandomOptionEdge>().ToImmutableArray();

        return
        [
            PlaybookSegmentView.Keyword(
                $"{DrawPrefix}{arms.Length.ToString(CultureInfo.InvariantCulture)}"),
            PlaybookSegmentView.Separator(OddsDivider),
            .. Items([.. arms.Select(Odds)], OptionSeparator),
        ];
    }

    private static ImmutableArray<PlaybookSegmentView> Odds(RandomOptionEdge arm) =>
        [.. Reaching(Share(arm.Weight), arm.Target), .. Guarded(arm.Condition)];

    private static ImmutableArray<PlaybookSegmentView> Share(ChoiceWeight weight) =>
        weight switch
        {
            NumberWeight number =>
                [PlaybookSegmentView.Plain($"{number.Percentage.ToString(CultureInfo.InvariantCulture)}%")],
            QueryWeight query => [PlaybookSegmentView.Query(SpeechText.PlaceholderFor(query.Key))],
            AutoWeight => [PlaybookSegmentView.Plain(Evenly)],
            _ => [],
        };

    // An option and a random arm both carry their condition after their own text, so the shape is
    // written once.
    private static ImmutableArray<PlaybookSegmentView> Guarded(Condition? condition) =>
        condition is KeyCondition key ? [PlaybookSegmentView.Keyword(Guard), Condition(key.Key)] : [];

    // A condition lives on the kinds that can carry one, which the playbook names through
    // IConditional, so a kind that gains one later is summarized without a change here. Only a key
    // condition has anything to show; another kind reads as unguarded until it does.
    private static KeyCondition? ConditionOf(Node node) =>
        (node as IConditional)?.Condition as KeyCondition;

    // A writer's own words, read from the fragments: a real query is a query, and everything else
    // that came from the script is plain text. Reading the fragments rather than the flattened
    // line is what keeps the braces a writer typed from being mistaken for a query.
    private static ImmutableArray<PlaybookSegmentView> Speech(ImmutableArray<SpeechFragment> speech) =>
        Trim(Merge(Walk(speech)));

    private static ImmutableArray<PlaybookSegmentView> Walk(ImmutableArray<SpeechFragment> speech) =>
        [.. speech.SelectMany(fragment => Fragment(fragment))];

    private static ImmutableArray<PlaybookSegmentView> Fragment(SpeechFragment fragment) =>
        fragment switch
        {
            TextFragment text => PlainParts(text.Text),
            StyledTextFragment styled => Walk(styled.Children),
            LinkFragment link => Walk(link.Label),
            ImageFragment image => Walk(image.Alt),
            LineBreakFragment => [PlaybookSegmentView.Plain(" ")],
            QueryFragment query => [PlaybookSegmentView.Query(SpeechText.PlaceholderFor(query.Key))],
            _ => [],
        };

    private static ImmutableArray<PlaybookSegmentView> Merge(ImmutableArray<PlaybookSegmentView> pieces)
    {
        var merged = ImmutableArray.CreateBuilder<PlaybookSegmentView>();

        foreach (var piece in pieces)
        {
            // Pieces that lead to different nodes stay apart however alike their roles, so a
            // merged piece can never answer for a node it does not name.
            if (merged.Count > 0 && merged[^1].Role == piece.Role && merged[^1].Target == piece.Target)
            {
                merged[^1] = merged[^1] with { Text = merged[^1].Text + piece.Text };
            }
            else
            {
                merged.Add(piece);
            }
        }

        return merged.ToImmutable();
    }

    private static ImmutableArray<PlaybookSegmentView> Trim(ImmutableArray<PlaybookSegmentView> pieces)
    {
        if (pieces.Length == 0)
        {
            return pieces;
        }

        var trimmed = pieces
            .SetItem(0, pieces[0] with { Text = pieces[0].Text.TrimStart() });
        trimmed = trimmed.SetItem(
            trimmed.Length - 1, trimmed[^1] with { Text = trimmed[^1].Text.TrimEnd() });

        return [.. trimmed.Where(piece => piece.Text.Length > 0)];
    }

    // The cut respects a word boundary so no word is left half-written. A piece the boundary lands
    // inside keeps its role for the part that survived, so a cut row stays colored.
    private static ImmutableArray<PlaybookSegmentView> Capped(ImmutableArray<PlaybookSegmentView> pieces)
    {
        var line = string.Concat(pieces.Select(piece => piece.Text));
        if (line.Length <= SummaryCap)
        {
            return pieces;
        }

        var boundary = line.LastIndexOf(' ', SummaryCap);
        var kept = (boundary >= 0 ? line[..boundary] : line[..SummaryCap]).TrimEnd();

        return [.. Take(pieces, kept.Length), PlaybookSegmentView.Separator(Ellipsis)];
    }

    private static ImmutableArray<PlaybookSegmentView> Take(
        ImmutableArray<PlaybookSegmentView> pieces, int length)
    {
        var taken = ImmutableArray.CreateBuilder<PlaybookSegmentView>();
        var remaining = length;

        foreach (var piece in pieces)
        {
            if (remaining <= 0)
            {
                break;
            }

            var text = piece.Text.Length <= remaining ? piece.Text : piece.Text[..remaining];
            taken.Add(piece with { Text = text });
            remaining -= text.Length;
        }

        return taken.ToImmutable();
    }

    // A condition is a query like any other — the boolean member of the family — so it wears the
    // query's role and the `?` the script marks it with. The reader then sees a question only the
    // running game can answer, rather than a name the report happens to know.
    private static PlaybookSegmentView Condition(string key) => PlaybookSegmentView.Query($"{key}{BooleanQuery}");

    private static string Target(int target) => target.ToString(CultureInfo.InvariantCulture);

    // A node's number stands for the node itself, so the piece that shows it also carries it.
    private static PlaybookSegmentView Reaches(int target) =>
        PlaybookSegmentView.LinkedTo(Target(target), target);

    private static ImmutableArray<PlaybookSegmentView> PlainParts(string text) =>
        text.Length == 0 ? [] : [PlaybookSegmentView.Plain(text)];







}
