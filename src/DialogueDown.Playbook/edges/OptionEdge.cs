using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// One arm of a player's choice, carrying the text the menu shows.
/// </summary>
/// <param name="Target">The node this option leads to.</param>
/// <param name="Label">The speech the menu shows for this option.</param>
/// <param name="Condition">What must hold for the option to be available, or <c>null</c>.</param>
public sealed record OptionEdge(
    int Target, ImmutableArray<SpeechFragment> Label, Condition? Condition) : Edge(Target)
{
    /// <summary>
    /// Gets the speech the menu shows for this option.
    /// </summary>
    /// <remarks>
    /// Compiled in rather than discovered, so presenting a menu never reads the target node —
    /// which keeps a menu free of the side effects a peek could trigger.
    /// </remarks>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("label")]
    public ImmutableArray<SpeechFragment> Label { get; } = Label.OrEmpty();

    /// <summary>
    /// Gets what must hold for the option to be available, or <c>null</c> when it always is.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}
