using System.Text.Json.Nodes;
using DialogueDown.Conformance;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Session entries built from the JSON a fixture would carry.</summary>
/// <remarks>
/// A test says what a session sends or expects in the corpus's own words, and reads it the same
/// way a fixture is read.
/// </remarks>
internal static class SessionEntries
{
    /// <summary>A send carrying the message this JSON describes.</summary>
    /// <param name="json">The message, as a fixture would write it.</param>
    /// <returns>The entry.</returns>
    public static Send Sent(string json) => new(Json(json));

    /// <summary>A send naming a bare command, as <c>next</c> does.</summary>
    /// <param name="command">The command's name, unquoted.</param>
    /// <returns>The entry.</returns>
    public static Send SentCommand(string command) => Sent($"\"{command}\"");

    /// <summary>An expectation claiming what this JSON describes.</summary>
    /// <param name="json">The claims, as a fixture would write them.</param>
    /// <returns>The entry.</returns>
    public static Expect Expected(string json) => new(Json(json));

    /// <summary>A value as a fixture would carry it.</summary>
    /// <param name="json">The value, as a fixture would write it. A fixture never writes a bare
    /// <c>null</c> where a value belongs.</param>
    /// <returns>The parsed value.</returns>
    public static JsonNode Json(string json) =>
        JsonNode.Parse(json)
        ?? throw new ArgumentException("A fixture value is never the bare literal null.", nameof(json));
}
