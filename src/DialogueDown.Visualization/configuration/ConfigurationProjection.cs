using DialogueDown.Configuration;

using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// Projects an <see cref="AppliedConfiguration"/> into the report's
/// <see cref="ConfigurationReport"/>: the file (when present) and each configured speaker with
/// its tags flattened for display — custom tags first, then reserved — so the client can color
/// reserved chips apart from custom ones.
/// </summary>
internal static class ConfigurationProjection
{
    public static ConfigurationReport Project(AppliedConfiguration applied)
    {
        ArgumentNullException.ThrowIfNull(applied);
        var speakers = applied.Options.Speakers.Select(ToView).ToList();
        // The reserved tag names come from the compiler's own closed vocabulary, so the
        // editor's autocompletion never suggests a reserved key the loader would reject.
        var reservedTags = ReservedTagNames.Known.Order().ToList();
        var mode = CompilationModes.NameOf(applied.Options.Mode);
        return new ConfigurationReport(applied.File, mode, speakers, reservedTags);
    }

    private static ConfiguredSpeakerView ToView(ConfiguredSpeaker speaker)
    {
        var tags = speaker.CustomTags.Select(tag => new TagView(tag.Name, tag.Value, false))
            .Concat(speaker.ReservedTags.Select(tag => new TagView(tag.Name, tag.Value, true)))
            .ToList();
        return new ConfiguredSpeakerView(speaker.Name, speaker.Id, tags);
    }
}
