namespace DialogueDown.Visualization.Live.Configuration;

/// <summary>
/// How a <see cref="LiveSession.CreateConfig">create-config</see> request settled: the file was
/// freshly written from the starter template, an existing starter-template file was adopted
/// idempotently (a create retry after a lost response), a different pre-existing file was adopted
/// as recovery so the session is no longer config-less, or a retry of the file this session already
/// adopted found it diverged and is a conflict.
/// </summary>
internal enum CreateConfigStatus
{
    /// <summary>The starter <c>dialogue.toml</c> was created and adopted.</summary>
    Created,

    /// <summary>An existing file equal to the starter template was adopted without rewriting.</summary>
    Adopted,

    /// <summary>
    /// A different <c>dialogue.toml</c> already existed at the serve root and was adopted without
    /// overwriting it — valid TOML into the visualizer, invalid TOML as saved-invalid — so a
    /// config-less session recovers into the existing configuration instead of a dead end.
    /// </summary>
    AdoptedExisting,

    /// <summary>
    /// A create retry for the file this session already adopted found its content diverged from the
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
