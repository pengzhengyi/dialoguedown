using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// The "fill default speaker" rule over a single line: a line that names no speaker gets
/// the <see cref="DefaultSpeaker"/> sentinel, so every spoken line has a speaker after
/// desugaring. The sentinel has no source text of its own, so it sits at a zero-width
/// caret at the line's start. An effect-only line — a bare jump or a silent command — is a
/// <see cref="ControlLine"/> recognized before this fill, so it is never seen here and never
/// gets a speaker. A line that already names a speaker is returned unchanged.
/// </summary>
internal static class DefaultSpeakerFiller
{
    public static Line Fill(Line line) =>
        line.Speaker is null
            ? line with { Speaker = new DefaultSpeaker(SourceSpan.EmptyAt(line.Span.Start)) }
            : line;
}
