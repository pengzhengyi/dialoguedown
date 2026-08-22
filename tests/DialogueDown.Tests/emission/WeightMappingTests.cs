using DialogueDown.Emission;
using DialogueDown.Playbook.Weights;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Emission;

public sealed class WeightMappingTests
{
    [Fact]
    public void Write_AWrittenPercentage_KeepsIt()
    {
        var written = Assert.IsType<NumberWeight>(WeightMapping.Write(NumberWeight(25.0)));

        Assert.Equal(25.0, written.Percentage);
    }

    [Fact]
    public void Write_AnAutomaticShare_CarriesNothing()
    {
        // What the share works out to depends on the arms offered alongside it, which only a
        // runtime knows, so the playbook says "auto" rather than a number.
        Assert.IsType<AutoWeight>(WeightMapping.Write(AutoWeight()));
    }

    [Fact]
    public void Write_AShareTheGameDecides_KeepsTheKeyItAsksAbout()
    {
        var written = Assert.IsType<QueryWeight>(WeightMapping.Write(QueryWeight("Bob.Luck")));

        Assert.Equal("Bob.Luck", written.Key);
    }

    [Fact]
    public void Write_EveryWeightTheAstHas_HasASample()
    {
        MappingAssert.AssertCoversEveryMember<Ast.ChoiceWeight>(Samples());
    }

    [Fact]
    public void Write_NoWeightAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => WeightMapping.Write(null!));
    }

    private static IReadOnlyList<Ast.ChoiceWeight> Samples() =>
        [NumberWeight(25.0), AutoWeight(), QueryWeight("Bob.Luck")];
}
