using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for Dialogue AST shapes, so transpiler tests read at the
/// level of the dialogue domain instead of repeating type checks.
/// </summary>
internal static class DialogueAstAssert
{
    public static Text AssertText(InlineFragment actual, string content)
    {
        var text = Assert.IsType<Text>(actual);
        Assert.Equal(content, text.Content);
        return text;
    }

    public static Text AssertSingleText(IReadOnlyList<InlineFragment> fragments, string content) =>
        AssertText(Assert.Single(fragments), content);

    public static StyledText AssertStyledText(InlineFragment actual, SpeechStyle style)
    {
        var styled = AssertStyledText(actual);
        Assert.Equal(style, styled.Style);
        return styled;
    }

    public static StyledText AssertStyledText(InlineFragment actual) =>
        Assert.IsType<StyledText>(actual);

    public static Image AssertImage(InlineFragment actual, string source)
    {
        var image = Assert.IsType<Image>(actual);
        Assert.Equal(source, image.Source);
        return image;
    }

    public static Link AssertLink(InlineFragment actual, string target)
    {
        var link = Assert.IsType<Link>(actual);
        Assert.Equal(target, link.Target);
        return link;
    }

    public static Jump AssertJump(InlineFragment actual, string target)
    {
        var jump = Assert.IsType<Jump>(actual);
        Assert.Equal(target, jump.Target);
        return jump;
    }

    public static Query AssertQuery(InlineFragment actual, string key)
    {
        var query = Assert.IsType<Query>(actual);
        Assert.Equal(key, query.Key);
        return query;
    }

    public static JumpIndicator AssertJumpIndicator(InlineFragment actual) =>
        Assert.IsType<JumpIndicator>(actual);

    public static LineBreak AssertLineBreak(InlineFragment actual) =>
        Assert.IsType<LineBreak>(actual);

    public static CustomTag AssertCustomTag(InlineFragment actual, string name, string? value = null) =>
        AssertCustomTag(Assert.IsAssignableFrom<Tag>(actual), name, value);

    public static ReservedTag AssertReservedTag(InlineFragment actual, string name, string? value = null) =>
        AssertReservedTag(Assert.IsAssignableFrom<Tag>(actual), name, value);

    public static CustomTag AssertCustomTag(Tag actual, string name, string? value = null) =>
        AssertTag<CustomTag>(actual, name, value);

    public static ReservedTag AssertReservedTag(Tag actual, string name, string? value = null) =>
        AssertTag<ReservedTag>(actual, name, value);

    public static SpeakerDeclaration AssertSpeakerDeclaration(
        Speaker actual, string name, string? id = null, params Tag[] tags)
    {
        var declaration = Assert.IsType<SpeakerDeclaration>(actual);
        Assert.Equal(name, declaration.Name);
        if (id is null)
        {
            Assert.Null(declaration.Id);
        }
        else
        {
            Assert.Equal(id, declaration.Id);
        }

        AssertTags(declaration.Tags, tags);
        return declaration;
    }

    public static PartialSpeakerDeclaration AssertPartialSpeakerDeclaration(
        Speaker actual, string id, params Tag[] tags)
    {
        var partial = Assert.IsType<PartialSpeakerDeclaration>(actual);
        Assert.Equal(id, partial.Id);
        AssertTags(partial.Tags, tags);
        return partial;
    }

    public static SpeakerNameReference AssertSpeakerNameReference(Speaker actual, string name)
    {
        var reference = Assert.IsType<SpeakerNameReference>(actual);
        Assert.Equal(name, reference.Name);
        return reference;
    }

    public static SpeakerIdReference AssertSpeakerIdReference(Speaker actual, string id)
    {
        var reference = Assert.IsType<SpeakerIdReference>(actual);
        Assert.Equal(id, reference.Id);
        return reference;
    }

    public static DefaultSpeaker AssertDefaultSpeaker(Speaker? actual) =>
        Assert.IsType<DefaultSpeaker>(actual);

    public static SceneHeading AssertSceneHeading(ScriptBlock actual, string title, int level)
    {
        var scene = Assert.IsType<SceneHeading>(actual);
        Assert.Equal(level, scene.Level);
        AssertText(Assert.Single(scene.Title), title);
        return scene;
    }

    public static Line AssertLine(ScriptBlock actual) => Assert.IsType<Line>(actual);

    public static Choices AssertChoices(ScriptBlock actual, bool isOrdered)
    {
        var choices = Assert.IsType<Choices>(actual);
        Assert.Equal(isOrdered, choices.IsOrdered);
        return choices;
    }

    public static RandomChoices AssertRandomChoices(ScriptBlock actual) =>
        Assert.IsType<RandomChoices>(actual);

    public static Line AssertChoiceLine(Choice choice) => AssertLine(Assert.Single(choice.Body));

    public static Line AssertRandomOptionLine(RandomOption option) =>
        AssertLine(Assert.Single(option.Body));

    public static Text AssertSpeechText(Line line, string content) =>
        AssertText(Assert.Single(line.Speech), content);

    private static void AssertTags(IReadOnlyList<Tag> actual, Tag[] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(actual);
            return;
        }

        // Compare by kind, name, and value — not span, which depends on the tag's
        // position in the source and is asserted separately.
        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].GetType(), actual[i].GetType());
            Assert.Equal(expected[i].Name, actual[i].Name);
            Assert.Equal(expected[i].Value, actual[i].Value);
        }
    }

    private static TTag AssertTag<TTag>(Tag actual, string name, string? value)
        where TTag : Tag
    {
        var tag = Assert.IsType<TTag>(actual);
        Assert.Equal(name, tag.Name);
        if (value is null)
        {
            Assert.Null(tag.Value);
        }
        else
        {
            Assert.Equal(value, tag.Value);
        }

        return tag;
    }
}
