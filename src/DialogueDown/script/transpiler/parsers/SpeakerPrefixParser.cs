using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler.Parsers;

/// <summary>
/// The grammar that recognizes a speaker prefix at the start of a line and reports
/// its parts as <see cref="SpeakerPrefixData"/> — an optional name, an optional id,
/// and any tags. Name, id, and tags must be separated by whitespace, so glued forms
/// like <c>@A#tag</c> or <c>#a#b</c> are not a prefix. It only recognizes shape; a
/// separate builder classifies and validates. A failed parse means the text is not
/// a speaker prefix.
/// </summary>
internal static class SpeakerPrefixParser
{
    private static readonly SpeakerPrefixData _empty = new(null, null, []);

    private static readonly IParser<Spanned<string>> _name = SuperpowerParser.Wrap(
        SuperpowerPrimitives.QuotedString.Try()
            .Or(Identifier.CStyle.Select(name => name.ToStringValue()))).Located();

    private static readonly IParser<Spanned<string>> _id = SuperpowerParser.Wrap(
        Character.EqualTo('@').IgnoreThen(Identifier.CStyle.Select(id => id.ToStringValue())))
        .Located();

    private static readonly IParser<char[]> _optionalWhitespace =
        SuperpowerParser.Wrap(Character.WhiteSpace.Many());

    private static readonly IParser<char[]> _requiredWhitespace =
        SuperpowerParser.Wrap(Character.WhiteSpace.AtLeastOnce());

    private static readonly IParser<char> _colon =
        SuperpowerParser.Wrap(Character.EqualTo(':'));

    // Each subsequent id or tag must be preceded by whitespace; the first element
    // (handled per branch below) carries no such requirement.
    private static readonly IParser<Spanned<string>> _spacedId =
        from _ in _requiredWhitespace
        from id in _id
        select id;

    private static readonly IParser<Spanned<TagData>> _spacedTag =
        from _ in _requiredWhitespace
        from tag in TagParser.Token.Located()
        select tag;

    private static readonly IParser<SpeakerPrefixData> _nameFirst =
        from name in _name
        from id in _spacedId.OptionalValue()
        from tags in _spacedTag.Repeated()
        select new SpeakerPrefixData(name, id, tags);

    private static readonly IParser<SpeakerPrefixData> _idFirst =
        from id in _id
        from tags in _spacedTag.Repeated()
        select new SpeakerPrefixData(null, id, tags);

    private static readonly IParser<SpeakerPrefixData> _tagsFirst =
        from first in TagParser.Token.Located()
        from rest in _spacedTag.Repeated()
        select new SpeakerPrefixData(null, null, Prepend(first, rest));

    private static readonly IParser<SpeakerPrefixData> _body =
        _nameFirst.Or(_idFirst).Or(_tagsFirst).OptionalOrDefault(_empty);

    // The match extends past the colon and consumes all whitespace after it, so
    // MatchedLength lands at the speech start. Post-colon whitespace is insignificant
    // regardless of amount; a leading space in speech must be quoted (see the DSL spec).
    // The colon's own range is kept for the separator sub-span.
    public static IParser<SpeakerPrefixData> Prefix { get; } =
        from _lead in _optionalWhitespace
        from data in _body
        from _trail in _optionalWhitespace
        from colon in _colon.Located()
        from _afterColon in _optionalWhitespace
        select data with { SeparatorRange = colon.Range };

    private static IReadOnlyList<Spanned<TagData>> Prepend(
        Spanned<TagData> first, IReadOnlyList<Spanned<TagData>> rest)
    {
        var tags = new List<Spanned<TagData>>(rest.Count + 1) { first };
        tags.AddRange(rest);
        return tags;
    }
}
