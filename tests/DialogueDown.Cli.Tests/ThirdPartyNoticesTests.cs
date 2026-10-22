using System.Text.Json;

namespace DialogueDown.Cli.Tests;

public sealed class ThirdPartyNoticesTests
{
    [Fact]
    public void BundledAssemblies_ListsThePackagesTheToolShips()
    {
        var bundled = BundledAssemblies();

        Assert.Contains("Markdig.dll", bundled);
        Assert.Contains("Spectre.Console.Cli.dll", bundled);
        Assert.DoesNotContain("DialogueDown.dll", bundled);
    }

    [Fact]
    public void NoticesFile_NamesEveryAssemblyTheToolBundles()
    {
        var notices = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md"));

        var unlisted = BundledAssemblies()
            .Where(assembly => !notices.Contains($"`{assembly}`", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            unlisted.Count == 0,
            $"THIRD-PARTY-NOTICES.md has no entry for: {string.Join(", ", unlisted)}");
    }

    /// <summary>
    /// The file names of the NuGet package assemblies the tool bundles: those its own dependency
    /// manifest lists that also sit beside it. A build-only package such as SourceLink is in the
    /// manifest but ships no assembly, so it drops out. DialogueDown's assemblies are project
    /// references, not packages, so they are not third-party code.
    /// </summary>
    private static List<string> BundledAssemblies()
    {
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DialogueDown.Cli.deps.json")));
        var libraries = manifest.RootElement.GetProperty("libraries");

        return manifest.RootElement
            .GetProperty("targets")
            .EnumerateObject()
            .SelectMany(target => target.Value.EnumerateObject())
            .Where(entry => libraries.GetProperty(entry.Name).GetProperty("type").GetString() == "package")
            .Where(entry => entry.Value.TryGetProperty("runtime", out _))
            .SelectMany(entry => entry.Value.GetProperty("runtime").EnumerateObject())
            .Select(runtimeAsset => Path.GetFileName(runtimeAsset.Name))
            .Where(assembly => File.Exists(Path.Combine(AppContext.BaseDirectory, assembly)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
