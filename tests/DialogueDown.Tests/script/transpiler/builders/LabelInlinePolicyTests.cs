using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.InlinePolicyAssert;
using Md = DialogueDown.Tests.Support.MarkdownAstFactory;
using MdEmphasisKind = DialogueDown.Markdown.EmphasisKind;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class LabelInlinePolicyTests
{
    private static readonly LabelInlinePolicy _policy = new();

    [Fact]
    public void Supports_TextAndStyling_ButNotFunctionalElements()
    {
        Assert.True(_policy.Supports(Md.Text("hi")));
        Assert.True(_policy.Supports(Md.Emphasis(MdEmphasisKind.Bold, Md.Text("hi"))));

        Assert.False(_policy.Supports(Md.CodeSpan("q")));
        Assert.False(_policy.Supports(Md.Link("#x", Md.Text("l"))));
        Assert.False(_policy.Supports(Md.Image("i.png", Md.Text("a"))));
        Assert.False(_policy.Supports(Md.LineBreak()));
    }

    [Fact]
    public void SupportsJumps_IsFalse() => Assert.False(_policy.SupportsJumps);

    [Fact]
    public void Resolve_CodeSpan_KeepsItsBackticks() =>
        AssertResolvesToText(_policy, Md.CodeSpan("q"), "`q`");

    [Fact]
    public void Resolve_LineBreak_BecomesASpace() =>
        AssertResolvesToText(_policy, Md.LineBreak(), " ");

    [Fact]
    public void Resolve_NestedLink_ReconstructsRecursively() =>
        AssertResolvesToText(
            _policy, Md.Link("#x", Md.Text("go "), Md.CodeSpan("k")), "[go `k`](#x)");

    [Theory]
    [InlineData("italic", "![*alt*](i.png)")]
    [InlineData("bold", "![**alt**](i.png)")]
    [InlineData("strike", "![~~alt~~](i.png)")]
    public void Resolve_NestedImageWithStyling_ReconstructsEachMarker(
        string kind, string expected)
    {
        var emphasisKind = kind switch
        {
            "italic" => MdEmphasisKind.Italic,
            "bold" => MdEmphasisKind.Bold,
            _ => MdEmphasisKind.Strikethrough,
        };

        AssertResolvesToText(
            _policy, Md.Image("i.png", Md.Emphasis(emphasisKind, Md.Text("alt"))), expected);
    }

    [Fact]
    public void Resolve_UnknownEmphasisKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Resolve(_policy, Md.Link("#x", Md.Emphasis((MdEmphasisKind)99, Md.Text("x")))));

    [Fact]
    public void Resolve_UnknownInline_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Resolve(_policy, new UnknownMarkdownInline(Md.Span())));
}
