using System.Collections.Immutable;
using DialogueDown.Common;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Script.Semantics;
using GraphNodes = DialogueDown.Graph.Nodes;

namespace DialogueDown.Emission;

/// <summary>
/// Everybody who speaks in a playbook, and where each of them sits in it.
/// </summary>
/// <remarks>
/// Speakers are hoisted out of the lines that quote them so a host has one place to bind a
/// portrait, a voice, or a color. They are gathered from the lines themselves rather than from
/// everyone the script declared: a playbook carries what playing it needs, and somebody who
/// never speaks is not that.
/// </remarks>
internal sealed class SpeakerNumbering
{
    private readonly Numbering<SpeakerSymbol> _numbering;

    private SpeakerNumbering(Numbering<SpeakerSymbol> numbering) => _numbering = numbering;

    /// <summary>Gets everybody who speaks, in the order they first do.</summary>
    public ImmutableArray<PlaybookSpeaker> Speakers =>
        [.. _numbering.InOrder.Select(SpeakerMapping.Write)];

    /// <summary>Numbers the speaker of every line in a graph.</summary>
    /// <param name="nodes">The graph's nodes, in document order.</param>
    /// <returns>The numbering for those speakers.</returns>
    public static SpeakerNumbering Of(IReadOnlyList<GraphNodes.DialogueNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        // Numbered by reference, because the binder enriches one symbol in place as it meets a
        // speaker's declarations: two lines by Alice quote the same object, not equal ones.
        return new SpeakerNumbering(Numbering<SpeakerSymbol>.Of(
            nodes.OfType<GraphNodes.LineNode>().Select(line => line.Speaker),
            ReferenceEqualityComparer.Instance));
    }

    /// <summary>Where the given speaker sits in the playbook.</summary>
    /// <param name="speaker">The speaker to locate.</param>
    /// <returns>Their position in the speaker list.</returns>
    public int Position(SpeakerSymbol speaker) =>
        _numbering.TryPosition(speaker, out var position)
            ? position
            : throw new ArgumentException(
                $"{speaker} says nothing in this graph, so no line can name them.",
                nameof(speaker));
}
