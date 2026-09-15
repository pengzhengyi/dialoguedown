using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks a reader accepts, generated rather than written out by hand.
/// </summary>
/// <remarks>
/// Only what this pass can play is drawn: a line that carries on, and an end. A later pass adds
/// its node and edge kinds here as it teaches the runner to play them, so a walk keeps covering
/// everything a run can meet.
/// <para>
/// A drawn playbook may loop, may begin anywhere, and may leave an end unreachable. All three are
/// shapes a script can compile to, and each is a walk worth taking.
/// </para>
/// </remarks>
internal static class PlaybookGen
{
    private const int MostNodes = 10;
    private const int MostSpeakers = 3;

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

    // Drawn without an index, then given one: a node states its own position, so it can only be
    // built once its place among the others is known.
    private static Gen<NodeDraft> Draft(int nodes, int speakers) =>
        Gen.Select(
            Gen.Bool,
            Gen.Int[0, speakers - 1],
            Gen.Int[0, nodes - 1],
            (ends, speaker, onward) => new NodeDraft(ends, speaker, onward));

    private static PlaybookDocument Document(NodeDraft[] drafts, int speakers, int entry) =>
        Playbooks.Document(
            [.. drafts.Select((draft, id) => draft.At(id))], Speakers(speakers), entry);

    // The first speaker is the anonymous default, so a drawn line can be said by nobody in
    // particular as well as by somebody named.
    private static ImmutableArray<PlaybookSpeaker> Speakers(int speakers) =>
        [.. Enumerable.Range(0, speakers).Select(at =>
            at == 0
                ? new PlaybookSpeaker(Id: null, Name: null, Default: true, Tags: [])
                : new PlaybookSpeaker(Id: null, Name: $"Speaker {at}", Default: false, Tags: []))];

    /// <summary>A node before it knows where it stands.</summary>
    /// <param name="Ends">Whether it ends the run rather than speaking.</param>
    /// <param name="Speaker">Who says it, by index.</param>
    /// <param name="Onward">Where succession leads.</param>
    private readonly record struct NodeDraft(bool Ends, int Speaker, int Onward)
    {
        /// <summary>The node, standing at a position.</summary>
        /// <param name="id">Its position in the playbook.</param>
        /// <returns>The node.</returns>
        public Node At(int id) =>
            Ends
                ? new EndNode(id)
                : new LineNode(
                    id, Speaker, [new TextFragment("Something.")], Condition: null, [new SuccessionEdge(Onward)]);
    }
}
