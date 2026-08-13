using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Lsp;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Editor;

public sealed class SemanticTokenProjectionTests
{
    private readonly SemanticTokenProjection _projection = new();

    [Fact]
    public void Project_NullMarkdown_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            _projection.Project(null!, Pipeline.Document("x"), "x"));

    [Fact]
    public void Project_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            _projection.Project(Pipeline.Compilation("x").Markdown, null!, "x"));

    [Fact]
    public void Project_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            _projection.Project(Pipeline.Compilation("").Markdown, Pipeline.Document(""), null!));

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
    public void Project_ControlBlock_ProjectsEachMarkerKeyword()
    {
        var source =
            """
            > `if` `Rich?`
            >
            > Welcome.
            >
            > `elseif` `Known?`
            >
            > Welcome back.
            >
            > `else`
            >
            > Try downstairs.
            """;

        var keywords = Project(source)
            .Where(token => token.Kind == TokenKind.ControlKeyword)
            .Select(token => token.TextIn(source));

        Assert.Equal(["`if`", "`elseif`", "`else`"], keywords);
    }

    [Fact]
    public void Project_CodeSpanForms_ProjectTheirSemanticKinds()
    {
        var source =
            """
            > `if` `Rainy?`
            >
            > `("fade in")`
            >
            > Alice: `playSound("wind")` `"playerName"`
            >
            > - `60%` Static weight.
            > - `Luck%` Dynamic weight.
            > - `%` Remaining weight.
            """;
        var tokens = Project(source);

        Assert.Equal(
            ["`(\"fade in\")`", "`playSound(\"wind\")`"],
            TextsOf(tokens, TokenKind.Command, source));
        Assert.Equal(["`\"playerName\"`"], TextsOf(tokens, TokenKind.Query, source));
        Assert.Equal(["`Rainy?`"], TextsOf(tokens, TokenKind.Condition, source));
        Assert.Equal(["`60%`", "`%`"], TextsOf(tokens, TokenKind.StaticWeight, source));
        Assert.Equal(["`Luck%`"], TextsOf(tokens, TokenKind.DynamicWeight, source));
    }

    [Fact]
    public void Project_MarkerShapedTopLevelParagraph_IsNotAControlKeyword()
    {
        var tokens = Project("`if` `Rich?`");

        Assert.DoesNotContain(tokens, token => token.Kind == TokenKind.ControlKeyword);
    }

    [Fact]
    public void Project_MarkerShapedParagraphInAnOrdinaryQuote_IsNotAControlKeyword()
    {
        var tokens = Project(
            """
            > An aside.
            >
            > `if` `Rich?`
            """);

        Assert.DoesNotContain(tokens, token => token.Kind == TokenKind.ControlKeyword);
    }

    [Fact]
    public void Project_TerminalDivert_ProjectsAReservedAnchorTokenOverTheEnd()
    {
        var source = "Alice: Farewell => [the end](#END)";

        var token = AssertSingleSemanticToken(Project(source), TokenKind.ReservedAnchor);
        Assert.Equal("#END", token.TextIn(source));
    }

    [Fact]
    public void Project_LowercaseEndAnchor_IsNotAReservedAnchor()
    {
        // A scene titled "End" slugs to lowercase "end"; a divert to it is an ordinary scene jump,
        // not the reserved uppercase terminator, so it carries no reserved-anchor token.
        var tokens = Project("Alice: Farewell => [the end](#end)");

        Assert.DoesNotContain(tokens, token => token.Kind == TokenKind.ReservedAnchor);
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

    [Fact]
    public void Project_AnIgnoredTable_MarksItAsLeftOutOfTheDialogue()
    {
        var source =
            """
            Alice: Ask around.

            | Rumor | Source |
            | --- | --- |
            | The bridge is out | The miller |
            """;

        var tokens = Project(source);

        var token = AssertSingleSemanticToken(tokens, TokenKind.IgnoredMarkdown);
        Assert.StartsWith("| Rumor", token.TextIn(source), StringComparison.Ordinal);
    }

    [Fact]
    public void Project_AnIgnoredDivider_IsMarked()
    {
        var source =
            """
            Alice: Hi

            ---

            Bob: Bye
            """;

        Assert.Equal("---", AssertSingleSemanticToken(Project(source), TokenKind.IgnoredMarkdown).TextIn(source));
    }

    [Fact]
    public void Project_SeveralIgnoredConstructs_AreEachMarked()
    {
        var source =
            """
            Alice: Hi

            ---

            Bob: Bye

            ---
            """;

        Assert.Equal(2, Project(source).Count(token => token.Kind == TokenKind.IgnoredMarkdown));
    }

    [Fact]
    public void Project_AnIgnoredConstructInsideAChoice_IsMarked()
    {
        // The diagnostic carries the construct's span wherever it sat, so nesting needs no walk.
        var source =
            """
            - Alice: Look

                ```mermaid
                graph TD
                ```
            """;

        Assert.Single(Project(source), token => token.Kind == TokenKind.IgnoredMarkdown);
    }

    [Fact]
    public void Project_KeptMarkdown_IsNotMarked()
    {
        // Kept text becomes dialogue text, so it reads as dialogue rather than as something apart.
        Assert.DoesNotContain(
            Project("<div>hi</div>"), token => token.Kind == TokenKind.IgnoredMarkdown);
    }

    [Fact]
    public void Project_DefaultIgnoredMarkdownConfiguredToKeep_IsNotMarked()
    {
        const string Source = """
            | Speaker | Mood |
            | --- | --- |
            | Alice | calm |
            """;
        var options = CompilerOptions.Default with
        {
            UnmodeledMarkdown = new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
            {
                [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep,
            },
        };

        Assert.DoesNotContain(
            Project(Source, options), token => token.Kind == TokenKind.IgnoredMarkdown);
    }

    [Fact]
    public void Project_DefaultKeptMarkdownConfiguredToIgnore_IsMarked()
    {
        const string Source = "<div>writer note</div>";
        var options = CompilerOptions.Default with
        {
            UnmodeledMarkdown = new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
            {
                [UnmodeledNodeKind.RawHtml] = UnmodeledNodeHandling.Ignore,
            },
        };

        Assert.Equal(
            Source,
            AssertSingleSemanticToken(
                Project(Source, options), TokenKind.IgnoredMarkdown).TextIn(Source));
    }

    [Fact]
    public void Project_AComment_IsNotMarked() =>
        // A comment is always left out, so the editor's own Markdown parser styles it.
        Assert.DoesNotContain(
            Project("Alice: Hi <!-- note --> there"),
            token => token.Kind == TokenKind.IgnoredMarkdown);

    [Fact]
    public void Project_AScriptWithNothingIgnored_MarksNothing() =>
        Assert.DoesNotContain(
            Project("Alice: Hello there."), token => token.Kind == TokenKind.IgnoredMarkdown);

    [Fact]
    public void Project_AnIgnoredConstruct_IsMarkedWhereverTheDiagnosticReportsIt()
    {
        // The coupling this projection rests on: the diagnostic locates what the policy ignored,
        // so demoting or suppressing it would silently take the highlighting with it.
        var source =
            """
            Alice: Hi

            ---
            """;
        var compilation = Pipeline.Compilation(source);

        var reported = compilation.LocatedDiagnostics
            .Single(diagnostic => diagnostic.Code == DiagnosticCatalog.IgnoredUnmodeledMarkdown.Code);
        var token = AssertSingleSemanticToken(Project(source), TokenKind.IgnoredMarkdown);

        Assert.Equal(reported.StartOffset, token.Range.Start.OffsetIn(source));
        Assert.Equal(reported.EndOffset, token.Range.End.OffsetIn(source));
    }

    private static SemanticToken AssertSingleSemanticToken(
        IEnumerable<SemanticToken> tokens, TokenKind kind) =>
        Assert.Single(tokens, token => token.Kind == kind);

    private static void AssertToken(
        IEnumerable<SemanticToken> tokens, TokenKind kind, string expected, string source) =>
        Assert.Equal(expected, AssertSingleSemanticToken(tokens, kind).TextIn(source));

    private static IReadOnlyList<string> TextsOf(
        IEnumerable<SemanticToken> tokens, TokenKind kind, string source) =>
        [.. tokens.Where(token => token.Kind == kind).Select(token => token.TextIn(source))];

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

    private IReadOnlyList<SemanticToken> Project(string source)
    {
        return Project(source, CompilerOptions.Default);
    }

    private IReadOnlyList<SemanticToken> Project(string source, CompilerOptions options)
    {
        var compilation = Pipeline.Compilation(source, options);
        return
        [
            .. _projection.Project(
                compilation.Markdown, compilation.Script, source, compilation.LocatedDiagnostics),
        ];
    }
}
