using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Builds an option as a menu offers it, from the option its node lists.
/// </summary>
internal static class OfferedOptions
{
    /// <summary>One option as a menu offers it.</summary>
    /// <remarks>
    /// The label is the option's words with its commands left out: a command in a label is
    /// performed when the line the option leads to plays, never when the menu is shown.
    /// </remarks>
    /// <param name="option">The option, as its node lists it.</param>
    /// <param name="available">Whether the player can take it.</param>
    /// <returns>The option as offered.</returns>
    public static OfferedOption From(OptionEdge option, bool available)
    {
        ArgumentNullException.ThrowIfNull(option);

        return new([.. SpeechTemplate.Segments(option.Label).SelectMany(segment => segment.Words)], available);
    }
}
