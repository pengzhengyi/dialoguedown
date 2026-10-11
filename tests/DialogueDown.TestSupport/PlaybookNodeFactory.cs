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
/// A <c>condition</c> is the key the world must allow before the node or edge applies.
/// </para>
/// </remarks>
public static class PlaybookNodeFactory
{
    /// <summary>A line somebody says.</summary>
    public static LineNode Line(
        string words, int speaker = 0, string? condition = null, int id = 0) =>
        new(id, speaker, Speech(words), If(condition), []);

    /// <summary>
    /// A line whose speech is the fragments a test composed, so it can hold a query, a tag, or
    /// styling rather than plain words.
    /// </summary>
    public static LineNode LineOf(
        ImmutableArray<SpeechFragment> speech,
        int speaker = 0,
        string? condition = null,
        int id = 0,
        params Edge[] ways) =>
        new(id, speaker, speech, If(condition), [.. ways]);

    /// <summary>
    /// A line with no speech at all, which the format allows, leaving by the ways given.
    /// </summary>
    /// <remarks>
    /// For a test about a line's ways out, where what it says is beside the point.
    /// </remarks>
    public static LineNode SilentLine(
        int speaker = 0, string? condition = null, int id = 0, params Edge[] ways) =>
        new(id, speaker, [], If(condition), [.. ways]);

    /// <summary>Where play stops.</summary>
    public static EndNode End(int id = 0) => new(id);

    /// <summary>A block condition with the arms it tries in order.</summary>
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
    public static BranchNode Branch(params Edge[] arms) => new(0, [.. arms]);

    /// <summary>An arm of a block condition, taken when the world allows the condition.</summary>
    public static BranchEdge Arm(string condition, int target = 0) =>
        new(target, new KeyCondition(condition));

    /// <summary>The else of a block condition, taken when no arm before it is.</summary>
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

    /// <summary>A control node that neither performs anything nor names where it goes.</summary>
    public static ControlNode DivertingUnlabeled(int target = 0) =>
        new(0, [], null, [Divert(target: target)]);

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

    /// <summary>One arm of a menu, carrying the words the menu shows for it.</summary>
    public static OptionEdge Option(string words, string? condition = null, int target = 0) =>
        new(target, Speech(words), If(condition));

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
    public static RandomChoiceNode RandomChoice(params Edge[] arms) => new(0, [.. arms]);

    /// <summary>One arm of a random choice, at odds the writer fixed in the script.</summary>
    public static RandomOptionEdge Chance(
        double percentage, string? condition = null, int target = 0) =>
        new(target, new NumberWeight(percentage), If(condition));

    /// <summary>One arm of a random choice, at odds the world supplies at play time.</summary>
    public static RandomOptionEdge Chance(string key, string? condition = null, int target = 0) =>
        new(target, new QueryWeight(key), If(condition));

    /// <summary>One arm of a random choice, sharing evenly in whatever the rest leave.</summary>
    public static RandomOptionEdge EvenChance(string? condition = null, int target = 0) =>
        new(target, new AutoWeight(), If(condition));

    private static KeyCondition? If(string? condition) =>
        condition is null ? null : new KeyCondition(condition);

    private static ImmutableArray<SpeechFragment> Speech(string words) =>
        [new TextFragment(words)];
}
