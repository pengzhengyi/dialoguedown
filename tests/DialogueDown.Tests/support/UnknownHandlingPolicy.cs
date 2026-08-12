using DialogueDown.Configuration;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// A policy from a future where <see cref="UnmodeledNodeHandling"/> has grown a member this code
/// was never taught to carry out, so a test can prove the front end notices rather than guesses.
/// </summary>
internal sealed class UnknownHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => (UnmodeledNodeHandling)(-1);
}
