using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// A jump that does not return: control transfers to the target and reading order does not resume.
/// </summary>
/// <param name="Target">The node control transfers to.</param>
/// <param name="Label">What the writer called this jump.</param>
/// <param name="Condition">What must hold for the jump to fire, or <c>null</c>.</param>
public sealed record DivertEdge(
    int Target, ImmutableArray<SpeechFragment> Label, Condition? Condition) : Edge(Target)
{
    /// <summary>
    /// Gets what the writer called this jump.
    /// </summary>
    /// <remarks>
    /// Carried because it exists nowhere else: a jump is written inside a line but is no part of
    /// what that line says, so without this the words are gone. A host may show them, use them as
    /// a hint, or ignore them.
    /// </remarks>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("label")]
    public ImmutableArray<SpeechFragment> Label { get; } = Label.OrEmpty();

    /// <summary>
    /// Gets what must hold for the jump to fire, or <c>null</c> when it always does.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}
