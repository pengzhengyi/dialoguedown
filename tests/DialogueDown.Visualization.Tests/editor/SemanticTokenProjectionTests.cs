using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Lsp;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Editor;

public sealed class SemanticTokenProjectionTests
{
    private readonly SemanticTokenProjection _projection = new();

    [Fact]
    public void Project_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _projection.Project(null!, "x"));

    [Fact]
    public void Project_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _projection.Project(Pipeline.Document(""), null!));

    [Fact]
    public void Project_EmptyDocument_HasNoTokens() =>
        Assert.Empty(Project(""));

    [Fact]
    public void Project_ProseLine_HasNoDialogueTokens() =>
        // A speaker-less line is plain prose; Markdown highlighting owns it.
        Assert.Empty(Project("Just some narration with no speaker."));

    [Fact]
    public void Project_SpeakerName_ProjectsANameAndSeparatorButNoId()
    {
        var source = "Alice: Hello there.";
        var tokens = Project(source);

        AssertToken(tokens, TokenKind.SpeakerName, "Alice", source);
        AssertToken(tokens, TokenKind.Separator, ":", source);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKind.SpeakerId);
    }

    [Fact]
    public void Project_SpeakerId_ProjectsAnIdIncludingTheAtAndASeparatorButNoName()
    {
        var source = "@alice: Hello there.";
        var tokens = Project(source);

        AssertToken(tokens, TokenKind.SpeakerId, "@alice", source);
        AssertToken(tokens, TokenKind.Separator, ":", source);
        Assert.DoesNotContain(tokens, token => token.Kind == TokenKind.SpeakerName);
    }

    [Fact]
    public void Project_SpeakerNameAndId_ProjectsNameIdAndSeparatorSeparately()
    {
        var source = "Alice @alice: Hello there.";
        var tokens = Project(source);

        AssertToken(tokens, TokenKind.SpeakerName, "Alice", source);
        AssertToken(tokens, TokenKind.SpeakerId, "@alice", source);
        AssertToken(tokens, TokenKind.Separator, ":", source);
    }

    [Fact]
    public void Project_SpeakerWithTag_ProjectsDisjointNameIdTagAndSeparatorTokens()
    {
        var source = "Alice @alice #happy: Hello there.";
        var tokens = Project(source);

        // Precise tokens are non-overlapping: the tag sits between the id and the colon, and
        // no speaker token covers it (unlike the retired coarse Speaker token).
        AssertToken(tokens, TokenKind.SpeakerName, "Alice", source);
        AssertToken(tokens, TokenKind.SpeakerId, "@alice", source);
        AssertToken(tokens, TokenKind.CustomTag, "#happy", source);
        AssertToken(tokens, TokenKind.Separator, ":", source);
        AssertTokensDoNotOverlap(tokens);
    }

    [Fact]
    public void Project_QuotedSpeakerName_SpanIncludesTheQuotes()
    {
        var source = "\"Dr. Vale\": Hello there.";
        var tokens = Project(source);

        AssertToken(tokens, TokenKind.SpeakerName, "\"Dr. Vale\"", source);
        AssertToken(tokens, TokenKind.Separator, ":", source);
    }

    [Fact]
    public void Project_OrphanTagWithNoSpeaker_HasNoSpeakerTokens()
    {
        // A prefix of only tags names no speaker; it recovers to a default that carries no
        // prefix spans, so no name, id, or separator token is projected.
        var tokens = Project("#lonely: Hello there.");

        Assert.DoesNotContain(tokens, token =>
            token.Kind is TokenKind.SpeakerName or TokenKind.SpeakerId or TokenKind.Separator);
    }

    [Fact]
    public void Project_CustomTag_TokenIncludesTheHash()
    {
        var source = "@alice #happy: Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.CustomTag);
        Assert.Equal("#happy", token.TextIn(source));
    }

    [Fact]
    public void Project_ReservedTag_TokenIncludesTheDoubleHash()
    {
        var source = "@alice ##narrator: Hello there.";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.ReservedTag);
        Assert.Equal("##narrator", token.TextIn(source));
    }

    [Fact]
    public void Project_JumpIndicator_TokenCoversTheArrow()
    {
        var source = "Alice: Onward => [next](#next)";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.JumpIndicator);
        Assert.Equal("=>", token.TextIn(source));
    }

    [Fact]
    public void Project_TokenRangeIsZeroBased()
    {
        // "Bob #wow: Hi." — the custom tag "#wow" starts at column 4 on the first line.
        var token = AssertSingleSemanticToken(Project("Bob #wow: Hi."), TokenKind.CustomTag);

        Assert.Equal(new LspPosition(0, 4), token.Range.Start);
        Assert.Equal(new LspPosition(0, 8), token.Range.End);
    }

    [Fact]
    public void Project_SpeakerInASoftWrappedParagraph_TokenLandsOnItsOwnLine()
    {
        // Regression: a speaker whose paragraph soft-wraps onto a second source line. Markdig
        // rebuilds such a paragraph's content buffer, so a buffer-relative content offset put
        // the token at the top of the file; it must sit on the speaker's own line instead.
        var source =
            """
            # Scene

            Alice: a line that
            softwraps onto a second.
            """;

        var token = AssertSingleSemanticToken(Project(source), TokenKind.SpeakerName);

        Assert.Equal("Alice", token.TextIn(source));
        Assert.Equal(2, token.Range.Start.Line); // zero-based: the third line, not the heading
    }

    private static SemanticToken AssertSingleSemanticToken(
        IEnumerable<SemanticToken> tokens, TokenKind kind) =>
        Assert.Single(tokens, token => token.Kind == kind);

    private static void AssertToken(
        IEnumerable<SemanticToken> tokens, TokenKind kind, string expected, string source) =>
        Assert.Equal(expected, AssertSingleSemanticToken(tokens, kind).TextIn(source));

    private static void AssertTokensDoNotOverlap(IReadOnlyList<SemanticToken> tokens)
    {
        var ordered = tokens
            .OrderBy(token => token.Range.Start.Line)
            .ThenBy(token => token.Range.Start.Character)
            .ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var previousEnd = ordered[i - 1].Range.End;
            var start = ordered[i].Range.Start;
            var startsAfterPrevious = start.Line > previousEnd.Line
                || (start.Line == previousEnd.Line && start.Character >= previousEnd.Character);
            Assert.True(startsAfterPrevious, "semantic tokens must not overlap");
        }
    }

    private IReadOnlyList<SemanticToken> Project(string source) =>
        [.. _projection.Project(Pipeline.Document(source), source)];
}
