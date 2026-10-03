using System.Text;
using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Transpiler.Parsers;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reports a styled-speaker-prefix warning when a line names no speaker but its leading text,
/// once the Markdown styling is removed, would parse as a speaker prefix — for example
/// <c>*Alice*:</c>. The line builder calls this when it finds no speaker prefix: the prefix
/// grammar reads only plain text, so a styled name goes unrecognized and the line silently
/// names no speaker. Only a terminating <c>:</c> outside the styling counts, so a deliberately
/// styled line such as <c>*Alice: hi*</c> does not warn.
/// </summary>
internal static class StyledSpeakerPrefixDetector
{
    public static void Report(IReadOnlyList<MarkdownInline> leading, IDiagnosticSink diagnostics)
    {
        if (TryScanStyledPrefix(leading, out var text, out var span)
            && SpeakerPrefixProbe.BeginsWithSpeakerPrefix(text))
        {
            diagnostics.Report(
                new Diagnostic(DiagnosticCatalog.StyledSpeakerPrefix, span, [text]));
        }
    }

    // Flattens the leading inlines to plain text up to a ':' in a top-level text inline, but only
    // when a styled inline precedes that colon — the candidate the caller then probes. Returns false
    // when a functional inline (code span, link, image, or break) comes first, when no top-level
    // colon ends the text, or when nothing is styled — a plain prefix, which the speaker prefix
    // parser already reads.
    private static bool TryScanStyledPrefix(
        IReadOnlyList<MarkdownInline> leading, out string text, out SourceSpan span)
    {
        text = string.Empty;
        span = default;

        var flattened = new StringBuilder();
        var sawStyle = false;
        foreach (var inline in leading)
        {
            if (inline.PlainText() is not { } plain)
            {
                return false; // a code span, link, image, or break — never part of a prefix.
            }

            // Only a colon in a top-level text inline terminates the prefix; one inside styling
            // (a fully styled line) does not, so the styled text must end before the ':'.
            if (inline is TextInline textInline && plain.IndexOf(':') is >= 0 and var colon)
            {
                if (!sawStyle)
                {
                    return false; // an unstyled prefix, which the speaker prefix parser reads.
                }

                flattened.Append(plain, 0, colon + 1);
                text = flattened.ToString();
                span = SourceSpan.Inclusive(
                    leading[0].Span.Start, textInline.ContentSpan.Start + colon);
                return true;
            }

            sawStyle |= inline is EmphasisInline;
            flattened.Append(plain);
        }

        return false;
    }
}
