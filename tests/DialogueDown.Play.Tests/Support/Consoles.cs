using NSubstitute;
using Spectre.Console;
using Spectre.Console.Testing;

namespace DialogueDown.Play.Tests.Support;

/// <summary>
/// Consoles a player test writes to and reads from: the test console's own, and one whose read is
/// scripted, for the reads a test console cannot produce — a wait that never answers, or a console
/// whose input has gone.
/// </summary>
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

    /// <summary>A console whose input answers with <paramref name="read"/>.</summary>
    /// <param name="read">What the input gives each time it is read.</param>
    public static IAnsiConsole WithInput(Func<Task<ConsoleKeyInfo?>> read)
    {
        var profile = new TestConsole().Profile;
        profile.Capabilities.Interactive = true;
        var input = Substitute.For<IAnsiConsoleInput>();
        input.ReadKeyAsync(true, Arg.Any<CancellationToken>()).Returns(_ => read());
        var console = Substitute.For<IAnsiConsole>();
        console.Profile.Returns(profile);
        console.Input.Returns(input);
        return console;
    }

    /// <summary>A console that has no key to give, as one whose input has ended.</summary>
    public static IAnsiConsole WithoutAKey() => WithInput(() => Task.FromResult<ConsoleKeyInfo?>(null));

    /// <summary>A console that never answers, so a wait can be cancelled while it waits.</summary>
    public static IAnsiConsole ThatNeverAnswers() =>
        WithInput(() => new TaskCompletionSource<ConsoleKeyInfo?>().Task);

    private static TestConsole Interactive()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        return console;
    }
}
