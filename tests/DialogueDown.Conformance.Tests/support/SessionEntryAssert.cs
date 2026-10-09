using System.Text.Json.Nodes;

namespace DialogueDown.Conformance.Tests.Support;

/// <summary>Assertions about the entries of a playable fixture's session.</summary>
internal static class SessionEntryAssert
{
    /// <summary>Asserts that the entry sends <paramref name="message"/>.</summary>
    /// <param name="entry">The session entry.</param>
    /// <param name="message">The JSON it must send; formatting is ignored.</param>
    /// <returns>The entry as a send.</returns>
    public static Send AssertSends(SessionEntry entry, string message)
    {
        var send = Assert.IsType<Send>(entry);
        AssertSameJson(message, send.Message);

        return send;
    }

    /// <summary>Asserts that the entry expects <paramref name="message"/>.</summary>
    /// <param name="entry">The session entry.</param>
    /// <param name="message">The JSON it must expect; formatting is ignored.</param>
    /// <returns>The entry as an expectation.</returns>
    public static Expect AssertExpects(SessionEntry entry, string message)
    {
        var expect = Assert.IsType<Expect>(entry);
        AssertSameJson(message, expect.Message);

        return expect;
    }

    private static void AssertSameJson(string expected, JsonNode actual)
    {
        // JsonNode.DeepEquals ignores formatting but says nothing on failure, so the message shows both.
        Assert.True(
            JsonNode.DeepEquals(JsonNode.Parse(expected), actual),
            $"Expected {expected} but read {actual.ToJsonString()}");
    }
}
