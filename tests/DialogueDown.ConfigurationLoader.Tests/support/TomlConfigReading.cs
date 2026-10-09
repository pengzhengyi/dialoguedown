using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests.Support;

/// <summary>
/// Parses a <c>dialogue.toml</c> snippet and runs one configuration reader over it, so a reader's
/// test states only the TOML and what the reader makes of it.
/// </summary>
internal static class TomlConfigReading
{
    /// <summary>The source name every snippet is parsed under, and so the source of a located error.</summary>
    public const string SourceName = "dialogue.toml";

    /// <summary>Parses a TOML snippet under <see cref="SourceName"/>.</summary>
    /// <param name="toml">The snippet.</param>
    /// <returns>The parsed document.</returns>
    public static DocumentSyntax Parse(string toml) =>
        new TomlDocumentParser(SourceName).Parse(toml);

    /// <summary>What <paramref name="reader"/> reads from a snippet it accepts.</summary>
    /// <typeparam name="T">What the reader returns.</typeparam>
    /// <param name="toml">The snippet.</param>
    /// <param name="reader">The reader's <c>Read</c> method.</param>
    /// <returns>What the reader returned.</returns>
    public static T Read<T>(string toml, Func<DocumentSyntax, T> reader) => reader(Parse(toml));

    /// <summary>Asserts that <paramref name="reader"/> rejects a snippet, and returns why.</summary>
    /// <typeparam name="T">What the reader would have returned.</typeparam>
    /// <param name="toml">The snippet.</param>
    /// <param name="reader">The reader's <c>Read</c> method.</param>
    /// <returns>The error the reader raised.</returns>
    public static DialogueConfigurationException AssertRejects<T>(string toml, Func<DocumentSyntax, T> reader) =>
        Assert.Throws<DialogueConfigurationException>(() => Read(toml, reader));
}
