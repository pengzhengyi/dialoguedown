using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class JumpTests
{
    [Fact]
    public void Constructor_ExposesTargetLabelAndSpan_AndIsAnInlineFragment()
    {
        var span = SourceSpanFactory.Span();
        var label = new InlineFragment[] { Text("Play tennis"), CustomTag("eager") };

        var jump = new Jump("#play-tennis", label, span);

        Assert.Equal("#play-tennis", jump.Target);
        Assert.Equal(label, jump.Label);
        Assert.Equal(span, jump.Span);
        Assert.IsAssignableFrom<InlineFragment>(jump);
        Assert.IsAssignableFrom<ScriptNode>(jump);
    }

    [Fact]
    public void Constructor_AllowsEmptyLabel() =>
        Assert.Empty(new Jump("#x", [], SourceSpanFactory.Span()).Label);

    [Fact]
    public void Constructor_AllowsEmptyTarget_LeavingItUnresolved() =>
        Assert.Equal(string.Empty, new Jump(string.Empty, [], SourceSpanFactory.Span()).Target);

    [Fact]
    public void Constructor_NullTarget_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new Jump(null!, [], SourceSpanFactory.Span()));

    [Fact]
    public void Constructor_HasNoCondition_ByDefault() =>
        Assert.Null(new Jump("#x", [], SourceSpanFactory.Span()).Condition);

    [Fact]
    public void Constructor_CarriesAnOptionalCondition()
    {
        var condition = new Condition("Rainy", SourceSpanFactory.Span());

        var jump = new Jump("#x", [], SourceSpanFactory.Span(), condition);

        Assert.Same(condition, jump.Condition);
    }

    [Fact]
    public void IsConditional_ReflectsWhetherAConditionGuardsTheJump()
    {
        Assert.False(new Jump("#x", [], SourceSpanFactory.Span()).IsConditional);
        Assert.True(new Jump("#x", [], SourceSpanFactory.Span(),
            new Condition("Rainy", SourceSpanFactory.Span())).IsConditional);
    }
}
