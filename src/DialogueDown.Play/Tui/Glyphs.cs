using Spectre.Console;

namespace DialogueDown.Play.Tui;

/// <summary>The few marks this player draws itself, in the form the console can show.</summary>
/// <remarks>
/// A terminal without Unicode gets ASCII stand-ins rather than a row of replacement characters,
/// so the cursor, the cross, and the key hints all still read.
/// </remarks>
internal static class Glyphs
{
    /// <summary>The mark beside the option under the cursor.</summary>
    public static string Cursor(IAnsiConsole console) => Unicode(console) ? "\u276f" : ">";

    /// <summary>The mark beside a notice.</summary>
    public static string Cross(IAnsiConsole console) => Unicode(console) ? "\u2717" : "x";

    /// <summary>The arrows a menu footer names.</summary>
    public static string Arrows(IAnsiConsole console) => Unicode(console) ? "\u2191\u2193" : "^v";

    /// <summary>A separator between phrases of a hint.</summary>
    public static string Separator(IAnsiConsole console) => Unicode(console) ? "\u00b7" : "-";

    private static bool Unicode(IAnsiConsole console) => console.Profile.Capabilities.Unicode;
}
