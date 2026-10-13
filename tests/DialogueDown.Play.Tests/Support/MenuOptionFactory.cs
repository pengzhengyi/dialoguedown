using System.Collections.Immutable;
using DialogueDown.Play.Tui;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Play.Tests.Support;

/// <summary>
/// Builds the options a menu shows, each from its words and whether the player can take it.
/// </summary>
internal static class MenuOptionFactory
{
    /// <summary>One option, labelled with plain words.</summary>
    /// <param name="label">The words the option shows.</param>
    /// <param name="available">Whether the player can take it.</param>
    /// <returns>The option.</returns>
    public static MenuOption Option(string label, bool available = true) => new([Text(label)], available);

    /// <summary>A menu of options, in the order they are offered.</summary>
    /// <param name="options">Each option's words and whether the player can take it.</param>
    /// <returns>The options.</returns>
    public static ImmutableArray<MenuOption> Menu(params (string Label, bool Available)[] options) =>
        [.. options.Select(option => Option(option.Label, option.Available))];
}
