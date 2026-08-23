using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook;

/// <summary>
/// The compatibility header a reader checks before loading a playbook: the format
/// version that wrote it, and the capabilities it needs.
/// </summary>
/// <remarks>
/// Capabilities rather than the version number carry compatibility, so a new
/// construct gates only the playbooks that actually use it.
/// </remarks>
public sealed record PlaybookFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybookFormat"/> class.
    /// </summary>
    /// <param name="version">The playbook format version that wrote the document.</param>
    /// <param name="requires">Capabilities a runtime must understand, or refuse the playbook.</param>
    /// <param name="uses">Capabilities present in the document but safe to ignore.</param>
    [JsonConstructor]
    public PlaybookFormat(int version, ImmutableArray<string> requires, ImmutableArray<string> uses)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        Version = version;
        Requires = requires.OrEmpty();
        Uses = uses.OrEmpty();
    }

    /// <summary>
    /// Gets the playbook format version that wrote the document.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; }

    /// <summary>
    /// Gets the capabilities a runtime must understand to play this playbook
    /// correctly. An unknown entry is a hard refusal, never a skipped construct.
    /// </summary>
    [JsonPropertyName("requires")]
    public ImmutableArray<string> Requires { get; }

    /// <summary>
    /// Gets the capabilities the document uses but a runtime may ignore and still
    /// play the story correctly.
    /// </summary>
    [JsonPropertyName("uses")]
    public ImmutableArray<string> Uses { get; }
}
