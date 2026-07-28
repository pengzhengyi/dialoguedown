using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Fills the <see cref="DefaultSpeaker"/> on every speaker-less line, delegating the per-line
/// fill to <see cref="DefaultSpeakerFiller"/>.
/// </summary>
internal sealed class DefaultSpeakerRule : DesugarRule
{
    protected override Line RewriteLine(Line line) =>
        DefaultSpeakerFiller.Fill(base.RewriteLine(line));
}
