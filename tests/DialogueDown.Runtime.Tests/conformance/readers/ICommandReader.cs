using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads one command a session can send.</summary>
/// <remarks>
/// One reader per command, keyed so two claiming one key is a startup failure rather than a silent
/// win for whichever was registered first — the shape the expectation matchers already use.
/// </remarks>
internal interface ICommandReader
{
    /// <summary>Gets the key in a send this reader owns, e.g. "done".</summary>
    string Key { get; }

    /// <summary>Reads the command the send names.</summary>
    /// <param name="payload">What the send carries beside the key, or <see langword="null"/> for a bare command.</param>
    /// <returns>The command.</returns>
    /// <exception cref="InvalidFixtureException">The send gives this command a shape it cannot take.</exception>
    Command Read(JsonNode? payload);
}
