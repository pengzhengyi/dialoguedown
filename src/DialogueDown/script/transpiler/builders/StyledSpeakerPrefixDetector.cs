using System.Text;
using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Transpiler.Parsers;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Reports a styled-speaker-prefix warning when a line names no speaker but its leading run,
/// once the Markdown styling is removed, would parse as a speaker prefix — for example
/// <c>*Alice*:</c>. The line builder calls this when the speaker peel fails: styling breaks the
/// plain-text run the prefix grammar needs, so the name goes unrecognized and the line is
/// silently unattributed. Only a terminating <c>:</c> outside the styling counts, so a
/// deliberately styled line such as <c>*Alice: hi*</c> does not warn.
/// </summary>
internal static class StyledSpeakerPrefixDetector
{
    public static void Report(IReadOnlyList<MarkdownInline> leading, IDiagnosticSink diagnostics)
    {
        if (WouldBePrefix(leading) is { } prefix)
        {
            diagnostics.Report(
                new Diagnostic(DiagnosticCatalog.StyledSpeakerPrefix, prefix.Span, [prefix.Text]));
        }
    }

    // The leading run flattened to plain text up to a terminating ':' that sits outside any styling,
    // reported only when a styled inline precedes that colon and the result parses as a prefix.
    private static (string Text, SourceSpan Span)? WouldBePrefix(IReadOnlyList<MarkdownInline> leading)
    {
        var flattened = new StringBuilder();
        var sawStyle = false;
        foreach (var inline in leading)
        {
            if (inline.PlainText() is not { } plain)
            {
                return null; // a code span, link, image, or break — never part of a prefix.
            }

            // Only a colon in a top-level text inline terminates the prefix; one inside styling
            // (a fully styled line) does not, so the styled run must end before the ':'.
            if (inline is TextInline text && plain.IndexOf(':') is >= 0 and var colon)
            {
                flattened.Append(plain, 0, colon + 1);
                var candidate = flattened.ToString();
                if (!sawStyle || !SpeakerPrefixProbe.BeginsWithSpeakerPrefix(candidate))
                {
                    return null;
                }

                var start = leading[0].Span.Start;
                return (candidate, new SourceSpan(start, text.ContentSpan.Start + colon + 1 - start));
            }

            sawStyle |= inline is EmphasisInline;
            flattened.Append(plain);
        }

        return null;
    }
}
