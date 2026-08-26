using DialogueDown.Playbook;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Runtime;

/// <summary>
/// What a run needs and never changes, and how to read it.
/// </summary>
/// <remarks>
/// A container, so that what a run depends on can grow -- entropy settings, the capabilities
/// whoever drives it declared -- while the signature every part of the runtime calls stays put.
/// <para>
/// It also owns how a playbook is addressed. A position is an index today and becomes a qualified
/// reference once play can cross into another script, so keeping the lookup here means that change
/// lands in one place rather than at every site that reads a node.
/// </para>
/// </remarks>
public sealed class PlayContext
{
    private PlayContext(PlaybookDocument playbook) => Playbook = playbook;

    /// <summary>Gets the playbook being played.</summary>
    public PlaybookDocument Playbook { get; }

    /// <summary>Gets where a playthrough begins when nothing says otherwise.</summary>
    public int Entry => Playbook.Entry;

    /// <summary>A context over one playbook.</summary>
    /// <param name="playbook">The playbook to play.</param>
    /// <returns>The context to step with.</returns>
    public static PlayContext Of(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        return new PlayContext(playbook);
    }

    /// <summary>The node a position addresses.</summary>
    /// <param name="position">Where in the playbook to look.</param>
    /// <returns>The node standing there.</returns>
    public Node NodeAt(int position) => Playbook.Nodes[position];

    /// <summary>Who speaks under an index.</summary>
    /// <param name="speaker">The speaker's position in the playbook's speaker table.</param>
    /// <returns>Their name, or <see langword="null"/> for the anonymous default speaker.</returns>
    public string? SpeakerName(int speaker) => Playbook.Speakers[speaker].Name;
}
