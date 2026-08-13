using DialogueDown.Configuration;
using static DialogueDown.Tests.Support.ConfigurationFactory;

namespace DialogueDown.Tests.Configuration;

public sealed class CompilerOptionsTests
{
    [Fact]
    public void Default_HasNoConfiguredSpeakers() =>
        Assert.Empty(CompilerOptions.Default.Speakers);

    [Fact]
    public void Default_UsesStageBoundaryMode() =>
        Assert.Equal(CompilationMode.StageBoundary, CompilerOptions.Default.Mode);

    [Fact]
    public void Mode_CanBeConfigured() =>
        Assert.Equal(
            CompilationMode.BestEffort,
            (CompilerOptions.Default with { Mode = CompilationMode.BestEffort }).Mode);

    [Fact]
    public void Speakers_AreEmptyOnAFreshInstance() =>
        Assert.Empty(new CompilerOptions().Speakers);

    [Fact]
    public void ForSemanticAnalyzer_ExposesTheConfiguredSpeakers()
    {
        var narrator = ConfiguredSpeaker(
            "Narrator", "narrator",
            customTags: [ConfiguredTag("mood", "happy")],
            reservedTags: [DefaultTag()]);
        var options = new CompilerOptions { Speakers = [narrator] };

        var configured = Assert.Single(options.ForSemanticAnalyzer().ConfiguredSpeakers);
        Assert.Equal("Narrator", configured.Name);
        Assert.Equal("narrator", configured.Id);
        Assert.Equal(ConfiguredTag("mood", "happy"), Assert.Single(configured.CustomTags));
        Assert.Equal(DefaultTag(), Assert.Single(configured.ReservedTags));
    }
}
