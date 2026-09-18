using DialogueDown.Configuration;
using DialogueDown.TestSupport;

namespace DialogueDown.Cli.Tests;

public sealed class ProjectConfigurationTests
{
    private const string NarratorConfig = """
        [[speakers]]
        name = "Narrator"
        default = true
        """;

    [Fact]
    public void Resolve_NoConfigAndNoFile_ReturnsDefault()
    {
        using var tree = new TempTree();

        var options = new ProjectConfiguration().Resolve(null, tree.Root);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Resolve_ExplicitConfig_LoadsThatFile()
    {
        using var tree = new TempTree();
        var configPath = tree.File("elsewhere/custom.toml", NarratorConfig);

        var options = new ProjectConfiguration().Resolve(configPath, tree.Root);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_DiscoversFileInStartDirectory()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorConfig);

        var options = new ProjectConfiguration().Resolve(null, tree.Root);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_WalksUpToNearestFile()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorConfig);
        var nested = tree.Dir("act1/scene3");

        var options = new ProjectConfiguration().Resolve(null, nested);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_NearestFileWins()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorConfig);
        var nested = tree.Dir("act1");
        tree.File($"act1/{ProjectConfiguration.FileName}", """
            [[speakers]]
            name = "Alice"
            """);

        var options = new ProjectConfiguration().Resolve(null, nested);

        Assert.Equal("Alice", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_DoesNotReadConfigAboveTheBoundary()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorConfig);
        var boundary = tree.Dir("project");
        var nested = tree.Dir("project/act1");

        var options = new ProjectConfiguration().Resolve(null, nested, boundary);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Resolve_ReadsConfigAtTheBoundary()
    {
        using var tree = new TempTree();
        var boundary = tree.Dir("project");
        tree.File($"project/{ProjectConfiguration.FileName}", NarratorConfig);
        var nested = tree.Dir("project/act1");

        var options = new ProjectConfiguration().Resolve(null, nested, boundary);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void ResolveApplied_NoFile_UsesDefaultsWithNoFile()
    {
        using var tree = new TempTree();

        var applied = new ProjectConfiguration().ResolveApplied(null, tree.Root);

        Assert.True(applied.UsesDefaultConfiguration);
        Assert.Null(applied.File);
        Assert.Same(CompilerOptions.Default, applied.Options);
    }

    [Fact]
    public void ResolveApplied_DiscoveredFile_CarriesItsPathTextAndOptions()
    {
        using var tree = new TempTree();
        var path = tree.File(ProjectConfiguration.FileName, NarratorConfig);

        var applied = new ProjectConfiguration().ResolveApplied(null, tree.Root);

        Assert.True(applied.IsConfiguredFromFile);
        Assert.Equal(path, applied.File!.Path);
        Assert.Equal(NarratorConfig, applied.File.Source);
        Assert.Equal("Narrator", Assert.Single(applied.Options.Speakers).Name);
    }

    [Fact]
    public void ResolveApplied_ExplicitConfig_CarriesThatFile()
    {
        using var tree = new TempTree();
        var path = tree.File("elsewhere/custom.toml", NarratorConfig);

        var applied = new ProjectConfiguration().ResolveApplied(path, tree.Root);

        Assert.True(applied.IsConfiguredFromFile);
        Assert.Equal(path, applied.File!.Path);
        Assert.Equal(NarratorConfig, applied.File.Source);
    }
}
