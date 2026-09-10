using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Reads a session's <c>send</c> message as the runtime command it names.</summary>
/// <remarks>
/// A separate, testable concern from operating the runner. It recognizes only the commands this
/// pass can play; anything else reads as <see langword="null"/>, which the caller reports as not
/// yet runnable.
/// </remarks>
internal static class Commands
{
    /// <summary>Reads the command a <c>send</c> message names.</summary>
    /// <param name="message">The <c>send</c> value, in the corpus's own words.</param>
    /// <returns>The command, or <see langword="null"/> when nothing plays it yet.</returns>
    public static Command? Read(JsonNode message) =>
        message.GetValueKind() == JsonValueKind.String && message.GetValue<string>() == "next"
            ? new Next()
            : null;
}
