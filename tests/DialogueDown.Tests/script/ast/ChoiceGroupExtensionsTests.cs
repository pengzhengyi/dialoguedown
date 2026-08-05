using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ChoiceGroupExtensionsTests
{
    [Fact]
    public void OptionBodies_PlayerChoices_YieldsEachOptionsBodyInOrder()
    {
        var first = Line(Text("left"));
        var second = Line(Text("right"));

        var bodies = Choices(Choice(first), Choice(second)).OptionBodies();

        Assert.Equal([first], bodies[0]);
        Assert.Equal([second], bodies[1]);
    }

    [Fact]
    public void OptionBodies_RandomChoices_ReadsTheSameShape()
    {
        var only = Line(Text("heads"));

        var bodies = RandomChoices(RandomOption(AutoWeight(), only)).OptionBodies();

        Assert.Equal([only], Assert.Single(bodies));
    }
}
