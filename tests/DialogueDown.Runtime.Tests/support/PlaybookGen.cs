using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks a reader accepts, generated rather than written out by hand.
/// </summary>
/// <remarks>
/// Only what the runner plays is drawn: a line, an end, a jump, a line or a control block that
/// asks the host to carry something out, a block condition, and a menu. A line may hold a command
/// before, between, or after its words, or say nothing but a command. A later pass adds its node
/// and edge kinds here as it teaches the runner to play them, so a walk keeps covering everything a
/// run can meet.
/// <para>
/// Any of them may ask the world something. A line or a control block may be guarded, a jump may
/// fire only when the world allows it, a block condition's first arm is always guarded, and a line
/// may have a query in what it says. A guard and a query never read the same key, so a world drawn
/// beside the playbook can answer each key in the kind it needs. A menu is the exception: it is
/// offered without reading the world, so nothing guards its options and its labels ask nothing.
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

        /// <summary>A control block asking the host to carry something out.</summary>
        Effects,

        /// <summary>A line carrying a jump, with a succession beside it to fall through to.</summary>
        LineThatJumps,

        /// <summary>A block condition with one arm, skipped when the world withholds it.</summary>
        Block,

        /// <summary>A block condition with one arm and an else.</summary>
        BlockWithAnElse,

        /// <summary>A menu of two options, one carrying on and one leading elsewhere.</summary>
        Menu,
    }

    /// <summary>Where a drawn line holds a command in what it says.</summary>
    private enum CommandAt
    {
        /// <summary>Nowhere: the line only speaks.</summary>
        Nowhere,

        /// <summary>Before its words.</summary>
        First,

        /// <summary>Between two runs of its words.</summary>
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
                Draws.Menu),
            Gen.Int[0, speakers - 1],
            Gen.Int[0, nodes - 1],
            Gen.Int[0, nodes - 1],
            Guard(),
            Guard(),
            Gen.OneOfConst([.. _truthKeys.Select(Condition (key) => new KeyCondition(key))]),
            Gen.Select(Gen.Bool, Gen.Enum<CommandAt>()),
            (draws, speaker, onward, elsewhere, guard, jumpGuard, armGuard, speech) =>
                new NodeDraft(
                    draws, speaker, onward, elsewhere, guard, jumpGuard, armGuard, speech.Item1, speech.Item2));

    // Nothing guards it as often as each key does, so a walk still meets plenty of nodes it can
    // pass without asking.
    private static Gen<Condition?> Guard() =>
        Gen.OneOfConst<Condition?>([null, .. _truthKeys.Select(key => new KeyCondition(key))]);

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
    /// <param name="ArmGuard">What the world must allow for a block condition's arm to be taken.</param>
    /// <param name="Asks">Whether a line has a query in what it says.</param>
    /// <param name="CommandAt">Where a line holds a command in what it says.</param>
    private readonly record struct NodeDraft(
        Draws Draws,
        int Speaker,
        int Onward,
        int Elsewhere,
        Condition? Guard,
        Condition? JumpGuard,
        Condition ArmGuard,
        bool Asks,
        CommandAt CommandAt)
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
                [new DefaultCommandFragment("do something")],
                Guard,
                [new SuccessionEdge(Onward)]),
            Draws.LineThatJumps => Speaks(id, [Jump(), new SuccessionEdge(Onward)]),
            Draws.Block => new BranchNode(id, [Arm(), new SuccessionEdge(Onward)]),
            Draws.BlockWithAnElse => new BranchNode(id, [Arm(), new BranchEdge(Onward, Condition: null)]),
            Draws.Menu => new ChoiceNode(id, Ordered: false, Options()),
            _ => Speaks(id, [new SuccessionEdge(Onward)]),
        };

        // A jump the world may withhold falls through to the line beneath it, which is how a
        // script compiles one. A jump that always fires has nowhere else to go.
        private ImmutableArray<Edge> JumpOnItsOwnLine() =>
            JumpGuard is null ? [Jump()] : [Jump(), new SuccessionEdge(Onward)];

        private LineNode Speaks(int id, ImmutableArray<Edge> out_) =>
            new(id, Speaker, Speech(), Guard, out_);

        // A query in the words after a command is a query written after a command, so drawing
        // both covers a line whose words the world must answer part-way through.
        private ImmutableArray<SpeechFragment> Speech() =>
            CommandAt switch
            {
                CommandAt.First => [Wave(), .. Words()],
                CommandAt.Between => [new TextFragment("Well "), Wave(), .. Words()],
                CommandAt.Last => [.. Words(), Wave()],
                CommandAt.Alone => [Wave()],
                _ => Words(),
            };

        private ImmutableArray<SpeechFragment> Words() =>
            Asks
                ? [new TextFragment("Something, "), new QueryFragment(WordsKey), new TextFragment(".")]
                : [new TextFragment("Something.")];

        private static CustomCommandFragment Wave() => new("Wave", []);

        private DivertEdge Jump() => new(Elsewhere, [], JumpGuard);

        private BranchEdge Arm() => new(Elsewhere, ArmGuard);

        private ImmutableArray<Edge> Options() =>
            [Option(Onward, "Go on."), Option(Elsewhere, "Go elsewhere.")];

        private static OptionEdge Option(int target, string label) =>
            new(target, [new TextFragment(label)], Condition: null);
    }
}
