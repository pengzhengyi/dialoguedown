using Spectre.Console;
using Spectre.Console.Testing;

namespace DialogueDown.Play.Tests.Support;

/// <summary>Consoles a player test writes to, with the capabilities the test is about.</summary>
internal static class Consoles
{
    /// <summary>An interactive console that strips styling, so assertions read as a person reads.</summary>
    public static TestConsole Plain()
    {
        var console = Interactive();
        console.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        return console;
    }

    /// <summary>An interactive console that keeps the styling and the escapes that carry it.</summary>
    public static TestConsole Styled()
    {
        var console = Interactive();
        console.EmitAnsiSequences = true;
        console.Profile.Capabilities.Ansi = true;
        return console;
    }

    /// <summary>A console that cannot be prompted, as a pipe or a redirect is.</summary>
    public static TestConsole NonInteractive()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = false;
        console.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        return console;
    }

    private static TestConsole Interactive()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        return console;
    }
}
