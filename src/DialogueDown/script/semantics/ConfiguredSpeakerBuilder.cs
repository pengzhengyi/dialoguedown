using DialogueDown.Common;
using DialogueDown.Configuration;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Bridges a configuration <see cref="ConfiguredSpeaker"/> to the AST <see cref="SpeakerDeclaration"/>
/// the speaker binder consumes — the one place that knows the declaration's shape. A configured
/// speaker has no script text, so the declaration is synthetic and carries an empty span; its
/// reserved and custom tags map straight to <see cref="ReservedTag"/>s and <see cref="CustomTag"/>s.
/// </summary>
internal static class ConfiguredSpeakerBuilder
{
    public static SpeakerDeclaration ToDeclaration(ConfiguredSpeaker speaker)
    {
        var span = SourceSpan.EmptyAt(0);

        var tags = new List<Tag>(speaker.ReservedTags.Length + speaker.CustomTags.Length);
        foreach (var reserved in speaker.ReservedTags)
        {
            tags.Add(new ReservedTag(reserved.Name, reserved.Value, span));
        }

        foreach (var custom in speaker.CustomTags)
        {
            tags.Add(new CustomTag(custom.Name, custom.Value, span));
        }

        return new SpeakerDeclaration(speaker.Name, speaker.Id, tags, span);
    }
}
