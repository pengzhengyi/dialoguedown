using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks a reader accepts, generated rather than written out by hand.
/// </summary>
/// <remarks>
/// Only what the runner plays is drawn: a line, an end, a jump, a line or a control block that
/// asks the host to perform something, a block condition, a menu, and a random choice whose shares
/// need no answer from the world. A line may hold a command before, between, or after its words, or
/// say nothing but a command. A node or edge kind the runner gains belongs here too, so a walk
/// covers everything a run can meet.
/// <para>
/// Any of them but a random choice may ask the world something. A line or a control block may be
/// guarded, a jump may fire only when the world allows it, a block condition's first branch is
/// always guarded, a menu's options may be guarded, and a line or an option's label may have a
/// query in what it says. A guard and a query never read the same key, so a world drawn beside the
/// playbook can answer each key in the kind it needs. A menu whose every option is guarded has a
/// fall-through beside them, as a script compiles to, so a walk sometimes passes a menu by. A
/// random choice whose random options all weigh zero has one too, so a walk sometimes passes a
/// random choice by.
/// </para>
/// <para>
/// A drawn playbook may loop, may begin anywhere, and may leave an end unreachable. All three are
/// shapes a script can compile to, and each is a walk worth taking.
/// </para>
/// </remarks>
internal static class PlaybookGen
{
    private const int MostNodes = 10;
    private const int MostSpeakers = 3;

    // The key a drawn query reads, answered with words.
    private const string WordsKey = "Name";

    // The keys a drawn guard reads, each answered with a truth. Two, so two nodes, or a line and
    // its jump, sometimes ask about the same one.
    private static readonly ImmutableArray<string> _truthKeys = ["Rainy", "Late"];

    /// <summary>What a drawn node turns out to be.</summary>
    private enum Draws
    {
        /// <summary>A line that carries on.</summary>
        Line,

        /// <summary>The end of a run.</summary>
        End,

        /// <summary>A jump on its own line: nothing said and nothing performed.</summary>
        Jump,

        /// <summary>A control block asking the host to perform something.</summary>
        Effects,

        /// <summary>A line carrying a jump, with a succession beside it to fall through to.</summary>
        LineThatJumps,

        /// <summary>A block condition with one branch, skipped when the world withholds it.</summary>
        Block,

        /// <summary>A block condition with one gated branch and an else.</summary>
        BlockWithAnElse,

        /// <summary>
        /// A menu of two options, one carrying on and one leading elsewhere, either of which the
        /// world may withhold.
        /// </summary>
        Menu,

        /// <summary>
        /// A random choice of two random options, one carrying on and one leading elsewhere, each
        /// weighed by a number or an auto weight.
        /// </summary>
        RandomChoice,
    }

    /// <summary>Where a drawn line holds a command in what it says.</summary>
    private enum CommandAt
    {
        /// <summary>Nowhere: the line only speaks.</summary>
        Nowhere,

        /// <summary>Before its words.</summary>
        First,

        /// <summary>Between two parts of its words.</summary>
        Between,

        /// <summary>After its words.</summary>
        Last,

        /// <summary>In place of any words.</summary>
        Alone,
    }

    /// <summary>Playbooks the default reader accepts.</summary>
    /// <returns>The generator.</returns>
    public static Gen<PlaybookDocument> Valid() =>
        Gen.SelectMany(
            Gen.Int[1, MostNodes],
            Gen.Int[1, MostSpeakers],
            (nodes, speakers) =>
                Gen.Select(
                    Draft(nodes, speakers).Array[nodes],
                    Gen.Int[0, nodes - 1],
                    (drafts, entry) => Document(drafts, speakers, entry)));

    /// <summary>Worlds that answer whatever a drawn playbook asks.</summary>
    /// <returns>The generator.</returns>
    public static Gen<DrawnWorld> Worlds() =>
        Gen.Bool.Array[_truthKeys.Length].Select(holds =>
            new DrawnWorld(
                _truthKeys.Zip(holds).ToImmutableDictionary(
                    truth => truth.First, truth => truth.Second, StringComparer.Ordinal)));

