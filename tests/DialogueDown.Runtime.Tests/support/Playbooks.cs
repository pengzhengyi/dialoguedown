using System.Collections.Immutable;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks built by hand, so a test about stepping says what it is about rather than compiling
/// a script to get there.
/// </summary>
/// <remarks>
/// A playbook is an immutable record with no behavior, so a test builds a real one rather than
/// standing in a substitute. Stepping dispatches on the kind of node it finds, which means a
/// substitute would have to return real nodes anyway.
/// </remarks>
internal static class Playbooks
{
    /// <summary>A context over exactly the nodes and speakers a test names.</summary>
    /// <param name="nodes">The steps of the playthrough.</param>
    /// <param name="speakers">Everybody who speaks, by the name they speak under; a null name is the anonymous default speaker.</param>
    /// <param name="entry">Where a playthrough begins.</param>
    /// <returns>A context ready to step.</returns>
    public static PlayContext Context(
        ImmutableArray<Node> nodes, IEnumerable<string?>? speakers = null, int entry = 0) =>
        PlayContext.Of(Document(nodes, [.. (speakers ?? []).Select(Speaker)], entry));

    /// <summary>The document behind such a context, for a test that needs the playbook itself.</summary>
    /// <param name="nodes">The steps of the playthrough.</param>
    /// <param name="speakers">Everybody who speaks.</param>
    /// <param name="entry">Where a playthrough begins.</param>
    /// <returns>The playbook.</returns>
    public static PlaybookDocument Document(
        ImmutableArray<Node> nodes, ImmutableArray<PlaybookSpeaker> speakers, int entry = 0) =>
        new(
            new PlaybookFormat(PlaybookSupport.NewestReadableVersion, ["core"], []),
            script: "a-script.dialogue.md",
            entry: entry,
            anchors: ImmutableSortedDictionary<string, int>.Empty,
            speakers: speakers,
            nodes: nodes);

