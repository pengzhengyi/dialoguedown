using DialogueDown.Configuration;

namespace DialogueDown.ConfigurationLoader.Tests.Support;

/// <summary>
/// Loads a <c>dialogue.toml</c> snippet through <see cref="TomlConfigurationLoader"/>, so a test
/// states only the TOML and what the loader makes of it.
/// </summary>
internal static class TomlConfigReading
{
    /// <summary>The source name every snippet is loaded under, and so the source of a located error.</summary>
    public const string SourceName = "dialogue.toml";

    /// <summary>The options the loader reads from a snippet.</summary>
    /// <param name="toml">The snippet.</param>
    /// <returns>The options it configures.</returns>
    public static CompilerOptions Parse(string toml) => TomlConfigurationLoader.Parse(toml, SourceName);
}
