using DialogueDown.Configuration;

namespace DialogueDown.Tests.Configuration;

public sealed class UnmodeledMarkdownNamesTests
{
    // The names are a contract with `dialogue.toml`: renaming one silently invalidates every
    // project that set it, so each is pinned rather than derived from the enum.
    [Theory]
    [InlineData(UnmodeledNodeKind.CodeBlock, "code-block")]
    [InlineData(UnmodeledNodeKind.ThematicBreak, "thematic-break")]
    [InlineData(UnmodeledNodeKind.Table, "table")]
    [InlineData(UnmodeledNodeKind.RawHtml, "raw-html")]
    [InlineData(UnmodeledNodeKind.Autolink, "autolink")]
    [InlineData(UnmodeledNodeKind.Other, "other")]
    public void NameOf_AKind_IsItsAuthorFacingName(UnmodeledNodeKind kind, string expected) =>
        Assert.Equal(expected, UnmodeledMarkdownNames.NameOf(kind));

    [Theory]
    [InlineData(UnmodeledNodeHandling.Keep, "keep")]
    [InlineData(UnmodeledNodeHandling.Ignore, "ignore")]
    public void NameOf_AHandling_IsItsAuthorFacingName(
        UnmodeledNodeHandling handling, string expected) =>
        Assert.Equal(expected, UnmodeledMarkdownNames.NameOf(handling));

    // Naming and parsing are two directions of one mapping; a name that cannot be read back is
    // a name a project could write and the loader would then reject.
    [Fact]
    public void EveryKindsNameParsesBackToThatKind() =>
        Assert.All(
            Enum.GetValues<UnmodeledNodeKind>(),
            kind => Assert.Equal(
                kind, UnmodeledMarkdownNames.TryParseKind(UnmodeledMarkdownNames.NameOf(kind))));

    [Fact]
    public void EveryHandlingsNameParsesBackToThatHandling() =>
        Assert.All(
            Enum.GetValues<UnmodeledNodeHandling>(),
            handling => Assert.Equal(
                handling,
                UnmodeledMarkdownNames.TryParseHandling(
                    UnmodeledMarkdownNames.NameOf(handling))));

    // A name that matches nothing is not an error here: the loader decides whether it is a typo to
    // report or a name from a newer version to leave alone, so the lookup answers null.
    [Fact]
    public void TryParseKind_ANameNoKindHas_IsNull() =>
        Assert.Null(UnmodeledMarkdownNames.TryParseKind("not-a-kind"));

    [Fact]
    public void TryParseHandling_ANameNoHandlingHas_IsNull() =>
        Assert.Null(UnmodeledMarkdownNames.TryParseHandling("not-a-handling"));

    [Fact]
    public void KindNamesDescription_ListsEveryKind() =>
        Assert.All(
            Enum.GetValues<UnmodeledNodeKind>(),
            kind => Assert.Contains(
                UnmodeledMarkdownNames.NameOf(kind),
                UnmodeledMarkdownNames.KindNamesDescription,
                StringComparison.Ordinal));

    [Fact]
    public void HandlingNamesDescription_ListsEveryHandling() =>
        Assert.All(
            Enum.GetValues<UnmodeledNodeHandling>(),
            handling => Assert.Contains(
                UnmodeledMarkdownNames.NameOf(handling),
                UnmodeledMarkdownNames.HandlingNamesDescription,
                StringComparison.Ordinal));
}
