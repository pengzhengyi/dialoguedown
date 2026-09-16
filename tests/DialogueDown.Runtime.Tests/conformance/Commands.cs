using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Reads what a session sends.</summary>
/// <remarks>
/// A separate, testable concern from operating the runner. It recognizes the commands this pass
/// can play; anything else reads as <see langword="null"/>, which the caller reports as not yet
/// runnable.
/// </remarks>
internal static class Commands
{
    /// <summary>Reads the command a send names.</summary>
    /// <param name="send">What the session sends, in the corpus's own words.</param>
    /// <returns>The command, or <see langword="null"/> when nothing plays it yet.</returns>
    public static Command? Read(Send send)
    {
        var message = send.Message;

        if (message.GetValueKind() == JsonValueKind.String)
        {
            return message.GetValue<string>() switch
            {
                "next" => new Next(),
                "done" => new Done(),
                _ => null,
            };
        }

        return message is JsonObject claims && claims["failed"] is JsonNode explanation
            ? new Failed(ReadExplanation(explanation))
            : null;
    }

    /// <summary>Whether a send is the one that opens the run.</summary>
    /// <param name="send">What the session sends.</param>
    /// <returns><see langword="true"/> when the send opens the run.</returns>
    public static bool IsStart(Send send) =>
        send.Message is JsonObject message && message.ContainsKey("start");

    // A failure carries the host's own words, so a fixture writes them; anything but a string
    // there is a fixture bug rather than a construct nobody has taught the harness.
    private static string ReadExplanation(JsonNode explanation) =>
        explanation.GetValueKind() == JsonValueKind.String
            ? explanation.GetValue<string>()
            : throw new InvalidFixtureException("A failed send carries the host's explanation, as a string.");
}
