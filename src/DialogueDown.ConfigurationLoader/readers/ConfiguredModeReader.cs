using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Readers;

/// <summary>
/// Reads the top-level <c>mode</c> setting into a <see cref="CompilationMode"/>, validating it
/// against the settable modes. Absent yields null, so the caller keeps the built-in default.
/// </summary>
/// <remarks>
/// <code>
/// mode = "best-effort"
/// </code>
/// A non-string, or a name that is not a settable mode, is rejected with a located
/// <see cref="DialogueConfigurationException"/>. That includes <c>fail-fast</c>, which throws
/// instead of reporting and is set only in code.
/// </remarks>
internal sealed class ConfiguredModeReader
{
    /// <summary>The root key this reader owns.</summary>
    public const string Key = "mode";

    public CompilationMode? Read(ConfigurationTable root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return root.Find(Key) is { } entry ? ReadMode(entry) : null;
    }

    private static CompilationMode ReadMode(ConfigurationEntry entry)
    {
        var name = entry.RequireString();
        return CompilationModes.TryParse(name)
            ?? throw new DialogueConfigurationException(
                $"Unknown mode '{name}'. Use {CompilationModes.SettableNamesDescription}.",
                entry.Value.Location);
    }
}
