using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Tests.Support;

/// <summary>Assertions about the error a <c>dialogue.toml</c> is rejected with.</summary>
internal static class ConfigurationErrorAssert
{
    /// <summary>
    /// Asserts that the error points at <paramref name="line"/> of the snippet and that its
    /// message mentions each fragment.
    /// </summary>
    /// <param name="error">The error the snippet was rejected with.</param>
    /// <param name="line">The 1-based line of the snippet the error points at.</param>
    /// <param name="mentioning">Text the message must contain.</param>
    public static void AssertRejectedAt(DialogueConfigurationException error, int line, params string[] mentioning)
    {
        Assert.Equal(TomlConfigReading.SourceName, error.Location.Source);
        Assert.Equal(line, error.Location.Line);
        AssertMentions(error, mentioning);
    }

    /// <summary>Asserts that the error's message mentions each fragment.</summary>
    /// <param name="error">The error the snippet was rejected with.</param>
    /// <param name="fragments">Text the message must contain.</param>
    public static void AssertMentions(DialogueConfigurationException error, params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, error.Message, StringComparison.Ordinal);
        }
    }
}
