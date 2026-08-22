namespace DialogueDown.Visualization.Live.Files;

/// <summary>
/// How paths are compared on this machine. Windows and macOS ignore case in file names while Linux
/// does not, so comparing the wrong way either misses a change or watches the wrong file.
/// </summary>
internal static class PathComparison
{
    /// <summary>The comparer to key paths by.</summary>
    public static StringComparer Comparer { get; } =
        OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    /// <summary>An absolute path without a trailing separator, so two spellings of one folder match.</summary>
    public static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
