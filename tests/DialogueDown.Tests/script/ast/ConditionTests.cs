using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ConditionTests
{
    [Fact]
    public void Constructor_ExposesKeyAndSpan_AndIsAnInlineFragment()
    {
        var span = SourceSpanFactory.Span();

        var condition = new Condition("Rainy", span);

        Assert.Equal("Rainy", condition.Key);
        Assert.Equal(span, condition.Span);
        Assert.IsAssignableFrom<InlineFragment>(condition);
        Assert.IsAssignableFrom<ScriptNode>(condition);
    }
}
