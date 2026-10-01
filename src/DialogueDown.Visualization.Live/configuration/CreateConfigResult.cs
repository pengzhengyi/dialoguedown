namespace DialogueDown.Visualization.Live.Configuration;

/// <summary>How a <see cref="LiveSession.CreateConfig">create-config</see> request settled.</summary>
internal enum CreateConfigStatus
{
    /// <summary>The starter <c>dialogue.toml</c> was created and adopted.</summary>
    Created,

    /// <summary>
    /// An existing file equal to the starter template was adopted without rewriting, as when a
    /// create is retried after its first response was lost.
    /// </summary>
    Adopted,

    /// <summary>
    /// A different <c>dialogue.toml</c> already existed at the serve root and was adopted without
    /// overwriting it: valid TOML is applied, and invalid TOML is recorded as saved-invalid.
    /// </summary>
    AdoptedExisting,

    /// <summary>
    /// A create retry for the file this session already adopted found its content differs from the
    /// starter template; nothing was written. The session already applies the file, so a reload
    /// opens it.
    /// </summary>
    Conflict,
}

/// <summary>
/// The outcome of a create-config request: its <see cref="Status"/> and a <see cref="Payload"/> —
/// the recompiled document JSON for <see cref="CreateConfigStatus.Created"/>,
/// <see cref="CreateConfigStatus.Adopted"/>, and <see cref="CreateConfigStatus.AdoptedExisting"/>,
/// or a reader-facing message for <see cref="CreateConfigStatus.Conflict"/>.
/// </summary>
internal sealed record CreateConfigResult(CreateConfigStatus Status, string Payload);
