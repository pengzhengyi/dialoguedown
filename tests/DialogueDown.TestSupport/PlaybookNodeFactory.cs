using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.TestSupport;

/// <summary>
/// Builds the nodes of a playbook and the ways out of them.
/// </summary>
/// <remarks>
/// <para>
/// What a node says comes first, as the words themselves, because a test over speech asserts the
/// words that come out and reads best beside what went in. Where a node sits and where it leads
/// come after, named and optional: most tests read what a node holds and have no interest in its
/// place, and a test about the graph names the ids and targets it is about.
/// </para>
/// <para>
/// A node that can fall through takes <c>next</c>, the node its succession leads to. The
/// succession is added after any other ways out it is given.
/// </para>
/// <para>
/// A <c>condition</c> is the key the world must allow before the node or edge applies.
/// </para>
/// </remarks>
public static class PlaybookNodeFactory
{
    /// <summary>A line somebody says, leaving by the ways given.</summary>
    public static LineNode Line(
        string words,
        int speaker = 0,
        string? condition = null,
        int id = 0,
        int? next = null,
        params Edge[] ways) =>
        new(id, speaker, Speech(words), If(condition), Ways(ways, next));

    /// <summary>
    /// A line whose speech is the fragments a test composed, so it can hold a query, a tag, or
    /// styling rather than plain words.
    /// </summary>
    public static LineNode LineOf(
        ImmutableArray<SpeechFragment> speech,
        int speaker = 0,
        string? condition = null,
        int id = 0,
        int? next = null,
        params Edge[] ways) =>
        new(id, speaker, speech, If(condition), Ways(ways, next));

    /// <summary>
    /// A line with no speech at all, which the format allows, leaving by the ways given.
    /// </summary>
    /// <remarks>
    /// For a test about a line's ways out, where what it says is beside the point.
    /// </remarks>
    public static LineNode SilentLine(
        int speaker = 0,
        string? condition = null,
        int id = 0,
        int? next = null,
        params Edge[] ways) =>
        new(id, speaker, [], If(condition), Ways(ways, next));

    /// <summary>Where play stops.</summary>
    public static EndNode End(int id = 0) => new(id);

    /// <summary>A block condition with the branches it tries in order.</summary>
    /// <remarks>
    /// <code>
    /// > `if` `"Rich"?`
    /// >
    /// > Alice: Upstairs.
    /// >
    /// > `else`
    /// >
    /// > Alice: Side door.
    /// </code>
    /// </remarks>
    public static BranchNode Branch(params Edge[] branches) => new(0, [.. branches]);

    /// <summary>A gated branch of a block condition, taken when the world allows the condition.</summary>
    public static BranchEdge Gated(string condition, int target = 0) =>
        new(target, new KeyCondition(condition));

    /// <summary>The else of a block condition, taken when no branch before it is.</summary>
    public static BranchEdge Else(int target = 0) => new(target, Condition: null);

    /// <summary>An effect-only line: something the host performs, attributed to nobody.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")` `PlayMusic("battle")`
    /// </code>
    /// </remarks>
    public static ControlNode Control(params SpeechFragment[] effects) =>
        new(0, [.. effects], null, []);

    /// <summary>An effect-only line that fires only when the world allows the condition.</summary>
    public static ControlNode ConditionalControl(string condition, params SpeechFragment[] effects) =>
        new(0, [.. effects], new KeyCondition(condition), []);

    /// <summary>
    /// A control node with no effects, which is what a bare scene-to-scene jump compiles to. Its
    /// one way out is the divert carrying the words the writer gave the jump.
    /// </summary>
    /// <remarks>
    /// <code>
    /// => [The Mountain Road](#the-mountain-road)
    /// </code>
    /// </remarks>
    public static ControlNode Diverting(string label, int target = 0) =>
        new(0, [], null, [new DivertEdge(target, Speech(label), null)]);

    /// <summary>
    /// A control node whose effects are the fragments a test composed, leaving by the ways given.
    /// </summary>
    /// <remarks>
    /// For a control node that needs an id, a <c>next</c>, or ways out; <see cref="Control"/>
    /// covers effects alone.
    /// </remarks>
    public static ControlNode ControlOf(
        ImmutableArray<SpeechFragment> effects,
        string? condition = null,
        int id = 0,
        int? next = null,
        params Edge[] ways) =>
        new(id, effects, If(condition), Ways(ways, next));

