namespace DialogueDown.Configuration;

/// <summary>
/// The author-facing names of the unmodeled-Markdown vocabulary — the kebab-case words shared by
/// the <c>dialogue.toml</c> <c>[markdown.unmodeled]</c> section and any tool that displays the
/// configuration, so the surfaces never drift. Mirrors
/// <see cref="CompilationModes"/> for the compilation modes.
/// </summary>
public static class UnmodeledMarkdownNames
{
    private static readonly IReadOnlyDictionary<UnmodeledNodeKind, string> _kinds =
        new Dictionary<UnmodeledNodeKind, string>
        {
            [UnmodeledNodeKind.CodeBlock] = "code-block",
            [UnmodeledNodeKind.ThematicBreak] = "thematic-break",
            [UnmodeledNodeKind.Table] = "table",
            [UnmodeledNodeKind.RawHtml] = "raw-html",
            [UnmodeledNodeKind.Autolink] = "autolink",
            [UnmodeledNodeKind.Other] = "other",
        };

    private static readonly IReadOnlyDictionary<UnmodeledNodeHandling, string> _handlings =
        new Dictionary<UnmodeledNodeHandling, string>
        {
            [UnmodeledNodeHandling.Keep] = "keep",
            [UnmodeledNodeHandling.Ignore] = "ignore",
        };

    /// <summary>The kind names as a human-readable list for help text and error messages.</summary>
    public static string KindNamesDescription { get; } = Describe(_kinds.Values);

    /// <summary>The handling names as a human-readable list for help text and error messages.</summary>
    public static string HandlingNamesDescription { get; } = Describe(_handlings.Values);

    /// <summary>The canonical author-facing name of an unmodeled kind.</summary>
    public static string NameOf(UnmodeledNodeKind kind) => _kinds[kind];

    /// <summary>The canonical author-facing name of a handling.</summary>
    public static string NameOf(UnmodeledNodeHandling handling) => _handlings[handling];

    /// <summary>Maps an author-facing name to its kind, or null when the name is unknown.</summary>
    public static UnmodeledNodeKind? TryParseKind(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var (kind, kindName) in _kinds)
        {
            if (kindName == name)
            {
                return kind;
            }
        }

        return null;
    }

    /// <summary>Maps an author-facing name to its handling, or null when the name is unknown.</summary>
    public static UnmodeledNodeHandling? TryParseHandling(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var (handling, handlingName) in _handlings)
        {
            if (handlingName == name)
            {
                return handling;
            }
        }

        return null;
    }

    private static string Describe(IEnumerable<string> names) =>
        string.Join(", ", names.Select(name => $"'{name}'"));
}
