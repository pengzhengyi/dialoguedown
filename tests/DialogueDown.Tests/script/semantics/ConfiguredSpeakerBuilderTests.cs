using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.ConfigurationFactory;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class ConfiguredSpeakerBuilderTests
{
    [Fact]
    public void ToDeclaration_CarriesNameAndId()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(ConfiguredSpeaker("Alice", "A"));

        Assert.Equal("Alice", declaration.Name);
        Assert.Equal("A", declaration.Id);
        Assert.Empty(declaration.Tags);
    }

    [Fact]
    public void ToDeclaration_ReservedTags_BecomeReservedTags()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(DefaultConfiguredSpeaker("Narrator"));

        AssertReservedTag(Assert.Single(declaration.Tags), "default");
    }

    [Fact]
    public void ToDeclaration_CustomTags_BecomeCustomTags()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(
            ConfiguredSpeaker("Alice", customTags: [ConfiguredTag("mood", "happy")]));

        AssertCustomTag(Assert.Single(declaration.Tags), "mood", "happy");
    }

    [Fact]
    public void ToDeclaration_UsesAnEmptySyntheticSpan()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(ConfiguredSpeaker("Alice"));

        Assert.True(declaration.Span.IsEmpty);
    }
}
