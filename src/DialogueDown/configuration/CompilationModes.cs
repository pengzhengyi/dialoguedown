namespace DialogueDown.Configuration;

/// <summary>
/// The author-facing names of the <see cref="CompilationMode"/>s: the kebab-case words shared by
/// the CLI's <c>--mode</c> option, the <c>dialogue.toml</c> <c>mode</c> key, and the
/// visualization's Config tab, so all three agree. Only two modes are <em>settable</em> there:
/// <see cref="CompilationMode.FailFast"/> throws at the first error instead of returning
/// diagnostics, so a caller chooses it in code. Its name is still known, for display.
/// </summary>
public static class CompilationModes
{
    private static readonly IReadOnlyDictionary<CompilationMode, string> _names =
        new Dictionary<CompilationMode, string>
        {
            [CompilationMode.StageBoundary] = "stage-boundary",
            [CompilationMode.BestEffort] = "best-effort",
            [CompilationMode.FailFast] = "fail-fast",
        };

    private static readonly IReadOnlyList<CompilationMode> _settable =
        [CompilationMode.StageBoundary, CompilationMode.BestEffort];

    /// <summary>
    /// The settable mode names as a human-readable list for help text and error messages, e.g.
    /// <c>'stage-boundary' or 'best-effort'</c>.
    /// </summary>
    public static string SettableNamesDescription { get; } =
        string.Join(" or ", _settable.Select(mode => $"'{_names[mode]}'"));

    /// <summary>The canonical author-facing name of any mode, including the non-settable fail-fast.</summary>
    public static string NameOf(CompilationMode mode) => _names[mode];

    /// <summary>Maps an author-facing name to its mode, or null when it is not a settable mode.</summary>
    public static CompilationMode? TryParse(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var mode in _settable)
        {
            if (_names[mode] == name)
            {
                return mode;
            }
        }

        return null;
    }
}
