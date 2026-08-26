using DialogueDown.Playbook;

namespace DialogueDown.Runtime;

/// <summary>
/// What a run needs and never changes.
/// </summary>
/// <remarks>
/// A container, so that what a run depends on can grow -- entropy settings, the capabilities
/// whoever drives it declared -- while the signature every part of the runtime calls stays put.
/// </remarks>
public sealed class PlayContext
{
    private PlayContext(PlaybookDocument playbook) => Playbook = playbook;

    /// <summary>Gets the playbook being played.</summary>
    public PlaybookDocument Playbook { get; }

    /// <summary>A context over one playbook.</summary>
    /// <param name="playbook">The playbook to play.</param>
    /// <returns>The context to step with.</returns>
    public static PlayContext Of(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        return new PlayContext(playbook);
    }
}
