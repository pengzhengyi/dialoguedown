using System.Collections.Immutable;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;

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
    public static PlayContext Of(
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
        Of([Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)], ["Alice"]);

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
        Of(
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
        Of([new ChoiceNode(0, Ordered: true, [new OptionEdge(1, [new TextFragment("Go east")], null)]), new EndNode(1)], ["Alice"]);

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

    /// <summary>A line node nothing leads on from.</summary>
    /// <param name="id">Its position in the playbook.</param>
    /// <param name="text">What is said.</param>
    /// <returns>The node.</returns>
    public static LineNode Dead(int id, string text) =>
        new(id, Speaker: 0, [new TextFragment(text)], Condition: null, []);

    private static PlaybookSpeaker Speaker(string? name) =>
        new(Id: null, Name: name, Default: name is null, Tags: []);
}
