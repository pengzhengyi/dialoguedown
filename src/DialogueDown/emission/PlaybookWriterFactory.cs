namespace DialogueDown.Emission;

/// <summary>
/// The container-free composition root for the default <see cref="IPlaybookWriter"/>, for callers
/// that do not run a dependency injection container. Container callers get the same writer from
/// the <c>AddDialogueDown</c> registration.
/// </summary>
public static class PlaybookWriterFactory
{
    /// <summary>Creates the writer for the playbook format this build writes.</summary>
    /// <returns>A writer, ready to use.</returns>
    public static IPlaybookWriter CreateDefault() => new PlaybookWriter();
}
