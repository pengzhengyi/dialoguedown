namespace DialogueDown.Cli.Fixing;

/// <summary>One rendered piece of a hunk line, and whether the fix changed it.</summary>
internal sealed record HunkSegment(string Text, bool Changed);
