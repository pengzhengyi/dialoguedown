using DialogueDown.Configuration;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Markdown;

public sealed class DefaultUnmodeledNodeHandlingPolicyTests
{
    private static readonly UnmodeledNodeKind[] _ignoredKinds =
    [
        UnmodeledNodeKind.CodeBlock,
        UnmodeledNodeKind.ThematicBreak,
        UnmodeledNodeKind.Table,
    ];

    private readonly IUnmodeledNodeHandlingPolicy _policy = DefaultUnmodeledNodeHandlingPolicy.Instance;

    [Fact]
    public void HandlingFor_AuthoringAids_IsIgnore()
    {
        // Code blocks (incl. diagrams), thematic breaks, and tables illustrate;
        // they are not spoken, so they default to being dropped.
        Assert.All(_ignoredKinds, kind =>
            Assert.Equal(UnmodeledNodeHandling.Ignore, _policy.HandlingFor(kind)));
    }

    [Fact]
    public void HandlingFor_EveryOtherKind_IsKeep()
    {
        // Any kind not explicitly ignored is kept as raw text — including kinds
        // added to the enum in the future.
        var rawTextKinds = Enum.GetValues<UnmodeledNodeKind>().Except(_ignoredKinds);

        Assert.All(rawTextKinds, kind =>
            Assert.Equal(UnmodeledNodeHandling.Keep, _policy.HandlingFor(kind)));
    }
}
