namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Creates the checks a playbook must satisfy to be played by this build.
/// </summary>
/// <remarks>
/// The only place this build's limits from <see cref="PlaybookSupport"/> are passed to the
/// checkers, so what a default reader accepts can be read in one method.
/// </remarks>
public static class PlaybookCheckerFactory
{
    /// <summary>
    /// Creates the checks this build runs, in the order they are worth asking.
    /// </summary>
    /// <returns>A check that accepts exactly what this build can play.</returns>
    public static IPlaybookChecker CreateDefault() =>
        new CompositeChecker(
            CreateFormat(),
            new NodePositionChecker(),
            new ReferenceChecker(),
            new OutwardShapeChecker(),
            new BranchArmOrderChecker());

    /// <summary>
    /// Creates the check for whether this build can read a playbook's format at all.
    /// </summary>
    /// <returns>A check over the format version and the capabilities a playbook requires.</returns>
    public static IPlaybookChecker CreateFormat() =>
        new FormatChecker(
            new VersionChecker(
                PlaybookSupport.OldestReadableVersion,
                PlaybookSupport.NewestReadableVersion),
            new CapabilityChecker(PlaybookSupport.Capabilities));
}
