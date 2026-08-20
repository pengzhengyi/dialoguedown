using DialogueDown.Emission;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.SpeechAssert;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Emission;

public sealed class EffectMappingTests
{
    [Fact]
    public void Write_AQuery_KeepsItsKey()
    {
        AssertQueries(EffectMapping.Write(Query("Bob.Affection")), "Bob.Affection");
    }

    [Fact]
    public void Write_ADefaultCommand_KeepsItsAction()
    {
        AssertCommands(EffectMapping.Write(DefaultCommand("wait")), "wait");
    }

    [Fact]
    public void Write_ACustomCommand_KeepsItsNameAndArguments()
    {
        AssertCalls(EffectMapping.Write(CustomCommand("shake", "3", "hard")), "shake", "3", "hard");
    }

    [Fact]
    public void Write_ACustomCommandWithNoArguments_CarriesNone()
    {
        AssertCalls(EffectMapping.Write(CustomCommand("blink")), "blink");
    }

    [Fact]
    public void Write_SeveralEffects_KeepsTheOrderTheyRunIn()
    {
        var effects = EffectMapping.Write([DefaultCommand("wait"), Query("Key")]);

        Assert.Collection(
            effects,
            first => AssertCommands(first, "wait"),
            second => AssertQueries(second, "Key"));
    }

    [Fact]
    public void Write_EveryCallTheAstCanMake_HasASample()
    {
        MappingAssert.AssertCoversEveryMember<Ast.GameCall>(Samples());
    }

    [Fact]
    public void Write_NoCallAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EffectMapping.Write((Ast.GameCall)null!));
    }

    [Fact]
    public void Write_NoListAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => EffectMapping.Write((IReadOnlyList<Ast.GameCall>)null!));
    }

    private static IReadOnlyList<Ast.GameCall> Samples() =>
        [Query("Key"), DefaultCommand("wait"), CustomCommand("shake")];
}
