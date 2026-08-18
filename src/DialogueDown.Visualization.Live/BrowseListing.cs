namespace DialogueDown.Visualization.Live;

/// <summary>
/// A directory listing under a <see cref="BrowseRoot"/>: the browsed directory and its
/// parent as root-relative paths (the root itself is the empty string, and its parent is
/// <c>null</c>), plus the sub-directories and the <c>.dialogue.md</c> sources it holds,
/// each root-relative with <c>/</c> separators.
/// </summary>
internal readonly record struct BrowseListing(
    string Path,
    string? Parent,
    IReadOnlyList<string> Directories,
    IReadOnlyList<string> Sources);
