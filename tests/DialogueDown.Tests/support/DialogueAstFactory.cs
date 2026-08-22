using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds Dialogue AST nodes for tests with spans and sensible defaults filled in,
/// so a test only states the parts it cares about.
/// </summary>
internal static class DialogueAstFactory
{
    public static ScriptDocument Document(params ScriptBlock[] body) => new(body);

    public static CustomTag CustomTag(string name, string? value = null) =>
        new(name, value, SourceSpanFactory.Span());

    public static ReservedTag ReservedTag(string name, string? value = null) =>
        new(name, value, SourceSpanFactory.Span());

    public static IReadOnlyList<Tag> Tags(params Tag[] tags) => tags;

    public static Text Text(string content) => new(content, SourceSpanFactory.Span());

    public static StyledText StyledText(SpeechStyle style, params InlineFragment[] children) =>
        new(style, children.Length == 0 ? [Text("styled")] : children, SourceSpanFactory.Span());

    public static Image Image(string source, params InlineFragment[] alt) =>
        new(source, alt.Length == 0 ? [Text("alt")] : alt, SourceSpanFactory.Span());

    public static Query Query(string key) => new(key, SourceSpanFactory.Span());

    public static DefaultCommand DefaultCommand(string action) =>
        new(action, SourceSpanFactory.Span());

    public static CustomCommand CustomCommand(string name, params string[] args) =>
        new(name, args, SourceSpanFactory.Span());

    public static Jump Jump(string target, params InlineFragment[] label) =>
        new(target, label, SourceSpanFactory.Span());

    public static JumpIndicator JumpIndicator() => new(SourceSpanFactory.Span());

    public static Condition Condition(string key) => new(key, SourceSpanFactory.Span());

    public static Link Link(string target, params InlineFragment[] label) =>
        new(target, label.Length == 0 ? [Text("label")] : label, SourceSpanFactory.Span());

    public static LineBreak LineBreak() => new(SourceSpanFactory.Span());

    public static DefaultSpeaker DefaultSpeaker() => new(SourceSpanFactory.Span());

    public static Line Line(params InlineFragment[] speech) =>
        new(null, speech, SourceSpanFactory.Span());

    public static Line ConditionalLine(Condition condition, params InlineFragment[] speech) =>
        new(null, speech, SourceSpanFactory.Span(), condition);

    public static ControlLine ControlLine(params InlineFragment[] effects) =>
        new(effects, SourceSpanFactory.Span());

    public static ControlLine ConditionalControlLine(Condition condition, params InlineFragment[] effects) =>
        new(effects, SourceSpanFactory.Span(), condition);

    public static Choice Choice(params ScriptBlock[] body) =>
        new(body, SourceSpanFactory.Span());

    public static Choice ConditionalChoice(Condition condition, params ScriptBlock[] body) =>
        new(body, SourceSpanFactory.Span(), condition);

    public static Choices Choices(params Choice[] options) =>
        Choices(isOrdered: false, options);

    public static Choices Choices(int spanStart, params Choice[] options) =>
        new(false, options, SourceSpanFactory.Span(spanStart));

    public static Choices Choices(bool isOrdered, params Choice[] options) =>
        new(isOrdered, options, SourceSpanFactory.Span());

    public static NumberWeight NumberWeight(double percentage) =>
        new(percentage, SourceSpanFactory.Span());

    public static AutoWeight AutoWeight() => new(SourceSpanFactory.Span());

    public static QueryWeight QueryWeight(string key) =>
        new(key, SourceSpanFactory.Span());

    public static RandomOption RandomOption(ChoiceWeight weight, params ScriptBlock[] body) =>
        new(weight, body, SourceSpanFactory.Span());

    public static RandomOption ConditionalRandomOption(
        Condition condition, ChoiceWeight weight, params ScriptBlock[] body) =>
        new(weight, body, SourceSpanFactory.Span(), condition);

    public static RandomChoices RandomChoices(params RandomOption[] options) =>
        new(options, SourceSpanFactory.Span());

    public static RandomChoices RandomChoices(int spanStart, params RandomOption[] options) =>
        new(options, SourceSpanFactory.Span(spanStart));

    public static Branch Branch(Condition condition, params ScriptBlock[] body) =>
        new(condition, body, SourceSpanFactory.Span());

    public static Branch ElseBranch(params ScriptBlock[] body) =>
        new(null, body, SourceSpanFactory.Span());

    public static ControlBlock ControlBlock(params Branch[] branches) =>
        new(branches, SourceSpanFactory.Span());

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
