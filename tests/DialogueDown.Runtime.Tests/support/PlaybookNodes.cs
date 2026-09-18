using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Nodes and arms built by hand, so a test that arranges a playbook says what it is about rather
/// than what a node's constructor takes.
/// </summary>
/// <remarks>
/// Each factory builds the simplest instance of its kind. A playbook is an immutable record with no
/// behavior, so a test builds a real node rather than standing in a substitute, and a node a test
/// hands to the runner has to be a real one anyway: stepping dispatches on the kind it finds.
/// </remarks>
internal static class PlaybookNodes
{
    /// <summary>A line node, said by a speaker and leading onward.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="speaker">Who says it, by index.</param>
    /// <param name="text">What is said.</param>
    /// <param name="next">Where succession leads.</param>
    /// <returns>The node.</returns>
    public static LineNode Line(int id, int speaker, string text, int next) =>
        new(id, speaker, [new TextFragment(text)], Condition: null, [new SuccessionEdge(next)]);

    /// <summary>A line node that only plays when the world says so.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="speaker">Who says it, by index.</param>
    /// <param name="text">What is said.</param>
    /// <param name="next">Where succession leads.</param>
    /// <param name="key">What the world is asked before the line plays.</param>
    /// <returns>The node.</returns>
    public static LineNode ConditionalLine(int id, int speaker, string text, int next, string key) =>
        new(id, speaker, [new TextFragment(text)], new KeyCondition(key), [new SuccessionEdge(next)]);

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
            [new DivertEdge(jumpTo, [], new KeyCondition(key)), new SuccessionEdge(next)]);

    /// <summary>A jump on its own line: nothing said, nothing performed, one way out.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="jumpTo">Where the jump leads.</param>
    /// <returns>The node.</returns>
    public static ControlNode Jump(int id, int jumpTo) =>
        new(id, [], Condition: null, [new DivertEdge(jumpTo, [], Condition: null)]);

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

    /// <summary>A line node nothing leads on from.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="text">What is said.</param>
    /// <returns>The node.</returns>
    public static LineNode Dead(int id, string text) =>
        new(id, Speaker: 0, [new TextFragment(text)], Condition: null, []);

    /// <summary>A choice node offering one option — a kind this pass cannot play.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="leadsTo">Where the option leads.</param>
    /// <param name="label">What the option offers, as the writer would read it.</param>
    /// <param name="ordered">Whether the options are a fixed order rather than a menu.</param>
    /// <returns>The node.</returns>
    public static ChoiceNode Choice(int id, int leadsTo, string label = "Go east", bool ordered = false) =>
        new(id, Ordered: ordered, [new OptionEdge(leadsTo, [new TextFragment(label)], Condition: null)]);

    /// <summary>A branch node with one gated arm — a kind this pass cannot play.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="leadsTo">Where the arm leads.</param>
    /// <param name="order">Where the arm sits in the branch's order.</param>
    /// <returns>The node.</returns>
    public static BranchNode Branch(int id, int leadsTo, int order = 0) =>
        new(id, [new BranchEdge(leadsTo, Order: order, Condition: null)]);

    /// <summary>A random choice with one auto-weighted option — a kind this pass cannot play.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="leadsTo">Where the option leads.</param>
    /// <returns>The node.</returns>
    public static RandomChoiceNode RandomChoice(int id, int leadsTo) =>
        new(id, [new RandomOptionEdge(leadsTo, new AutoWeight(), Condition: null)]);

    /// <summary>An end node: a playthrough that reaches it is over.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <returns>The node.</returns>
    public static EndNode End(int id) => new(id);

    /// <summary>One node of every kind the playbook format defines, each the simplest of its kind.</summary>
    /// <remarks>
    /// The one list of the kinds, so a test that walks them and a test that checks the list is
    /// complete cannot disagree about what "every kind" means.
    /// </remarks>
    /// <returns>The nodes, one per kind.</returns>
    public static ImmutableArray<Node> OneOfEachKind() =>
    [
        Line(0, speaker: 0, "Hello.", next: 1),
        End(0),
        new ControlNode(0, [], Condition: null, [new SuccessionEdge(1)]),
        Effects(0, next: 1, "fade in"),
        Choice(0, leadsTo: 1),
        Branch(0, leadsTo: 1),
        RandomChoice(0, leadsTo: 1),
    ];
}
