using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>
/// What a command reader makes of a send's payload, asserted in the words a fixture uses.
/// </summary>
internal static class CommandReaderAssert
{
    /// <summary>Asserts a payload reads as exactly this command: the same kind, carrying the same.</summary>
    /// <param name="reader">The reader under test.</param>
    /// <param name="payload">
    /// What the send carries beside its key, as a fixture writes it, or <see langword="null"/> for a
    /// bare send such as <c>"next"</c>.
    /// </param>
    /// <param name="expected">The command the payload should read as.</param>
    public static void AssertReadAs(ICommandReader reader, string? payload, Command expected) =>
        Assert.Equal(expected, reader.Read(Parsed(payload)));

    /// <summary>Asserts a payload gives the command a shape it cannot take, so the fixture is wrong.</summary>
    /// <param name="reader">The reader under test.</param>
    /// <param name="payload">
    /// What the send carries beside its key, as a fixture writes it, or <see langword="null"/> for a
    /// bare send.
    /// </param>
    public static void AssertRejected(ICommandReader reader, string? payload) =>
        Assert.Throws<InvalidFixtureException>(() => reader.Read(Parsed(payload)));

    private static JsonNode? Parsed(string? payload) => payload is null ? null : JsonNode.Parse(payload);
}
