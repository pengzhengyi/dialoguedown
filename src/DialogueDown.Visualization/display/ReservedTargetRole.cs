namespace DialogueDown.Visualization.Display;

/// <summary>The structural role a reserved target plays in one dialogue run.</summary>
internal enum ReservedTargetRole
{
    /// <summary>An entry point where a run can begin.</summary>
    Entry,

    /// <summary>A terminal point that ends a run.</summary>
    Terminal,
}
