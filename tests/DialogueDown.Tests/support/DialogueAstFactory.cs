using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds Dialogue AST nodes for tests with spans and sensible defaults filled in,
/// so a test only states the parts it cares about.
/// </summary>
internal static class DialogueAstFactory
{
    public static CustomTag CustomTag(string name, string? value = null) =>
        new(name, value, SourceSpanFactory.Span());

    public static ReservedTag ReservedTag(string name, string? value = null) =>
        new(name, value, SourceSpanFactory.Span());

    public static IReadOnlyList<Tag> Tags(params Tag[] tags) => tags;

    public static Text Text(string content) => new(content, SourceSpanFactory.Span());

    public static Jump Jump(string target, params InlineFragment[] label) =>
        new(target, label, SourceSpanFactory.Span());

    public static JumpIndicator JumpIndicator() => new(SourceSpanFactory.Span());

    public static Link Link(string target, params InlineFragment[] label) =>
        new(target, label.Length == 0 ? [Text("label")] : label, SourceSpanFactory.Span());

    public static LineBreak LineBreak() => new(SourceSpanFactory.Span());

    public static DefaultSpeaker DefaultSpeaker() => new(SourceSpanFactory.Span());

    public static Line Line(params InlineFragment[] speech) =>
        new(null, speech, SourceSpanFactory.Span());

    public static Choice Choice(params ScriptBlock[] body) =>
        new(body, SourceSpanFactory.Span());

    public static Choices Choices(params Choice[] options) =>
        Choices(isOrdered: false, options);

    public static Choices Choices(int spanStart, params Choice[] options) =>
        new(false, options, SourceSpanFactory.Span(spanStart));

    public static Choices Choices(bool isOrdered, params Choice[] options) =>
        new(isOrdered, options, SourceSpanFactory.Span());

    public static RandomOption RandomOption(ChoiceWeight weight, params ScriptBlock[] body) =>
        new(weight, body, SourceSpanFactory.Span());

    public static RandomChoices RandomChoices(params RandomOption[] options) =>
        new(options, SourceSpanFactory.Span());

    public static RandomChoices RandomChoices(int spanStart, params RandomOption[] options) =>
        new(options, SourceSpanFactory.Span(spanStart));

    public static SceneHeading SceneHeading(string title = "Scene", int level = 1) =>
        new([Text(title)], level, SourceSpanFactory.Span());

    public static SpeakerDeclaration SpeakerDeclaration(
        string name, string? id = null, params Tag[] tags) =>
        new(name, id, tags, SourceSpanFactory.Span());

    public static SpeakerDeclaration DefaultSpeakerDeclaration(string name) =>
        SpeakerDeclaration(name, tags: ReservedTag("default"));

    public static SpeakerNameReference SpeakerNameReference(string name) =>
        new(name, SourceSpanFactory.Span());

    public static SpeakerIdReference SpeakerIdReference(string id) =>
        new(id, SourceSpanFactory.Span());
}
