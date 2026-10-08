using DialogueDown.Configuration;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds the public configuration option types, so a test builds a configured
/// speaker or tag through one place and a constructor change touches only this file.
/// </summary>
internal static class ConfigurationFactory
{
    public static ConfiguredTag ConfiguredTag(string name, string? value = null) => new(name, value);

    /// <summary>The reserved <c>##default</c> tag that marks a configured speaker as the default.</summary>
    public static ConfiguredTag DefaultTag() => ConfiguredTag("default");

    public static ConfiguredSpeaker ConfiguredSpeaker(
        string name,
        string? id = null,
        IReadOnlyList<ConfiguredTag>? customTags = null,
        IReadOnlyList<ConfiguredTag>? reservedTags = null) =>
        new(name, id, customTags ?? [], reservedTags ?? []);

    /// <summary>A configured speaker marked default: a bare name plus the reserved default tag.</summary>
    public static ConfiguredSpeaker DefaultConfiguredSpeaker(string name = "Narrator") =>
        ConfiguredSpeaker(name, reservedTags: [DefaultTag()]);
}
