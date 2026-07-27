using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds one <see cref="Line"/> from a group of Markdown inlines — a paragraph, or one
/// slice of it between hard breaks. The work is done by a single-use <see cref="Assembler"/>
/// that peels an optional leading <see cref="Condition"/> guard, then an optional speaker, off
/// the front, and builds the remaining speech through the <see cref="InlineBuilder"/>. The
/// line's span covers the whole group, the condition and speaker prefix included. The group
/// must be non-empty; an empty line is dropped upstream.
/// </summary>
internal sealed class LineBuilder(SpeakerBuilder speakerBuilder, InlineBuilder inlineBuilder)
{
    public Line Build(IReadOnlyList<MarkdownInline> group, IDiagnosticSink diagnostics)
    {
        if (group.Count == 0)
        {
            throw new ArgumentException(
                "A line must be built from at least one inline.", nameof(group));
        }

        return new Assembler(speakerBuilder, inlineBuilder, group, diagnostics).Build();
    }

    /// <summary>
    /// A single-use assembler for one line. It holds the not-yet-consumed inlines in
    /// <see cref="_remaining"/> and peels the guard, then the speaker, off the front in order —
    /// each step reads and reassigns the shared remainder as it consumes the front. It is created
    /// fresh per line, so <see cref="LineBuilder"/> stays stateless.
    /// </summary>
    private sealed class Assembler(
        SpeakerBuilder speakerBuilder, InlineBuilder inlineBuilder,
        IReadOnlyList<MarkdownInline> group, IDiagnosticSink diagnostics)
    {
        private readonly SourceSpan _span = SourceSpan.Covering(group);
        private List<MarkdownInline> _remaining = [.. group];

        public Line Build()
        {
            var condition = PeelCondition();
            var speaker = PeelSpeaker();
            return new Line(speaker, inlineBuilder.Build(_remaining, diagnostics), _span, condition);
        }

        // Whether the content begins with the jump indicator `=>` (still raw text at this stage,
        // tokenized later). Such a condition guards the jump, not the line, so it is not peeled.
        private static bool PrecedesAJump(IReadOnlyList<MarkdownInline> content) =>
            content is [TextInline head, ..]
            && head.Text.StartsWith("=>", StringComparison.Ordinal);

        // A leading `"key"?` condition code span is the line's guard — but only when non-jump
        // content follows it to guard. A condition that directly precedes a jump guards the jump
        // (bound later in desugar), and a lone condition guards nothing; both are left in place.
        private Condition? PeelCondition()
        {
            if (!ConditionReader.TryPeel(_remaining, out var condition, out var remainder)
                || remainder.Count == 0 || PrecedesAJump(remainder))
            {
                return null;
            }

            _remaining = [.. remainder];
            return condition;
        }

        // Splits an optional speaker prefix off the leading text. A prefix that binds tags but
        // names no speaker reports through the speaker builder and recovers to a default speaker.
        private Speaker? PeelSpeaker()
        {
            if (_remaining[0] is not TextInline leading)
            {
                return null;
            }

            // Anchor at ContentSpan: the speaker prefix is parsed from the unescaped Text, whose
            // source position sits past any stripped leading backslash.
            var input = new ParseInput(leading.Text, leading.ContentSpan.Start);
            var result = speakerBuilder.Build(input, diagnostics);
            if (!result.Success)
            {
                return null;
            }

            RemoveSpeakerPrefix(input.Advance(result.MatchedLength));
            return result.MatchedValue;
        }

        // Drops the parsed speaker prefix from the leading text: the re-anchored leftover replaces
        // the leading text when part of it remains, otherwise the leading text is removed.
        private void RemoveSpeakerPrefix(ParseInput leftover)
        {
            if (leftover.Text.Length > 0)
            {
                _remaining[0] = new TextInline(
                    leftover.Text, new SourceSpan(leftover.Position, leftover.Text.Length));
            }
            else
            {
                _remaining.RemoveAt(0);
            }
        }
    }
}
