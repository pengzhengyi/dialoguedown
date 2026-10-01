using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Speech;
using Generator.Equals;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// A jump that does not return: control transfers to the target and reading order does not resume.
/// </summary>
/// <param name="Target">The node control transfers to.</param>
/// <param name="Label">What the writer called this jump.</param>
/// <param name="Condition">What must hold for the jump to fire, or <c>null</c>.</param>
[Equatable]
public sealed partial record DivertEdge(
    int Target, ImmutableArray<SpeechFragment> Label, Condition? Condition)
    : Edge(Target), IConditional
{
    /// <summary>
    /// Gets what the writer called this jump.
    /// </summary>
    /// <remarks>
    /// The link text of the jump: <c>=&gt; [Play tennis](#play-tennis)</c> has the label
    /// <c>Play tennis</c>. It is not part of the line's speech, so it is kept here. A host may show
    /// it, use it as a hint, or ignore it.
    /// </remarks>
    [OrderedEquality]
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
