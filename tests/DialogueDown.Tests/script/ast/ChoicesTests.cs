using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ChoicesTests
{
    [Fact]
    public void Constructor_ExposesOrderingOptionsAndSpan_AndIsAChoiceGroup()
    {
        var span = SourceSpanFactory.Span();
        var options = new[] { Choice(Line(Text("Is it really?"))), Choice(Line(Text("I agree."))) };

        var choices = new Choices(IsOrdered: true, options, span);

        Assert.True(choices.IsOrdered);
        Assert.Equal(options, choices.Options);
        Assert.Equal(span, choices.Span);
        Assert.IsAssignableFrom<ChoiceGroup>(choices);
        Assert.IsAssignableFrom<ScriptNode>(choices);
    }

    [Fact]
    public void Constructor_KeepsUnorderedFlag_ForShuffleableOptions() =>
        Assert.False(
            new Choices(IsOrdered: false, [Choice(Line(Text("Yes")))], SourceSpanFactory.Span())
                .IsOrdered);
}
