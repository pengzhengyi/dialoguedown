using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Builds options as a menu offers them, from the options its node lists.
/// </summary>
internal static class OfferedOptions
{
    /// <summary>One option as a menu offers it.</summary>
    /// <remarks>
    /// The label is the option's words with its commands left out and its queries filled from the
    /// world's answers. A command in a label is performed when the line the option leads to plays,
    /// never when the menu is shown.
    /// </remarks>
    /// <param name="option">The option, as its node lists it.</param>
    /// <param name="available">Whether the player can take it.</param>
    /// <param name="supply">
    /// What the world said about the label's queries, or <see langword="null"/> when nothing was
    /// asked.
    /// </param>
    /// <returns>The option as offered.</returns>
    public static OfferedOption From(OptionEdge option, bool available, Supply? supply = null)
    {
        ArgumentNullException.ThrowIfNull(option);

        var words = SpeechTemplate.Words(option.Label);

        return new(supply?.Fill(words) ?? words, available);
    }

    /// <summary>Every option of a menu as the menu offers them.</summary>
    /// <param name="options">The menu's options, as its node lists them.</param>
    /// <param name="available">
    /// The positions of the options the player can take. Every other option is offered as one the
    /// player cannot take.
    /// </param>
    /// <param name="supply">
    /// What the world said about the labels' queries, or <see langword="null"/> when nothing was
    /// asked.
    /// </param>
    /// <returns>Every option as offered, in the order the node lists them.</returns>
    public static ImmutableArray<OfferedOption> From(
        ImmutableArray<OptionEdge> options, ImmutableArray<int> available, Supply? supply = null) =>
        [.. options.Select((option, at) => From(option, available.Contains(at), supply))];
}
