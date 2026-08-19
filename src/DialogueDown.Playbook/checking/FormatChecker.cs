namespace DialogueDown.Playbook;

/// <summary>
/// Refuses a playbook this build cannot read at all.
/// </summary>
/// <remarks>
/// Two questions in the order they are worth asking: whether the document has a shape we know,
/// then whether what fills that shape is a story we can tell. A document of an unknown version
/// may describe its capabilities in terms we would misread, so the version settles first.
/// </remarks>
public sealed class FormatChecker : IPlaybookChecker
{
    private readonly IPlaybookChecker _version;
    private readonly IPlaybookChecker _capabilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatChecker"/> class.
    /// </summary>
    /// <param name="version">Whether the document's shape is one we know.</param>
    /// <param name="capabilities">Whether what fills that shape is a story we can tell.</param>
    public FormatChecker(IPlaybookChecker version, IPlaybookChecker capabilities)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(capabilities);

        _version = version;
        _capabilities = capabilities;
    }

    /// <summary>Gets the checker for the format this build reads.</summary>
    public static FormatChecker Default { get; } =
        new(VersionChecker.Default, CapabilityChecker.Default);

    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        _version.Check(playbook);
        _capabilities.Check(playbook);
    }
}
