namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Creates the checks a playbook must satisfy to be played by this build.
/// </summary>
/// <remarks>
/// <para>
/// The one place the standard set is wired, so what a default reader accepts can be read in a
/// single method rather than gathered from a static on every checker.
/// </para>
/// <para>
/// Keeping it here also keeps each checker honest: a checker takes the policy it applies and
/// knows nothing of the one this build happens to use, which is what lets a runtime supply
/// narrower rules of its own.
/// </para>
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
            new ReferenceChecker());

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
