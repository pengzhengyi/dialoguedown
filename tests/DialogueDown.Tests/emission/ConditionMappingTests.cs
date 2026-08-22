using DialogueDown.Emission;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Emission;

public sealed class ConditionMappingTests
{
    [Fact]
    public void Write_ACondition_KeepsTheKeyItAsksAbout()
    {
        var written = Assert.IsType<KeyCondition>(ConditionMapping.Write(Condition("IsCurious")));

        Assert.Equal("IsCurious", written.Key);
    }

    [Fact]
    public void Write_NothingToAskAbout_StaysNothing()
    {
        // An unconditional line, edge, or option has no condition, and the format says so by
        // leaving the field out rather than by writing an always-true one.
        Assert.Null(ConditionMapping.Write(null));
    }

    [Fact]
    public void Write_EveryConditionTheAstHas_HasASample()
    {
        MappingAssert.AssertCoversEveryMember<Ast.Condition>([Condition("IsCurious")]);
    }
}
