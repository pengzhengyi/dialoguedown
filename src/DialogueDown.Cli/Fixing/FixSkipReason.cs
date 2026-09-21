namespace DialogueDown.Cli.Fixing;

/// <summary>Why a diagnostic's preferred fix was not applied to the script.</summary>
internal enum FixSkipReason
{
    /// <summary>An edit intersects a range that a fix earlier in the file already replaced.</summary>
    OverlapsAnAppliedFix,

    /// <summary>An edit falls outside the script text.</summary>
    OutsideTheScript,
}