    // Drawn without an index, then given one: a node states its own position, so it can only be
    // built once its place among the others is known.
    private static Gen<NodeDraft> Draft(int nodes, int speakers) =>
        Gen.Select(
            Gen.OneOfConst(
                Draws.Line,
                Draws.End,
                Draws.Jump,
                Draws.Effects,
                Draws.LineThatJumps,
                Draws.Block,
                Draws.BlockWithAnElse,
                Draws.Menu,
                Draws.RandomChoice),
            Gen.Int[0, speakers - 1],
            Gen.Int[0, nodes - 1],
            Gen.Int[0, nodes - 1],
            Gen.Select(Guard(), Guard(), BranchGuard()),
            Gen.Select(Gen.Bool, Gen.Enum<CommandAt>()),
            Gen.Select(Menu(), RandomChoice()),
            (draws, speaker, onward, elsewhere, guards, speech, choices) =>
                new NodeDraft(
                    draws,
                    speaker,
                    onward,
                    elsewhere,
                    Guard: guards.Item1,
                    JumpGuard: guards.Item2,
                    BranchGuard: guards.Item3,
                    Asks: speech.Item1,
                    CommandAt: speech.Item2,
                    Menu: choices.Item1,
                    RandomChoice: choices.Item2));

    // No guard is drawn as often as each key, so a walk still meets plenty of nodes it can pass
    // without asking.
    private static Gen<Condition?> Guard() =>
        Gen.OneOfConst<Condition?>([null, .. _truthKeys.Select(key => new KeyCondition(key))]);

    // A block condition's first branch is always guarded, so the walk meets both the branch and
    // what follows it.
    private static Gen<Condition> BranchGuard() =>
        Gen.OneOfConst([.. _truthKeys.Select(Condition (key) => new KeyCondition(key))]);

    private static Gen<MenuDraft> Menu() =>
        Gen.Select(
            Guard(),
            Guard(),
            Gen.Bool,
            Gen.Bool,
            (onwardGuard, elsewhereGuard, onwardAsks, elsewhereAsks) =>
                new MenuDraft(onwardGuard, elsewhereGuard, onwardAsks, elsewhereAsks));

    private static Gen<RandomChoiceDraft> RandomChoice() =>
        Gen.Select(Weight(), Weight(), (onward, elsewhere) => new RandomChoiceDraft(onward, elsewhere));

    // A number beside an auto weight leaves the auto weight the rest of 100, and two zeros leave
    // nothing drawable.
    private static Gen<ChoiceWeight> Weight() =>
        Gen.OneOfConst<ChoiceWeight>(new AutoWeight(), new NumberWeight(0), new NumberWeight(60));

    private static PlaybookDocument Document(NodeDraft[] drafts, int speakers, int entry) =>
        PlayContextFactory.Document(
            [.. drafts.Select((draft, id) => draft.At(id))], Speakers(speakers), entry);

    // The first speaker is the anonymous default, so a drawn line can be said by nobody in
    // particular as well as by somebody named.
    private static ImmutableArray<PlaybookSpeaker> Speakers(int speakers) =>
        [.. Enumerable.Range(0, speakers).Select(at =>
            at == 0
                ? new PlaybookSpeaker(Id: null, Name: null, Default: true, Tags: [])
                : new PlaybookSpeaker(Id: null, Name: $"Speaker {at}", Default: false, Tags: []))];

