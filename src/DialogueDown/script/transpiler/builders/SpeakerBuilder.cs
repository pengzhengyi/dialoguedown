using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Recognizes and classifies a speaker prefix at the start of a line: metadata (an id
/// and/or tags) makes it a declaration, a bare name a name reference, and a bare id an
/// id reference. An <see cref="Ok"/> result carries the <see cref="Speaker"/> and how
/// much was consumed; a failure means the line has no speaker. A prefix that binds
/// metadata but names no speaker reports <see cref="DiagnosticCatalog.TagsWithoutSpeaker"/>
/// and recovers to a <see cref="DefaultSpeaker"/>, dropping the orphan tags.
/// </summary>
internal sealed class SpeakerBuilder(IParser<SpeakerPrefixData> parser, TagBuilder tagBuilder)
{
    public ParseResult<Speaker> Build(ParseInput input, IDiagnosticSink diagnostics)
    {
        var result = parser.Consume(input);
        if (result.Error is { } error)
        {
            return ParseResult<Speaker>.Fail(error);
        }

        var speaker = Classify(
            result.MatchedValue, result.MatchedRange.ToSourceSpan(), input.Text, diagnostics);
        return speaker is null
            ? ParseResult<Speaker>.Fail(new ParseError("no speaker prefix"))
            : ParseResult<Speaker>.Ok(new ParseMatch<Speaker>(speaker, result.MatchedRange));
    }

    // A written prefix always ends in a colon, so the separator is always present; the name and
    // id spans are present only when the prefix names the speaker that way.
    private static SpeakerPrefixSpans PrefixSpansOf(SpeakerPrefixData data) =>
        new(SpanOf(data.Name), SpanOf(data.Id), data.SeparatorRange.ToSourceSpan());

    private static SourceSpan? SpanOf(Spanned<string>? part) =>
        part is { } located ? located.Range.ToSourceSpan() : null;

    private Speaker? Classify(
        SpeakerPrefixData data, SourceSpan span, string content, IDiagnosticSink diagnostics)
    {
        var tagNodes = data.Tags
            .Select(tag => tagBuilder.Build(tag.Value, tag.Range.ToSourceSpan()))
            .ToList();
        var prefixSpans = PrefixSpansOf(data);

        if (data.TryGetName(out var name))
        {
            return data.Id is not null || tagNodes.Count > 0
                ? new SpeakerDeclaration(name, data.Id?.Value, tagNodes, span) { PrefixSpans = prefixSpans }
                : new SpeakerNameReference(name, span) { PrefixSpans = prefixSpans };
        }

        if (data.TryGetId(out var id))
        {
            return tagNodes.Count > 0
                ? new PartialSpeakerDeclaration(id, tagNodes, span) { PrefixSpans = prefixSpans }
                : new SpeakerIdReference(id, span) { PrefixSpans = prefixSpans };
        }

        if (tagNodes.Count > 0)
        {
            // Tags with no speaker to attach to: report and recover by dropping the tags and
            // attributing the line to the default speaker, so the rest of the line still compiles.
            // The default speaker names no one, so it carries no prefix spans.
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.TagsWithoutSpeaker, span, [content]));
            return new DefaultSpeaker(span);
        }

        return null;
    }
}
