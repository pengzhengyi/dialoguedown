using System.Reflection;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group D — packaging shape. Version numbers are the one piece of assembly metadata a
/// consumer reads before anything else, and the SDK supplies a plausible default when a
/// project declares none. That default is indistinguishable from a deliberate choice at a
/// glance, so nothing but a rule catches an assembly shipping as <c>1.0.0</c>.
/// </summary>
public sealed class PackagingTests
{
    /// <remarks>
    /// The core library carries package metadata — a <c>PackageId</c>, a license, a
    /// description — so it is set up to be published, and a published <c>1.0.0</c> cannot be
    /// taken back from a package feed. Sweeping every shipped assembly rather than naming the
    /// core keeps a new project from being added without one.
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
