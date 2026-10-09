using DialogueDown.Configuration;

namespace DialogueDown.ConfigurationLoader.Tests.Support;

/// <summary>Assertions about the speakers a configuration declares.</summary>
internal static class ConfiguredSpeakerAssert
{
    /// <summary>Asserts that exactly one speaker was declared, and returns it.</summary>
    /// <param name="speakers">The speakers a configuration declared.</param>
    /// <returns>The one speaker, for a test that says more about it.</returns>
    public static ConfiguredSpeaker AssertOnlySpeaker(IReadOnlyList<ConfiguredSpeaker> speakers) =>
        Assert.Single(speakers);
}
