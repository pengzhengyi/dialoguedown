using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Document;

/// <summary>
/// One key of a <see cref="ConfigurationTable"/>: its name, where the key was written, and its
/// value.
/// </summary>
/// <remarks>
/// <c>mode = "best-effort"</c> is the entry <c>mode</c>, located at <c>mode</c>, whose value is
/// located at <c>"best-effort"</c>. A quoted key is read by its content, so <c>"mode"</c> is the
/// same key.
/// </remarks>
internal sealed record ConfigurationEntry(
    string Key, ConfigurationSourceLocation KeyLocation, ConfigurationValue Value)
{
    /// <summary>
    /// The value as a string; a value of any other kind throws an error located at the value.
    /// </summary>
    public string RequireString() => Value is ConfigurationScalar { Value: string text }
        ? text
        : throw new DialogueConfigurationException($"'{Key}' must be a string.", Value.Location);
}
