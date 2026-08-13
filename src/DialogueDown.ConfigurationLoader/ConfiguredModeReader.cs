using DialogueDown.Configuration;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads the top-level <c>mode</c> key of a parsed <see cref="DocumentSyntax"/> into a
/// <see cref="CompilationMode"/>, validating it against the settable modes. Absent yields null, so
/// the caller keeps the built-in default. A malformed value — a non-string, or a name that is not a
/// settable mode (including <c>fail-fast</c>, which is an embedding contract rather than a reporting
/// mode) — is rejected with a located <see cref="DialogueConfigurationException"/>. Unrelated root
/// keys are ignored, so the format stays forward-compatible as new settings are added.
/// </summary>
internal sealed class ConfiguredModeReader
{
    private const string ModeKey = "mode";

    public CompilationMode? Read(DocumentSyntax document)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (var entry in document.KeyValues)
        {
            if (TomlKeys.Name(entry.Key) == ModeKey)
            {
                return ReadMode(entry);
            }
        }

        return null;
    }

    private static CompilationMode ReadMode(KeyValueSyntax entry)
    {
        if (entry.Value is not StringValueSyntax text)
        {
            throw TomlErrors.At("'mode' must be a string.", entry.Value!);
        }

        return CompilationModes.TryParse(text.Value!)
            ?? throw TomlErrors.At(
                $"Unknown mode '{text.Value}'. Use {CompilationModes.SettableNamesDescription}.",
                entry.Value);
    }
}
