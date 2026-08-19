namespace DialogueDown.Visualization.Live.Launching;

/// <summary>
/// How the launcher opens a source into a served session: read-only and auto-updating
/// (<see cref="View"/>) or editable (<see cref="Edit"/>). The offline snapshot is an
/// export (<c>-o</c>), not a launch mode.
/// </summary>
public enum LaunchMode
{
    /// <summary>Serve read-only and hot-reload when the source changes on disk.</summary>
    View,

    /// <summary>Serve an editable report and save edits back to the file.</summary>
    Edit,
}
