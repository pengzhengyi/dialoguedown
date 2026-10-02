using DialogueDown.Playbook;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Runtime;

/// <summary>
/// What a run needs and never changes: the playbook, and lookups into it.
/// </summary>
/// <remarks>
/// The runtime looks up nodes and speakers only through <see cref="NodeAt"/> and
/// <see cref="SpeakerName"/>, so how a position addresses the playbook is decided in one place.
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

    /// <summary>The name of the speaker at an index.</summary>
    /// <param name="speaker">The speaker's position in the playbook's speaker table.</param>
    /// <returns>Their name, or <see langword="null"/> for the anonymous default speaker.</returns>
    public string? SpeakerName(int speaker) => Playbook.Speakers[speaker].Name;
}