    /// <summary>One line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context a single <c>Next</c> finishes.</returns>
    public static PlayContext OneLine() =>
        Context([Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)], ["Alice"]);

    /// <summary>Two lines, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello.
    ///
    /// Bob: Goodbye.
    /// </code>
    /// </remarks>
    /// <returns>A context that shows succession going somewhere.</returns>
    public static PlayContext TwoLines() =>
        Context(
            [Line(0, speaker: 0, "Hello.", next: 1), Line(1, speaker: 1, "Goodbye.", next: 2), new EndNode(2)],
            ["Alice", "Bob"]);

    /// <summary>A choice, which is a node kind this pass cannot play.</summary>
    /// <remarks>
    /// <code>
    /// - Go east
    /// </code>
    /// </remarks>
    /// <returns>A context that begins at a choice.</returns>
    public static PlayContext NotYetPlayable() =>
        Context([new ChoiceNode(0, Ordered: true, [new OptionEdge(1, [new TextFragment("Go east")], null)]), new EndNode(1)], ["Alice"]);

    /// <summary>A line node, said by a speaker and leading onward.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="speaker">Who says it, by index.</param>
    /// <param name="text">What is said.</param>
    /// <param name="next">Where succession leads.</param>
    /// <returns>The node.</returns>
    public static LineNode Line(int id, int speaker, string text, int next) =>
        new(id, speaker, [new TextFragment(text)], Condition: null, [new SuccessionEdge(next)]);

    /// <summary>A line node that only plays when the world allows it.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="speaker">Who says it, by index.</param>
    /// <param name="text">What is said.</param>
    /// <param name="next">Where succession leads.</param>
    /// <param name="key">What the world is asked before the line plays.</param>
    /// <returns>The node.</returns>
    public static LineNode ConditionalLine(int id, int speaker, string text, int next, string key) =>
        new(id, speaker, [new TextFragment(text)], new KeyCondition(key), [new SuccessionEdge(next)]);

    /// <summary>A line whose speech is written out fragment by fragment.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="speaker">Who says it, by index.</param>
    /// <param name="next">Where succession leads.</param>
    /// <param name="condition">What must hold for the line to play, or <see langword="null"/>.</param>
    /// <param name="speech">What is said, in the order written.</param>
    /// <returns>The node.</returns>
    /// <remarks>
    /// For a line a query stands in, which plain text cannot spell.
    /// </remarks>
    public static LineNode LineSaying(
        int id, int speaker, int next, Condition? condition, params SpeechFragment[] speech) =>
        new(id, speaker, [.. speech], condition, [new SuccessionEdge(next)]);

    /// <summary>A line carrying a jump the world must allow, and a succession to fall through to.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="speaker">Who says it, by index.</param>
    /// <param name="text">What is said.</param>
    /// <param name="jumpTo">Where the jump leads when the world allows it.</param>
    /// <param name="next">Where succession leads when it does not.</param>
    /// <param name="key">What the world is asked before the jump fires.</param>
    /// <returns>The node.</returns>
    public static LineNode LineWithConditionalJump(
        int id, int speaker, string text, int jumpTo, int next, string key) =>
        new(
            id,
            speaker,
            [new TextFragment(text)],
            Condition: null,
            [Divert(jumpTo, key), new SuccessionEdge(next)]);

    /// <summary>A jump on its own line: nothing said, nothing performed, one way out.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="jumpTo">Where the jump leads.</param>
    /// <returns>The node.</returns>
    public static ControlNode Jump(int id, int jumpTo) => Bare(id, Divert(jumpTo));

    /// <summary>A node that says nothing and performs nothing, leaving only by the ways given.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="ways">The ways out, in the order written.</param>
    /// <returns>The node.</returns>
    /// <remarks>
    /// For a test about which way out a run takes, where what the node itself does is beside the
    /// point.
    /// </remarks>
    public static ControlNode Bare(int id, params Edge[] ways) =>
        new(id, [], Condition: null, [.. ways]);

    /// <summary>A jump, taken whenever a run leaves the node carrying it.</summary>
    /// <param name="target">Where the jump leads.</param>
    /// <returns>The edge.</returns>
    public static DivertEdge Divert(int target) => new(target, [], Condition: null);

    /// <summary>A jump taken only while the world answers that the key holds.</summary>
    /// <param name="target">Where the jump leads.</param>
    /// <param name="key">What the world is asked before the jump fires.</param>
    /// <returns>The edge.</returns>
    public static DivertEdge Divert(int target, string key) =>
        new(target, [], new KeyCondition(key));

    /// <summary>A block condition, which says nothing and performs nothing and goes on by an arm.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="ways">Its arms in the order they are tried, and a succession when it has no else.</param>
    /// <returns>The node.</returns>
    public static BranchNode Branch(int id, params Edge[] ways) => new(id, [.. ways]);

    /// <summary>An arm of a block condition, taken when the world answers that the key holds.</summary>
    /// <param name="target">Where the arm leads.</param>
    /// <param name="order">Where it sits in the order the arms are tried, counting from zero.</param>
    /// <param name="key">What the world is asked before the arm is taken.</param>
    /// <returns>The edge.</returns>
    public static BranchEdge Arm(int target, int order, string key) =>
        new(target, order, new KeyCondition(key));

    /// <summary>The else of a block condition, taken when no arm before it is.</summary>
    /// <param name="target">Where it leads.</param>
    /// <param name="order">Where it sits in the order the arms are tried, which is last.</param>
    /// <returns>The edge.</returns>
    public static BranchEdge Else(int target, int order) => new(target, order, Condition: null);

    /// <summary>A ring of jumps, each leading to the next and the last back to the first.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -> node 1 -> node 2 -> ... -> node length-1 -> node 0
    /// </code>
    /// Nothing in it ever hands the host anything, so a walk with no bound never comes out.
    /// </remarks>
    /// <param name="length">How many jumps the ring holds.</param>
    /// <returns>A context whose entry walks forever unless something stops it.</returns>
    public static PlayContext RingOfJumps(int length) =>
        Context([.. Enumerable.Range(0, length).Select(at => Jump(at, (at + 1) % length))]);

    /// <summary>A chain of jumps ending at the end.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -> node 1 -> node 2 -> ... -> node jumps-1 -> node jumps, the end
    /// </code>
    /// The walk passes every node the playbook has, exactly once. One node further along and it
    /// would be passing one of them twice, which is the case a bound must not confuse this with.
    /// </remarks>
    /// <param name="jumps">How many jumps precede the end.</param>
    /// <returns>A context whose walk passes every node exactly once.</returns>
    public static PlayContext ChainOfJumps(int jumps) =>
        Context([.. Enumerable.Range(0, jumps).Select(at => (Node)Jump(at, at + 1)), new EndNode(jumps)]);

    /// <summary>An effect, then a line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")`
    ///
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose run waits on the host before it says anything.</returns>
    public static PlayContext AnEffectThenALine() =>
        Context(
            [Effects(0, next: 1, "fade in"), Line(1, speaker: 0, "Hello.", next: 2), new EndNode(2)],
            ["Alice"]);

    /// <summary>A line whose jump the world must allow, with a line to fall through to.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Away. `Alice.HasKey?` =&gt; [Inside](#inside)
    ///
    /// Alice: Here.
    ///
    /// # Inside
    ///
    /// Alice: Inside.
    /// </code>
    /// </remarks>
    /// <returns>A context where the world decides which line is said after the first.</returns>
    public static PlayContext ALineWhoseJumpAsksTheWorld() =>
        Context(
            [
                LineWithConditionalJump(0, speaker: 0, "Away.", jumpTo: 2, next: 1, key: "Alice.HasKey"),
                Line(1, speaker: 0, "Here.", next: 2),
                Line(2, speaker: 0, "Inside.", next: 3),
                new EndNode(3),
            ],
            ["Alice"]);

    /// <summary>A jump on its own line that the world must allow, with a line to fall through to.</summary>
    /// <remarks>
    /// <code>
    /// `Rainy?` =&gt; [Inn](#inn)
    ///
    /// Alice: Onward in the sun.
    ///
    /// # Inn
    ///
    /// Alice: Inside, out of the rain.
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing and cannot be left without asking.</returns>
    public static PlayContext AGuardedJumpOnItsOwnLine() =>
        Context(
            [
                Bare(0, Divert(2, "Rainy"), new SuccessionEdge(1)),
                Line(1, speaker: 0, "Onward in the sun.", next: 2),
                Line(2, speaker: 0, "Inside, out of the rain.", next: 3),
                new EndNode(3),
            ],
            ["Alice"]);

    /// <summary>A block condition with an if, an elseif, and an else.</summary>
    /// <remarks>
    /// <code>
    /// &gt; `if` `Alice.HasKey?`
    /// &gt;
    /// &gt; Alice: The key turns.
    /// &gt;
    /// &gt; `elseif` `Alice.HasPick?`
    /// &gt;
    /// &gt; Alice: The pick clicks.
    /// &gt;
    /// &gt; `else`
    /// &gt;
    /// &gt; Alice: The door stays shut.
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing, and whose every arm says a different line.</returns>
    public static PlayContext AConditionalBlock() =>
        Context(
            [
                Branch(0, Arm(1, order: 0, "Alice.HasKey"), Arm(2, order: 1, "Alice.HasPick"), Else(3, order: 2)),
                Line(1, speaker: 0, "The key turns.", next: 4),
                Line(2, speaker: 0, "The pick clicks.", next: 4),
                Line(3, speaker: 0, "The door stays shut.", next: 4),
                new EndNode(4),
            ],
            ["Alice"]);

    /// <summary>A control node carrying effects for the host to carry out.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="next">Where succession leads once the host is done.</param>
    /// <param name="actions">What the host is asked to carry out, in the order written.</param>
    /// <returns>The node.</returns>
    public static ControlNode Effects(int id, int next, params string[] actions) =>
        new(
            id,
            [.. actions.Select(SpeechFragment (action) => new DefaultCommandFragment(action))],
            Condition: null,
            [new SuccessionEdge(next)]);

    /// <summary>One node of every kind the playbook format defines, each the simplest of its kind.</summary>
    /// <remarks>
    /// Written out rather than reflected over, because a kind has to be built before it can be
    /// asked anything, and only a person knows what the simplest one of each looks like. A test
    /// guards the list against the format, so a kind added to the format and not to this reads as
    /// a failure rather than as a quiet gap.
    /// </remarks>
    /// <returns>The nodes, each standing at position 0 and leading to position 1.</returns>
    public static IEnumerable<Node> OneOfEveryNodeKind() =>
    [
        new LineNode(0, 0, [new TextFragment("Hello.")], Condition: null, [new SuccessionEdge(1)]),
        new EndNode(0),
        new ControlNode(0, [], Condition: null, [new SuccessionEdge(1)]),
        new ControlNode(0, [new DefaultCommandFragment("fade in")], Condition: null, [new SuccessionEdge(1)]),
        new ChoiceNode(0, Ordered: false, [new OptionEdge(1, [new TextFragment("Go east")], Condition: null)]),
        new BranchNode(0, [new BranchEdge(1, Order: 0, Condition: null)]),
        new RandomChoiceNode(0, [new RandomOptionEdge(1, new AutoWeight(), Condition: null)]),
    ];

    /// <summary>A line node nothing leads on from.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="text">What is said.</param>
    /// <returns>The node.</returns>
    public static LineNode Dead(int id, string text) =>
        new(id, Speaker: 0, [new TextFragment(text)], Condition: null, []);

    private static PlaybookSpeaker Speaker(string? name) =>
        new(Id: null, Name: name, Default: name is null, Tags: []);
}
