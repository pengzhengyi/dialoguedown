using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Recognizes a speaker-less, effect-only line — a bare jump or one or more silent commands — as a
/// <see cref="ControlLine"/>, so an effect is modeled as control, not speech, and never gets a
/// speaker. A line that names a speaker or carries prose stays a spoken <see cref="Line"/>. It runs
/// after jumps are assembled, so "effect-only" is decidable, and before the default-speaker fill,
/// so a control line is never given a speaker.
/// </summary>
internal sealed class ControlLineRecognitionRule : DesugarRule
{
    protected override ScriptBlock RewriteBlock(ScriptBlock block)
    {
        var rewritten = base.RewriteBlock(block);
        return rewritten is Line line && line.Speaker is null && IsEffectOnly(line.Speech)
            ? new ControlLine(line.Speech, line.Span, line.Condition)
            : rewritten;
    }

    // Effect-only means at least one effect (a jump or a command) and no spoken content — a query
    // reads state into speech, so it counts as spoken. Blank text and line breaks are padding.
    private static bool IsEffectOnly(IReadOnlyList<InlineFragment> speech)
    {
        var hasEffect = false;
        foreach (var fragment in speech)
        {
            switch (fragment)
            {
                case Jump or DefaultCommand or CustomCommand:
                    hasEffect = true;
                    break;
                case LineBreak:
                    break;
                case Text text when string.IsNullOrWhiteSpace(text.Content):
                    break;
                default:
                    return false;
            }
        }

        return hasEffect;
    }
}
