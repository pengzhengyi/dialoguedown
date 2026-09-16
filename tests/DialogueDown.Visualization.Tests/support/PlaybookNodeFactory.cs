using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// Builds the playbook nodes, edges, and speakers a report test reads.
/// </summary>
/// <remarks>
/// Speech arrives as the words themselves, because a test over a summary asserts the words that
/// come out and reads best beside what went in. A node's id and its targets have defaults: a
/// summary is written from what a node holds, so most tests have no interest in where it sits or
/// where it leads.
/// </remarks>
internal static class PlaybookNodeFactory
{
    /// <summary>A line somebody says.</summary>
    public static LineNode Line(
        string words, int speaker = 0, Condition? condition = null, int id = 0) =>
        new(id, speaker, Speech(words), condition, []);

    /// <summary>A line with no speech at all, which the format allows.</summary>
    public static LineNode SilentLine(int speaker = 0, int id = 0) =>
        new(id, speaker, [], null, []);

    /// <summary>Where a run stops.</summary>
    public static EndNode End(int id = 0) => new(id);

    /// <summary>A block condition fanning out to the arms it tries in order.</summary>
    public static BranchNode Branch(params Edge[] arms) => new(0, [.. arms]);

    /// <summary>One arm of a block condition; a null key is the final else.</summary>
    public static BranchEdge Arm(string? key, int order = 0, int target = 0) =>
        new(target, order, key is null ? null : new KeyCondition(key));

    /// <summary>An effect-only line: something the host performs, attributed to nobody.</summary>
    public static ControlNode Control(params SpeechFragment[] effects) =>
        new(0, [.. effects], null, []);

    /// <summary>An effect-only line that fires only when the world answers to the given key.</summary>
    public static ControlNode ConditionalControl(string key, params SpeechFragment[] effects) =>
        new(0, [.. effects], new KeyCondition(key), []);

    /// <summary>
    /// A control node with no effects, which is what a bare scene-to-scene jump compiles to. Its
    /// one way out is the divert carrying the words the writer gave the jump.
    /// </summary>
    public static ControlNode Diverting(string label, int target = 0) =>
        new(0, [], null, [new DivertEdge(target, Speech(label), null)]);

    /// <summary>A control node that neither performs anything nor names where it goes.</summary>
    public static ControlNode DivertingUnlabeled(int target = 0) =>
        new(0, [], null, [new DivertEdge(target, [], null)]);

    /// <summary>One of the commands the host already knows how to perform.</summary>
    public static SpeechFragment DefaultCommand(string action) =>
        new DefaultCommandFragment(action);

    /// <summary>A command the host supplies, with the arguments the script passes it.</summary>
    public static SpeechFragment CustomCommand(string name, params string[] args) =>
        new CustomCommandFragment(name, [.. args]);

    /// <summary>A menu the player picks from; its options are the ways out.</summary>
    public static ChoiceNode Choice(params Edge[] options) => new(0, false, [.. options]);

    /// <summary>One arm of a menu, carrying the words the menu shows for it.</summary>
    public static OptionEdge Option(string words, string? condition = null, int target = 0) =>
        new(target, Speech(words), condition is null ? null : new KeyCondition(condition));

    /// <summary>An option the writer left blank, which the compiler reports as a diagnostic.</summary>
    public static OptionEdge UnlabeledOption(int target = 0) => new(target, [], null);

    /// <summary>A choice the engine draws instead of offering.</summary>
    public static RandomChoiceNode RandomChoice(params Edge[] arms) => new(0, [.. arms]);

    /// <summary>One arm of a random choice, at odds the writer fixed in the script.</summary>
    public static RandomOptionEdge Chance(
        double percentage, string? condition = null, int target = 0) =>
        new(
            target,
            new NumberWeight(percentage),
            condition is null ? null : new KeyCondition(condition));

    /// <summary>One arm of a random choice, at odds the world supplies at play time.</summary>
    public static RandomOptionEdge Chance(string key, string? condition = null, int target = 0) =>
        new(
            target,
            new QueryWeight(key),
            condition is null ? null : new KeyCondition(condition));

    /// <summary>One arm of a random choice, sharing evenly in whatever the rest leave.</summary>
    public static RandomOptionEdge EvenChance(string? condition = null, int target = 0) =>
        new(target, new AutoWeight(), condition is null ? null : new KeyCondition(condition));

    /// <summary>A question the world answers by key.</summary>
    public static Condition If(string key) => new KeyCondition(key);

    /// <summary>
    /// The playbook's speakers, in order. A null name is the anonymous speaker every script has.
    /// </summary>
    public static ImmutableArray<PlaybookSpeaker> Speakers(params string?[] names) =>
        [.. names.Select(name => new PlaybookSpeaker(null, name, false, []))];

    /// <summary>
    /// A line whose speech is the fragments a test composed, so it can hold a query, a tag, or
    /// styling rather than plain words.
    /// </summary>
    public static LineNode LineOf(ImmutableArray<SpeechFragment> speech, int speaker = 0, int id = 0) =>
        new(id, speaker, speech, null, []);

    /// <summary>Words a writer typed, including any braces they meant literally.</summary>
    public static SpeechFragment Words(string text) => new TextFragment(text);

    /// <summary>A question the game answers at play time.</summary>
    public static SpeechFragment Query(string key) => new QueryFragment(key);

    /// <summary>Words emphasized a certain way, which a summary reads for their words alone.</summary>
    public static SpeechFragment Emphasized(params SpeechFragment[] children) =>
        new StyledTextFragment(SpeechStyle.Italic, [.. children]);

    private static ImmutableArray<SpeechFragment> Speech(string words) =>
        [new TextFragment(words)];
}
