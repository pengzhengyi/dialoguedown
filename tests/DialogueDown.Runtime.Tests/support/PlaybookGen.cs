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
/// Only what this pass can play is drawn: a line, an end, a jump, and a line or a control block
/// that asks the host to carry something out. A later pass adds its node and edge kinds here as
/// it teaches the runner to play them, so a walk keeps covering everything a run can meet.
/// <para>
/// A drawn playbook may loop, may begin anywhere, and may leave an end unreachable. All three are
/// shapes a script can compile to, and each is a walk worth taking.
/// </para>
/// </remarks>
internal static class PlaybookGen
{
    private const int MostNodes = 10;
    private const int MostSpeakers = 3;

    /// <summary>What a drawn node turns out to be.</summary>
    private enum Draws
    {
        /// <summary>A line that carries on.</summary>
        Line,

        /// <summary>The end of a run.</summary>
        End,

        /// <summary>A jump on its own line: nothing said, nothing performed, one way out.</summary>
        Jump,

        /// <summary>A control block asking the host to carry something out.</summary>
        Effects,

        /// <summary>A line carrying a jump, with a succession beside it to fall through to.</summary>
        LineThatJumps,
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

    // Drawn without an index, then given one: a node states its own position, so it can only be
    // built once its place among the others is known.
    private static Gen<NodeDraft> Draft(int nodes, int speakers) =>
        Gen.Select(
            Gen.OneOfConst(Draws.Line, Draws.End, Draws.Jump, Draws.Effects, Draws.LineThatJumps),
            Gen.Int[0, speakers - 1],
            Gen.Int[0, nodes - 1],
            Gen.Int[0, nodes - 1],
            (draws, speaker, onward, elsewhere) =>
                new NodeDraft(draws, speaker, onward, elsewhere));

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
    private readonly record struct NodeDraft(Draws Draws, int Speaker, int Onward, int Elsewhere)
    {
        /// <summary>The node, standing at a position.</summary>
        /// <param name="id">Its position in the playbook.</param>
        /// <returns>The node.</returns>
        public Node At(int id) => Draws switch
        {
            Draws.End => new EndNode(id),
            Draws.Jump => new ControlNode(id, [], Condition: null, [Jump(Elsewhere)]),
            Draws.Effects => new ControlNode(
                id,
                [new DefaultCommandFragment("do something")],
                Condition: null,
                [new SuccessionEdge(Onward)]),
            Draws.LineThatJumps => Speaks(id, [Jump(Elsewhere), new SuccessionEdge(Onward)]),
            _ => Speaks(id, [new SuccessionEdge(Onward)]),
        };

        private LineNode Speaks(int id, ImmutableArray<Edge> out_) =>
            new(id, Speaker, [new TextFragment("Something.")], Condition: null, out_);

        // Unconditional, because a jump the world must allow is one this pass refuses on arrival.
        private static DivertEdge Jump(int target) => new(target, [], Condition: null);
    }
}
