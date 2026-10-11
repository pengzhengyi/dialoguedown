using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Tests.Configuration;

public sealed class AppliedConfigurationTests
{
    [Fact]
    public void FromFile_CarriesTheFileAndReadsAsConfiguredFromFile()
    {
        var options = new CompilerOptions { Speakers = [new ConfiguredSpeaker("Alice", "A", [], [])] };

        var applied = AppliedConfiguration.FromFile("/proj/dialogue.toml", "[[speakers]]", options);

        Assert.True(applied.IsConfiguredFromFile);
        Assert.False(applied.UsesDefaultConfiguration);
        Assert.NotNull(applied.File);
        Assert.Equal("/proj/dialogue.toml", applied.File.Path);
        Assert.Equal("[[speakers]]", applied.File.Source);
        Assert.Same(options, applied.Options);
    }

    [Fact]
    public void WithoutFile_HasNoFileAndReadsAsUsingDefaults()
    {
        var applied = AppliedConfiguration.WithoutFile(CompilerOptions.Default);

        Assert.False(applied.IsConfiguredFromFile);
        Assert.True(applied.UsesDefaultConfiguration);
        Assert.Null(applied.File);
        Assert.Same(CompilerOptions.Default, applied.Options);
    }

    [Fact]
    public void FromFile_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => AppliedConfiguration.FromFile(null!, "x", CompilerOptions.Default));
        Assert.Throws<ArgumentNullException>(
            () => AppliedConfiguration.FromFile("/p", null!, CompilerOptions.Default));
        Assert.Throws<ArgumentNullException>(
            () => AppliedConfiguration.FromFile("/p", "x", null!));
    }

    [Fact]
    public void WithoutFile_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AppliedConfiguration.WithoutFile(null!));
    }
}
