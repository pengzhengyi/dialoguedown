using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdigNodeFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class UnmodeledNodeHandlingPolicyExtensionsTests
{
    private static readonly IUnmodeledNodeHandlingPolicy _default =
        DefaultUnmodeledNodeHandlingPolicy.Instance;

    [Fact]
    public void ShouldIgnore_ABlockTheDefaultPolicyDrops_IsTrue()
    {
        // The extension classifies the node itself, so a caller never names the kind.
        Assert.True(_default.ShouldIgnore(ThematicBreak()));
        Assert.True(_default.ShouldIgnore(PipeTable()));
        Assert.True(_default.ShouldIgnore(FencedCode()));
    }

    [Fact]
    public void ShouldIgnore_ABlockTheDefaultPolicyKeeps_IsFalse() =>
        Assert.False(_default.ShouldIgnore(HtmlBlockNode()));

    [Fact]
    public void ShouldKeep_ABlockTheDefaultPolicyKeeps_IsTrue()
    {
        Assert.True(_default.ShouldKeep(HtmlBlockNode()));
        Assert.True(_default.ShouldKeep(UnrecognizedBlock()));
    }

    [Fact]
    public void ShouldKeep_ABlockTheDefaultPolicyDrops_IsFalse() =>
        Assert.False(_default.ShouldKeep(ThematicBreak()));

    [Fact]
    public void ShouldIgnore_AnInline_FollowsThePolicyForItsKind()
    {
        Assert.False(_default.ShouldIgnore(Autolink()));
        Assert.True(
            TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.Autolink)
                .ShouldIgnore(Autolink()));
    }

    [Fact]
    public void ShouldKeep_AnInline_FollowsThePolicyForItsKind()
    {
        Assert.True(_default.ShouldKeep(InlineHtml()));
        Assert.False(
            TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.RawHtml)
                .ShouldKeep(InlineHtml()));
    }

    [Fact]
    public void BothQuestions_AreFalse_ForAHandlingThisCodeDoesNotKnow()
    {
        // The two are deliberately not each other's negation: a handling nothing here
        // understands answers "no" to both, which is what lets a caller notice it rather than
        // guess. MarkdigUnmodeledNodeHandler relies on this to fail loudly.
        var policy = new UnknownHandlingPolicy();

        Assert.False(policy.ShouldIgnore(ThematicBreak()));
        Assert.False(policy.ShouldKeep(ThematicBreak()));
        Assert.False(policy.ShouldIgnore(Autolink()));
        Assert.False(policy.ShouldKeep(Autolink()));
    }
}
