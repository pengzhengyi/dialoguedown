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
    public static Send Sent(string json) => new(JsonNode.Parse(json)!);

    /// <summary>A send naming a bare command, as <c>next</c> does.</summary>
    /// <param name="command">The command's name, unquoted.</param>
    /// <returns>The entry.</returns>
    public static Send SentCommand(string command) => Sent($"\"{command}\"");

    /// <summary>An expectation claiming what this JSON describes.</summary>
    /// <param name="json">The claims, as a fixture would write them.</param>
    /// <returns>The entry.</returns>
    public static Expect Expected(string json) => new(JsonNode.Parse(json)!);
}
