using System.Globalization;

using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a playbook written in a format version this reader does not read.
/// </summary>
public sealed class VersionChecker : IPlaybookChecker
{
    private readonly int _oldest;
    private readonly int _newest;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionChecker"/> class.
    /// </summary>
    /// <param name="oldest">The oldest format version to accept.</param>
    /// <param name="newest">The newest format version to accept.</param>
    public VersionChecker(int oldest, int newest)
    {
        // With oldest above newest, every playbook would be refused with a message blaming the
        // document, so the mistake is reported here instead.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(oldest, newest);

        _oldest = oldest.AssertNotNegative(nameof(oldest));
        _newest = newest;
    }

    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        var version = playbook.Format.Version;

        if (version > _newest)
        {
            throw new InvalidPlaybookException(
                $"This playbook is version {GetVersionString(version)}, and this build reads up to {GetVersionString(_newest)}.");
        }

        if (version < _oldest)
        {
            throw new InvalidPlaybookException(
                $"This playbook is version {GetVersionString(version)}, and this build reads back to {GetVersionString(_oldest)}.");
        }
    }

    private static string GetVersionString(int version) => version.ToString(CultureInfo.InvariantCulture);
}
