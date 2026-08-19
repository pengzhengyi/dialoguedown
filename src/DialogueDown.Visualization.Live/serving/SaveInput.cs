namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// One save request from the client: the buffer <see cref="Source"/>, its <see cref="Target"/>
/// (the dialogue document, or <c>"config"</c> for <c>dialogue.toml</c>), the
/// <see cref="ExpectedBaseline"/> the client believes is on disk, the
/// <see cref="Validation"/> policy (require valid Config, or allow invalid), and the
/// <see cref="Conflict"/> policy (check the expected baseline, or force overwrite after an
/// explicit confirmation). The client-side trigger (idle/explicit/navigation) is not carried:
/// it controls scheduling only, never what the server may write.
/// </summary>
internal sealed record SaveInput(
    string? Source,
    string? Target = null,
    string? ExpectedBaseline = null,
    string? Validation = null,
    string? Conflict = null)
{
    /// <summary>Whether this save targets the configuration file rather than the dialogue document.</summary>
    public bool IsConfig => string.Equals(Target, "config", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the client explicitly confirmed a force overwrite past the baseline check.</summary>
    public bool Overwrite => string.Equals(Conflict, "overwrite", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the save must produce valid Config (Auto/navigation) rather than persist invalid TOML.</summary>
    public bool RequireValid => string.Equals(Validation, "require-valid", StringComparison.OrdinalIgnoreCase);
}
