using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using static DialogueDown.Cli.Tests.Support.ConfigFiles;

namespace DialogueDown.Cli.Tests;

public sealed class ProjectConfigurationTests
{
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
        var configPath = tree.File("elsewhere/custom.toml", NarratorByDefault);

        var options = new ProjectConfiguration().Resolve(configPath, tree.Root);

        AssertOnlySpeaker(options, "Narrator");
    }

    [Fact]
    public void Resolve_DiscoversFileInStartDirectory()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorByDefault);

        var options = new ProjectConfiguration().Resolve(null, tree.Root);

        AssertOnlySpeaker(options, "Narrator");
    }

    [Fact]
    public void Resolve_WalksUpToNearestFile()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorByDefault);
        var nested = tree.Dir("act1/scene3");

        var options = new ProjectConfiguration().Resolve(null, nested);

        AssertOnlySpeaker(options, "Narrator");
    }

    [Fact]
    public void Resolve_NearestFileWins()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorByDefault);
        var nested = tree.Dir("act1");
        tree.File($"act1/{ProjectConfiguration.FileName}", """
            [[speakers]]
            name = "Alice"
            """);

        var options = new ProjectConfiguration().Resolve(null, nested);

        AssertOnlySpeaker(options, "Alice");
    }

    [Fact]
    public void Resolve_DoesNotReadConfigAboveTheBoundary()
    {
        using var tree = new TempTree();
        tree.File(ProjectConfiguration.FileName, NarratorByDefault);
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
        tree.File($"project/{ProjectConfiguration.FileName}", NarratorByDefault);
        var nested = tree.Dir("project/act1");

        var options = new ProjectConfiguration().Resolve(null, nested, boundary);

        AssertOnlySpeaker(options, "Narrator");
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
        var path = tree.File(ProjectConfiguration.FileName, NarratorByDefault);

        var applied = new ProjectConfiguration().ResolveApplied(null, tree.Root);

        AssertConfiguredFrom(applied, path, NarratorByDefault);
        AssertOnlySpeaker(applied.Options, "Narrator");
    }

    [Fact]
    public void ResolveApplied_ExplicitConfig_CarriesThatFile()
    {
        using var tree = new TempTree();
        var path = tree.File("elsewhere/custom.toml", NarratorByDefault);

        var applied = new ProjectConfiguration().ResolveApplied(path, tree.Root);

        AssertConfiguredFrom(applied, path, NarratorByDefault);
    }

    private static void AssertOnlySpeaker(CompilerOptions options, string name) =>
        Assert.Equal(name, Assert.Single(options.Speakers).Name);

    /// <summary>Asserts that the configuration was read from the file at <paramref name="path"/>.</summary>
    /// <param name="applied">The resolved configuration.</param>
    /// <param name="path">Where the file is.</param>
    /// <param name="source">What the file says.</param>
    private static void AssertConfiguredFrom(AppliedConfiguration applied, string path, string source)
    {
        Assert.True(applied.IsConfiguredFromFile);
        Assert.Equal(path, applied.File!.Path);
        Assert.Equal(source, applied.File.Source);
    }
}
