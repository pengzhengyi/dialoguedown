using System.Collections.Immutable;

namespace DialogueDown.Playbook;

/// <summary>
/// What this build of the reader can read: the format versions it accepts, and the capabilities
/// it honors.
/// </summary>
/// <remarks>
/// Gathered in one place so growing the format is a single, reviewable edit rather than a hunt
/// through the checkers. Checkers take the values they need as arguments and never read these
/// directly, so a runtime remains free to state a narrower envelope of its own.
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
