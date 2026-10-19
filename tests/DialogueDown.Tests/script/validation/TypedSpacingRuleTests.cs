using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;

namespace DialogueDown.Tests.Script.Validation;

public sealed class TypedSpacingRuleTests
{
    [Fact]
    public void Check_SpacesTypedTogetherInSpeech_NotesThemAtTheirSpan()
    {
        const string Source = "Alice: a    b";

        var note = AssertReported(Check(Source), DiagnosticCatalog.TypedSpacing);

        Assert.Equal(DiagnosticSeverity.Info, note.Severity);
        Assert.Equal(SpanOf(Source, "    "), note.Span);
    }

    [Fact]
    public void Check_ATabBesideASpace_Notes() =>
        AssertNotes("Alice: a \tb", " \t");

    [Fact]
    public void Check_SpacingSplitAcrossEntities_NotesItOnce() =>
        AssertNotes("Alice: a&#32;&#32;b", "&#32;&#32;");

    [Fact]
    public void Check_ASpaceBesideAnEntitySpace_NotesBoth() =>
        AssertNotes("Alice: a &#32;b", " &#32;");

    [Fact]
    public void Check_AnEncodedLineBreakBetweenSpaces_Notes() =>
        AssertNotes("Alice: x &#10; y", " &#10; ");

    [Fact]
    public void Check_TwoPlacesInOneLine_NotesEach() =>
        AssertNotes("Alice: a  b   c", "  ", "   ");

    [Fact]
    public void Check_SpacingInsideEmphasis_Notes() =>
        AssertNotes("Alice: **a  b**", "  ");

    [Fact]
    public void Check_SpacingInALinksLabel_Notes() =>
        AssertNotes("Alice: [Talk  to Bob](#inn)", "  ");

    [Fact]
    public void Check_SpacingInAltText_Notes() =>
        AssertNotes("Alice: ![Alice  smiling](alice.png)", "  ");

    [Fact]
    public void Check_SpacingInAJumpsLabel_Notes() =>
        AssertNotes("=> [Back  home](#start)", "  ");

    [Fact]
    public void Check_SpacingBeforeATrailingCommand_Notes() =>
        AssertNotes("Alice: Hello  `Wave()`", "  ");

    [Fact]
    public void Check_SpacingOnEachSideOfAJump_NotesEachSideApart() =>
        AssertNotes("Alice: a  => [Go](#inn)   b", "  ", "   ");

    [Fact]
    public void Check_SpacingInAnOptionsLabel_NotesItOnce()
    {
        const string Source =
            """
            Alice: Leaving?

            - Stay  here
            - Go
            """;

        AssertNotes(Source, "  ");
    }

    [Fact]
    public void Check_SingleSpaces_NotesNothing() =>
        AssertNotReported(Check("Alice: Hello, Bob."));

    [Fact]
    public void Check_ASingleTab_NotesNothing() =>
        AssertNotReported(Check("Alice: a\tb"));

    [Fact]
    public void Check_NamedSpacesBesideASpace_NotesNothing() =>
        AssertNotReported(Check("Alice: a&nbsp;&nbsp; b"));

    [Fact]
    public void Check_ASpaceOnEachSideOfACommand_NotesNothing() =>
        AssertNotReported(Check("Alice: Hi `Wave()` there"));

    [Theory]
    [InlineData("Alice: Hi [ Talk](#inn)")]
    [InlineData("Alice: [Talk ](#inn) now")]
    public void Check_ASpaceOnEachSideOfALinksEdge_NotesNothing(string source) =>
        AssertNotReported(Check(source));

    [Fact]
    public void Check_SpacingAfterASpeakersColon_NotesNothing() =>
        AssertNotReported(Check("Alice:    Hello"));

    [Fact]
    public void Check_SpacingBetweenAControlLinesEffects_NotesNothing() =>
        AssertNotReported(Check("`Wave()`  `Bow()`"));

    [Fact]
    public void Check_SpacingInAHeading_NotesNothing()
    {
        const string Source =
            """
            # The  Big  Scene

            Alice: Hi.
            """;

        AssertNotReported(Check(Source));
    }

    // Asserts the rule notes exactly these spacings of the source, in order, each at its own span.
    private static void AssertNotes(string source, params string[] spacings)
    {
        var notes = Check(source);

        Assert.All(notes, note => Assert.Equal(DiagnosticCatalog.TypedSpacing, note.Descriptor));
        Assert.Equal(spacings.Select(spacing => SpanOf(source, spacing)), notes.Select(note => note.Span));
    }

    // The span of the first place the spacing is written in the source.
    private static SourceSpan SpanOf(string source, string spacing) =>
        new(source.IndexOf(spacing, StringComparison.Ordinal), spacing.Length);

    private static IReadOnlyList<Diagnostic> Check(string source)
    {
        var bag = new DiagnosticBag();
        new TypedSpacingRule().Check(DialogueTreeIndex.Build(Pipeline.UntilDesugared(source)), bag);
        return bag.Diagnostics;
    }
}
