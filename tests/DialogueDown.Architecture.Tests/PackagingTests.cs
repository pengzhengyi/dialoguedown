using System.Reflection;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Packaging shape. The SDK gives a project that declares no version the default
/// <c>1.0.0</c>, which looks like a deliberate choice, so a rule checks every shipped
/// assembly for the release version.
/// </summary>
public sealed class PackagingTests
{
    /// <remarks>
    /// Every shipped assembly is checked, not only the published core library, so a new
    /// project cannot be added without the release version.
    /// </remarks>
    [Fact]
    public void EveryShippedAssembly_CarriesTheReleaseVersion()
    {
        var release = ReleaseVersion(Architecture.CliAssembly);

        var disagreeing = Architecture.AllAssemblies
            .Select(assembly => (Name: assembly.GetName().Name!, Version: ReleaseVersion(assembly)))
            .Where(assembly => assembly.Version != release)
            .Select(assembly => $"{assembly.Name} is {assembly.Version}")
            .ToList();

        Assert.NotEqual("1.0.0", release);
        Assert.True(
            disagreeing.Count == 0,
            $"Every shipped assembly should say {release}, but: {string.Join("; ", disagreeing)}.");
    }

    // The informational version, without any build metadata a "+<commit>" suffix adds.
    private static string ReleaseVersion(Assembly assembly)
    {
        var version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? version[..plus] : version;
    }
}
