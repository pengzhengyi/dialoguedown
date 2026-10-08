using System.Collections.Immutable;

namespace DialogueDown.Playbook;

/// <summary>
/// What this build of the reader can read: the format versions it accepts, and the capabilities
/// it honors.
/// </summary>
/// <remarks>
/// Checkers take these values as arguments and never read them directly, so a runtime can pass
/// narrower limits of its own.
/// </remarks>
public static class PlaybookSupport
{
    /// <summary>The newest format version this build understands.</summary>
    public const int NewestReadableVersion = 0;

    /// <summary>The oldest format version this build still reads.</summary>
    public const int OldestReadableVersion = 0;

    /// <summary>
    /// Gets the capabilities this build understands. A playbook requiring anything else is
    /// refused rather than played approximately.
    /// </summary>
    public static ImmutableHashSet<string> Capabilities { get; } = [Playbook.Capabilities.Core];
}