    /// <summary>A control node that neither performs anything nor names where it goes.</summary>
    /// <remarks>
    /// <code>
    /// => [](#the-cellar)
    /// </code>
    /// </remarks>
    public static ControlNode DivertingUnlabeled(int target = 0, int id = 0) =>
        new(id, [], null, [Divert(target: target)]);

    /// <summary>A jump with no words of its own, taken whenever the world allows its condition.</summary>
    public static DivertEdge Divert(string? condition = null, int target = 0) =>
        new(target, [], If(condition));

    /// <summary>A menu the player picks from; its options are the ways out.</summary>
    /// <remarks>
    /// <code>
    /// - Alice: Go east.
    ///
    /// - => [Brave the west road](#END)
    /// </code>
    /// </remarks>
    public static ChoiceNode Choice(params Edge[] options) => new(0, false, [.. options]);

    /// <summary>A menu at the id given.</summary>
    public static ChoiceNode Choice(int id, params Edge[] options) => new(id, false, [.. options]);

    /// <summary>A numbered menu, whose options are shown in the order written.</summary>
    /// <remarks>
    /// <code>
    /// 1. Alice: Open the door.
    /// 2. Alice: Wait here.
    /// </code>
    /// </remarks>
    public static ChoiceNode NumberedChoice(params Edge[] options) => new(0, true, [.. options]);

    /// <summary>A numbered menu at the id given.</summary>
    public static ChoiceNode NumberedChoice(int id, params Edge[] options) =>
        new(id, true, [.. options]);

    /// <summary>One option of a menu, carrying the words the menu shows for it.</summary>
    public static OptionEdge Option(string words, string? condition = null, int target = 0) =>
        new(target, Speech(words), If(condition));

    /// <summary>
    /// An option whose label is the fragments a test composed, so it can hold a query or a command.
    /// </summary>
    public static OptionEdge OptionOf(
        ImmutableArray<SpeechFragment> label, string? condition = null, int target = 0) =>
        new(target, label, If(condition));

    /// <summary>An option the writer left blank, which the compiler reports as a diagnostic.</summary>
    public static OptionEdge UnlabeledOption(int target = 0) => new(target, [], null);

    /// <summary>A choice the engine draws instead of offering.</summary>
    /// <remarks>
    /// <code>
    /// - `80%` Alice: Heads.
    ///
    /// - `20%` Alice: Tails.
    /// </code>
    /// </remarks>
    public static RandomChoiceNode RandomChoice(params Edge[] options) => new(0, [.. options]);

    /// <summary>A random choice at the id given.</summary>
    public static RandomChoiceNode RandomChoice(int id, params Edge[] options) => new(id, [.. options]);

    /// <summary>One random option with a number weight, such as <c>`50%`</c>.</summary>
    public static RandomOptionEdge Chance(
        double percentage, string? condition = null, int target = 0) =>
        new(target, new NumberWeight(percentage), If(condition));

    /// <summary>
    /// One random option with a key weight, such as <c>`Guard.Mood%`</c>, which the world
    /// answers at play time.
    /// </summary>
    public static RandomOptionEdge Chance(string key, string? condition = null, int target = 0) =>
        new(target, new QueryWeight(key), If(condition));

    /// <summary>
    /// One random option with an auto weight, <c>`%`</c>: an even share of whatever the rest leave.
    /// </summary>
    public static RandomOptionEdge EvenChance(string? condition = null, int target = 0) =>
        new(target, new AutoWeight(), If(condition));

    /// <summary>
    /// The way on from a node when no other way is taken: a line's next node, a menu's
    /// fall-through, or the way past a block whose conditions all fail.
    /// </summary>
    public static SuccessionEdge Succession(int target) => new(target);

    private static ImmutableArray<Edge> Ways(Edge[] ways, int? next) =>
        next is int target ? [.. ways, Succession(target)] : [.. ways];

    private static KeyCondition? If(string? condition) =>
        condition is null ? null : new KeyCondition(condition);

    private static ImmutableArray<SpeechFragment> Speech(string words) =>
        [new TextFragment(words)];
}
