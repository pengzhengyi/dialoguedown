using DialogueDown.Configuration;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// A policy that answers with an <see cref="UnmodeledNodeHandling"/> value outside the defined
/// members, so a test can show the front end throws rather than guessing.
/// </summary>
internal sealed class UnknownHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => (UnmodeledNodeHandling)(-1);
}
