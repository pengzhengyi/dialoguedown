namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a playbook this build cannot read at all.
/// </summary>
/// <remarks>
/// Checks the format version first, then the required capabilities: a document of an unknown
/// version may list its capabilities in a form this build would misread.
/// </remarks>
public sealed class FormatChecker : IPlaybookChecker
{
    private readonly IPlaybookChecker _version;
    private readonly IPlaybookChecker _capabilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatChecker"/> class.
    /// </summary>
    /// <param name="version">The check on the format version.</param>
    /// <param name="capabilities">The check on the capabilities a playbook requires.</param>
    public FormatChecker(IPlaybookChecker version, IPlaybookChecker capabilities)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(capabilities);

        _version = version;
        _capabilities = capabilities;
    }

    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        _version.Check(playbook);
        _capabilities.Check(playbook);
    }
}
