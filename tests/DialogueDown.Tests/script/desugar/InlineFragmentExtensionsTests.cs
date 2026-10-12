using DialogueDown.Script.Desugar;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class InlineFragmentExtensionsTests
{
    [Fact]
    public void IsBlank_TextOfOnlyWhitespace_IsTrue()
    {
        Assert.True(Text("   ").IsBlank());
    }

    [Fact]
    public void IsBlank_TextWithVisibleContent_IsFalse()
    {
        Assert.False(Text("go").IsBlank());
    }

    [Fact]
    public void IsBlank_LineBreak_IsFalse()
    {
        // A soft break ends a single-line construct; it is not padding.
        Assert.False(LineBreak().IsBlank());
    }

    [Fact]
    public void IsBlank_NonTextFragment_IsFalse()
    {
        Assert.False(Link("#play", Text("go")).IsBlank());
    }

    [Fact]
    public void NonBlank_WhitespaceText_IsFalse()
    {
        Assert.False(Text("   ").NonBlank());
    }

    [Fact]
    public void NonBlank_TextWithVisibleContent_IsTrue()
    {
        Assert.True(Text("go").NonBlank());
    }

    [Fact]
    public void NonBlank_NonTextFragment_IsTrue()
    {
        Assert.True(LineBreak().NonBlank());
    }
}
