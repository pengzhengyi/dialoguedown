using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Cli;

/// <summary>
/// Resolves the <see cref="CompilerOptions"/> a command uses: an explicit <c>--config</c> path,
/// else the nearest <c>dialogue.toml</c> found by walking up from a starting directory, else
/// <see cref="CompilerOptions.Default"/>. The nearest file wins, so one file at a project root
/// serves the scripts in its subfolders. It is the only CLI type that touches the TOML loader.
/// </summary>
internal sealed class ProjectConfiguration
{
    /// <summary>The configuration file the CLI discovers by walking up the directory tree.</summary>
    public const string FileName = ConfigurationFile.DefaultName;

    /// <summary>
    /// Resolves the options for a compile. An <paramref name="explicitConfigPath"/> (the
    /// <c>--config</c> value) is loaded directly; otherwise the nearest <see cref="FileName"/> at
    /// or above <paramref name="startDirectory"/> is loaded, and the search stops at
    /// <paramref name="boundaryDirectory"/> when given (it is never read above the boundary).
    /// Nothing found yields <see cref="CompilerOptions.Default"/>.
    /// </summary>
    public CompilerOptions Resolve(
        string? explicitConfigPath, string startDirectory, string? boundaryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);
        var path = ResolvePath(explicitConfigPath, startDirectory, boundaryDirectory);
        return path is null ? CompilerOptions.Default : TomlConfigurationLoader.Load(path);
    }

    /// <summary>
    /// Resolves the applied configuration for a compile — the same discovery as
    /// <see cref="Resolve"/>, but keeping the file's path and raw text so a report can show
    /// the exact <c>dialogue.toml</c> alongside its resolved options. Nothing found yields the
    /// no-file applied configuration (the built-in defaults).
    /// </summary>
    public AppliedConfiguration ResolveApplied(
        string? explicitConfigPath, string startDirectory, string? boundaryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);
        var path = ResolvePath(explicitConfigPath, startDirectory, boundaryDirectory);
        return path is null
            ? AppliedConfiguration.WithoutFile(CompilerOptions.Default)
            : AppliedConfiguration.FromFile(path, File.ReadAllText(path), TomlConfigurationLoader.Load(path));
    }

    // The config file this compile uses: the explicit --config path, else the nearest
    // dialogue.toml within the boundary, else none.
    private static string? ResolvePath(
        string? explicitConfigPath, string startDirectory, string? boundaryDirectory) =>
        string.IsNullOrWhiteSpace(explicitConfigPath)
            ? Discover(startDirectory, boundaryDirectory)
            : explicitConfigPath;

    private static string? Discover(string startDirectory, string? boundaryDirectory)
    {
        var boundary = boundaryDirectory is null ? null : Path.GetFullPath(boundaryDirectory);
        for (var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
             directory is not null && IsWithinBoundary(directory.FullName, boundary);
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    // The walk climbs only within the boundary subtree, so a config above the boundary — the
    // served root's parent — is never read. A null boundary climbs to the filesystem root.
    private static bool IsWithinBoundary(string directory, string? boundary)
    {
        if (boundary is null)
        {
            return true;
        }

        var normalized = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var limit = boundary.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(normalized, limit, StringComparison.Ordinal)
            || normalized.StartsWith(limit + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
