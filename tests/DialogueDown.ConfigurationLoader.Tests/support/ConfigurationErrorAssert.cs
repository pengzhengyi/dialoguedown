using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Tests.Support;

/// <summary>Assertions about the error a <c>dialogue.toml</c> is rejected with.</summary>
internal static class ConfigurationErrorAssert
{
    /// <summary>
    /// Asserts that the loader rejects <paramref name="toml"/> with exactly <paramref name="message"/>,
    /// located at <paramref name="line"/> and <paramref name="column"/> of the snippet.
    /// </summary>
    /// <param name="toml">The snippet.</param>
    /// <param name="message">The whole message the error carries.</param>
    /// <param name="line">The 1-based line the error points at.</param>
    /// <param name="column">The 1-based column the error points at.</param>
    public static void AssertRejects(string toml, string message, int line, int column)
    {
        var error = AssertRejects(toml);

        Assert.Equal(message, error.Message);
        Assert.Equal(new ConfigurationSourceLocation(TomlConfigReading.SourceName, line, column), error.Location);
    }

    /// <summary>Asserts that the loader rejects <paramref name="toml"/>, and returns the error.</summary>
    /// <param name="toml">The snippet.</param>
    /// <returns>The error, for a test whose message Tomlyn composes.</returns>
    public static DialogueConfigurationException AssertRejects(string toml) =>
        Assert.Throws<DialogueConfigurationException>(() => TomlConfigReading.Parse(toml));
}
