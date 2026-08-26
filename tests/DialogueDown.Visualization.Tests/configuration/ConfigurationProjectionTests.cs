using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Tests.Configuration;

public sealed class ConfigurationProjectionTests
{
    [Fact]
    public void Project_CarriesTheCompilersReservedTagNames()
    {
        var report = ConfigurationProjection.Project(
            AppliedConfiguration.WithoutFile(CompilerOptions.Default));

        Assert.Equal(ReservedTagNames.Known.Order(), report.ReservedTags);
        Assert.Contains(ReservedTagNames.Default, report.ReservedTags);
    }

    [Fact]
    public void Project_CarriesTheConfiguredModeName()
    {
        var applied = AppliedConfiguration.WithoutFile(
            CompilerOptions.Default with { Mode = CompilationMode.BestEffort });

        var report = ConfigurationProjection.Project(applied);

        Assert.Equal("best-effort", report.Mode);
    }

    [Fact]
    public void Project_DefaultOptions_CarriesTheStageBoundaryModeName()
    {
        var report = ConfigurationProjection.Project(
            AppliedConfiguration.WithoutFile(CompilerOptions.Default));

        Assert.Equal("stage-boundary", report.Mode);
    }

    [Fact]
    public void Project_NoFile_YieldsNullFileAndNoSpeakers()
    {
        var report = ConfigurationProjection.Project(
            AppliedConfiguration.WithoutFile(CompilerOptions.Default));

        Assert.Null(report.File);
        Assert.Empty(report.Speakers);
    }

    [Fact]
    public void Project_WithFile_CarriesTheFile()
    {
        var applied = AppliedConfiguration.FromFile(
            "/proj/dialogue.toml", "[[speakers]]", CompilerOptions.Default);

        var report = ConfigurationProjection.Project(applied);

        Assert.NotNull(report.File);
        Assert.Equal("/proj/dialogue.toml", report.File!.Path);
        Assert.Equal("[[speakers]]", report.File.Source);
    }

    [Fact]
    public void Project_Speaker_FlattensCustomThenReservedTagsWithFlags()
    {
        var speaker = new ConfiguredSpeaker(
            "Alice",
            "A",
            customTags: [new ConfiguredTag("role", "guide"), new ConfiguredTag("main")],
            reservedTags: [new ConfiguredTag("default")]);
        var applied = AppliedConfiguration.FromFile(
            "/p", "x", new CompilerOptions { Speakers = [speaker] });

        var view = Assert.Single(ConfigurationProjection.Project(applied).Speakers);

        Assert.Equal("Alice", view.Name);
        Assert.Equal("A", view.Id);
        Assert.Collection(
            view.Tags,
            tag => AssertTag(tag, "role", "guide", reserved: false),
            tag => AssertTag(tag, "main", null, reserved: false),
            tag => AssertTag(tag, "default", null, reserved: true));
    }

    private static void AssertTag(TagView tag, string name, string? value, bool reserved)
    {
        Assert.Equal(name, tag.Name);
        Assert.Equal(value, tag.Value);
        Assert.Equal(reserved, tag.Reserved);
    }
}
