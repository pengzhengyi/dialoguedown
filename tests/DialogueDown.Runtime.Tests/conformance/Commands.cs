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

        return message.GetValueKind() == JsonValueKind.String && message.GetValue<string>() == "next"
            ? new Next()
            : null;
    }

    /// <summary>Whether a send is the one that opens the run.</summary>
    /// <param name="send">What the session sends.</param>
    /// <returns><see langword="true"/> when the send opens the run.</returns>
    public static bool IsStart(Send send) =>
        send.Message is JsonObject message && message.ContainsKey("start");
}
