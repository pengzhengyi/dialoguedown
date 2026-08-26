using System.Collections.Immutable;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks built by hand, so a test about stepping says what it is about rather than compiling
/// a script to get there.
/// </summary>
internal static class Playbooks
{
    /// <summary>One line, then the end.</summary>
    /// <returns>A playbook a single <c>Next</c> finishes.</returns>
    public static PlaybookDocument OneLine() => Of(
        [Speaker("Alice")],
        [Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)]);

    /// <summary>Two lines, then the end.</summary>
    /// <returns>A playbook that shows succession going somewhere.</returns>
    public static PlaybookDocument TwoLines() => Of(
        [Speaker("Alice"), Speaker("Bob")],
        [Line(0, speaker: 0, "Hello.", next: 1), Line(1, speaker: 1, "Goodbye.", next: 2), new EndNode(2)]);

    /// <summary>One line said by nobody in particular.</summary>
    /// <returns>A playbook whose only speaker is the anonymous default.</returns>
    public static PlaybookDocument Anonymous() => Of(
        [new PlaybookSpeaker(Id: null, Name: null, Default: true, Tags: [])],
        [Line(0, speaker: 0, "Nobody said this.", next: 1), new EndNode(1)]);

    private static PlaybookSpeaker Speaker(string name) =>
        new(Id: null, Name: name, Default: false, Tags: []);

    private static LineNode Line(int id, int speaker, string text, int next) =>
        new(id, speaker, [new TextFragment(text)], Condition: null, [new SuccessionEdge(next)]);

    private static PlaybookDocument Of(
        ImmutableArray<PlaybookSpeaker> speakers, ImmutableArray<Node> nodes) =>
        new(
            new PlaybookFormat(PlaybookSupport.NewestReadableVersion, ["core"], []),
            script: "a-script.dialogue.md",
            entry: 0,
            anchors: ImmutableSortedDictionary<string, int>.Empty,
            speakers: speakers,
            nodes: nodes);
}
