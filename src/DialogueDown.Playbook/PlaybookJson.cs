using System.Text.Json;

namespace DialogueDown.Playbook;

/// <summary>
/// The canonical JSON settings for reading and writing a playbook. Every reader and
/// writer must share these, because they are part of the format contract rather
/// than a caller's preference.
/// </summary>
public static class PlaybookJson
{
    /// <summary>
    /// Gets the settings a playbook is written with and read back by.
    /// </summary>
    /// <remarks>
    /// Indented by default: a playbook is meant to be opened, searched, and reasoned
    /// about, and readability is worth more than bytes for a file this size.
    /// </remarks>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
    };
}
