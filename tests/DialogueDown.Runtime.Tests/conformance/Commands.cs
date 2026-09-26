using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Tests.Conformance.Readers;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Reads what a session sends.</summary>
/// <remarks>
/// A separate, testable concern from operating the runner. Each command has a reader of its own,
/// keyed so two claiming one key is a startup failure; <see cref="TryRead"/> answers
/// <see langword="false"/> for a send no reader owns, which the caller reports as not yet
/// playable, and a send that names a command but shapes it wrongly is a fixture bug.
/// </remarks>
internal static class Commands
{
    // Keyed rather than searched, so two readers claiming one key is a startup failure rather than
    // a silent win for whichever was registered first. A bare command names itself, a shaped one is
    // the single key wrapping its payload, and both kinds answer to one key space.
    private static readonly Dictionary<string, ICommandReader> _byKey =
        new ICommandReader[] { new NextReader(), new DoneReader(), new FailedReader(), new SupplyReader() }
            .ToDictionary(reader => reader.Key, StringComparer.Ordinal);

    /// <summary>The display name of a send: the command it names, or its message when it names none.</summary>
    /// <param name="send">What the session sends, in the corpus's own words.</param>
    /// <returns>The display name.</returns>
    public static string NameOf(Send send) =>
        TryReadKeyAndPayload(send.Message, out var key, out _) ? key : send.Message.ToJsonString();

    /// <summary>Reads the command a send names.</summary>
    /// <param name="send">What the session sends, in the corpus's own words.</param>
    /// <param name="command">The command, or <see langword="null"/> when no reader owns the send.</param>
    /// <returns><see langword="true"/> when a reader owns the send and the shape it gives it is one it can take.</returns>
    /// <exception cref="InvalidFixtureException">The send gives a command a shape it cannot take.</exception>
    public static bool TryRead(Send send, [NotNullWhen(true)] out Command? command)
    {
        command = null;

        if (!TryReadKeyAndPayload(send.Message, out var key, out var payload))
        {
            return false;
        }

        if (!_byKey.TryGetValue(key, out var reader))
        {
            return false;
        }

        command = reader.Read(payload);

        return true;
    }

    /// <summary>Whether a send is the one that opens the run.</summary>
    /// <param name="send">What the session sends.</param>
    /// <returns><see langword="true"/> when the send opens the run.</returns>
    public static bool IsStart(Send send) =>
        send.Message is JsonObject message && message.ContainsKey("start");

    // A bare command names itself in a string; a shaped one is the single key it is sent under,
    // with its payload beside it. Anything else names no command at all.
    private static bool TryReadKeyAndPayload(
        JsonNode message,
        [NotNullWhen(true)] out string? key,
        out JsonNode? payload)
    {
        payload = null;

        if (message.GetValueKind() == JsonValueKind.String)
        {
            key = message.GetValue<string>();

            return true;
        }

        if (message is JsonObject { Count: 1 } shaped)
        {
            key = shaped.First().Key;
            payload = shaped.First().Value;

            return true;
        }

        key = null;

        return false;
    }
}
