using NSubstitute;
using Spectre.Console;
using Spectre.Console.Testing;

namespace DialogueDown.Play.Tests.Support;

/// <summary>
/// Consoles for the reads a test console cannot produce: a wait that never answers, and a console
/// whose input has gone.
/// </summary>
internal static class StubConsoles
{
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
    public static IAnsiConsole WithNoKey() => WithInput(() => Task.FromResult<ConsoleKeyInfo?>(null));

    /// <summary>A console that never answers, so a wait can be cancelled while it waits.</summary>
    public static IAnsiConsole ThatNeverAnswers() =>
        WithInput(() => new TaskCompletionSource<ConsoleKeyInfo?>().Task);
}
