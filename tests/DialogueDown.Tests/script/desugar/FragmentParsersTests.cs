using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using Pidgin;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class FragmentParsersTests
{
    [Fact]
    public void OfType_MatchesAFragmentOfThatKind_AndNarrowsIt()
    {
        var condition = Condition("Rainy");

        var result = FragmentParsers.OfType<Condition>().Parse([condition]);

        Assert.True(result.Success);
        Assert.Same(condition, result.Value);
    }

    [Fact]
    public void OfType_DoesNotMatchAFragmentOfAnotherKind()
    {
        var result = FragmentParsers.OfType<Condition>().Parse([Text("hi")]);

        Assert.False(result.Success);
    }
}