    /// <summary>A node before it knows where it stands.</summary>
    /// <param name="Draws">What it turns out to be.</param>
    /// <param name="Speaker">Who says it, by index.</param>
    /// <param name="Onward">Where succession leads.</param>
    /// <param name="Elsewhere">Where a jump leads, when the node carries one.</param>
    /// <param name="Guard">What the world must allow for a line or control block to play.</param>
    /// <param name="JumpGuard">What the world must allow for a jump to fire.</param>
    /// <param name="BranchGuard">What the world must allow for a block condition's branch to be taken.</param>
    /// <param name="Asks">Whether a line has a query in what it says.</param>
    /// <param name="CommandAt">Where a line holds a command in what it says.</param>
    /// <param name="Menu">What a menu's options ask of the world.</param>
    /// <param name="RandomChoice">How a random choice's random options are weighed.</param>
    private readonly record struct NodeDraft(
        Draws Draws,
        int Speaker,
        int Onward,
        int Elsewhere,
        Condition? Guard,
        Condition? JumpGuard,
        Condition BranchGuard,
        bool Asks,
        CommandAt CommandAt,
        MenuDraft Menu,
        RandomChoiceDraft RandomChoice)
    {
        /// <summary>The node, standing at a position.</summary>
        /// <param name="id">Its position in the playbook.</param>
        /// <returns>The node.</returns>
        public Node At(int id) => Draws switch
        {
            Draws.End => new EndNode(id),
            Draws.Jump => new ControlNode(id, [], Condition: null, JumpOnItsOwnLine()),
            Draws.Effects => new ControlNode(
                id,
                [DefaultCommand("do something")],
                Guard,
                [new SuccessionEdge(Onward)]),
            Draws.LineThatJumps => Speaks(id, [Jump(), new SuccessionEdge(Onward)]),
            Draws.Block => new BranchNode(id, [GatedBranch(), new SuccessionEdge(Onward)]),
            Draws.BlockWithAnElse => new BranchNode(id, [GatedBranch(), new BranchEdge(Onward, Condition: null)]),
            Draws.Menu => new ChoiceNode(id, Ordered: false, Options()),
            Draws.RandomChoice => new RandomChoiceNode(id, RandomOptions()),
            _ => Speaks(id, [new SuccessionEdge(Onward)]),
        };

        // A jump the world may withhold falls through to the line beneath it, which is how a
        // script compiles one. A jump that always fires has nowhere else to go.
        private ImmutableArray<Edge> JumpOnItsOwnLine() =>
            JumpGuard is null ? [Jump()] : [Jump(), new SuccessionEdge(Onward)];

        private LineNode Speaks(int id, ImmutableArray<Edge> out_) =>
            new(id, Speaker, Speech(), Guard, out_);

        // A line that asks and holds its command first or between puts the query after the
        // command, so the world is asked part-way through the line.
        private ImmutableArray<SpeechFragment> Speech() =>
            CommandAt switch
            {
                CommandAt.First => [Wave(), .. Words()],
                CommandAt.Between => [Text("Well "), Wave(), .. Words()],
                CommandAt.Last => [.. Words(), Wave()],
                CommandAt.Alone => [Wave()],
                _ => Words(),
            };

        private ImmutableArray<SpeechFragment> Words() =>
            Asks
                ? [Text("Something, "), Query(WordsKey), Text(".")]
                : [Text("Something.")];

        private static CustomCommandFragment Wave() => CustomCommand("Wave");

        private DivertEdge Jump() => new(Elsewhere, [], JumpGuard);

        private BranchEdge GatedBranch() => new(Elsewhere, BranchGuard);

        // A menu whose every option the world may withhold falls through past them, which is how a
        // script compiles one, so the walk goes on when the world withholds both.
        private ImmutableArray<Edge> Options() =>
            Menu.EveryOptionIsGuarded ? [.. TwoOptions(), new SuccessionEdge(Onward)] : [.. TwoOptions()];

        private ImmutableArray<OptionEdge> TwoOptions() =>
            [
                Option(Onward, "Go on", Menu.OnwardGuard, Menu.OnwardAsks),
                Option(Elsewhere, "Go elsewhere", Menu.ElsewhereGuard, Menu.ElsewhereAsks),
            ];

        // An option whose label asks names who it is said to, which the world fills in when the
        // menu is offered.
        private static OptionEdge Option(int target, string words, Condition? guard, bool asks) =>
            new(target, asks ? [Text($"{words}, "), Query(WordsKey), Text(".")] : [Text($"{words}.")], guard);

        // A random choice with nothing drawable falls through past its random options, so the walk
        // goes on rather than being refused there.
        private ImmutableArray<Edge> RandomOptions() =>
            RandomChoice.HasNothingDrawable
                ? [.. TwoRandomOptions(), new SuccessionEdge(Onward)]
                : [.. TwoRandomOptions()];

        private ImmutableArray<RandomOptionEdge> TwoRandomOptions() =>
            [
                new(Onward, RandomChoice.OnwardWeight, Condition: null),
                new(Elsewhere, RandomChoice.ElsewhereWeight, Condition: null),
            ];
    }

    /// <summary>What a drawn menu's options ask of the world.</summary>
    /// <param name="OnwardGuard">What the world must allow for the option that carries on to be taken.</param>
    /// <param name="ElsewhereGuard">What the world must allow for the option leading elsewhere to be taken.</param>
    /// <param name="OnwardAsks">Whether the label of the option that carries on has a query in it.</param>
    /// <param name="ElsewhereAsks">Whether the label of the option leading elsewhere has a query in it.</param>
    private readonly record struct MenuDraft(
        Condition? OnwardGuard, Condition? ElsewhereGuard, bool OnwardAsks, bool ElsewhereAsks)
    {
        /// <summary>Gets whether the world may withhold every option.</summary>
        public bool EveryOptionIsGuarded => OnwardGuard is not null && ElsewhereGuard is not null;
    }

    /// <summary>How a drawn random choice's random options are weighed.</summary>
    /// <param name="OnwardWeight">The weight of the random option that carries on.</param>
    /// <param name="ElsewhereWeight">The weight of the random option leading elsewhere.</param>
    private readonly record struct RandomChoiceDraft(ChoiceWeight OnwardWeight, ChoiceWeight ElsewhereWeight)
    {
        /// <summary>Gets whether both random options weigh zero, so no draw could land on either.</summary>
        public bool HasNothingDrawable => IsZero(OnwardWeight) && IsZero(ElsewhereWeight);

        private static bool IsZero(ChoiceWeight weight) => weight is NumberWeight { Percentage: 0 };
    }
}
