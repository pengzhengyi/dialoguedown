using DialogueDown.Diagnostics;
using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Tests.Diagnostics;

public sealed class DiagnosticProjectionTests
{
    private readonly DiagnosticProjection _projection = new();

    [Fact]
    public void Project_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _projection.Project(null!));

    [Fact]
    public void Project_NoDiagnostics_IsEmpty() =>
        Assert.Empty(_projection.Project([]));

    [Theory]
    [InlineData(DiagnosticSeverity.Error, 1)]
    [InlineData(DiagnosticSeverity.Warning, 2)]
    [InlineData(DiagnosticSeverity.Info, 3)]
    public void Project_MapsCoreSeverityToTheLspProtocolNumber(
        DiagnosticSeverity core, int expectedLspNumber)
    {
        var projected = ProjectOne(Located(severity: core));

        Assert.Equal(expectedLspNumber, (int)projected.Severity);
    }

    [Fact]
    public void Project_ConvertsOneBasedPositionsToZeroBasedRange()
    {
        var projected = ProjectOne(
            Located(start: new LinePosition(3, 5), end: new LinePosition(3, 9)));

        Assert.Equal(new LspPosition(2, 4), projected.Range.Start);
        Assert.Equal(new LspPosition(2, 8), projected.Range.End);
    }

    [Fact]
    public void Project_FirstLineFirstColumn_MapsToTheOrigin()
    {
        var projected = ProjectOne(
            Located(start: new LinePosition(1, 1), end: new LinePosition(1, 1)));

        Assert.Equal(new LspPosition(0, 0), projected.Range.Start);
        Assert.Equal(new LspPosition(0, 0), projected.Range.End);
    }

    [Fact]
    public void Project_ZeroWidthSpan_CollapsesTheRange()
    {
        var position = new LinePosition(4, 7);

        var projected = ProjectOne(Located(start: position, end: position));

        Assert.Equal(projected.Range.Start, projected.Range.End);
        Assert.Equal(new LspPosition(3, 6), projected.Range.Start);
    }

    [Fact]
    public void Project_MultiLineSpan_MapsBothEnds()
    {
        var projected = ProjectOne(
            Located(start: new LinePosition(2, 3), end: new LinePosition(5, 1)));

        Assert.Equal(new LspPosition(1, 2), projected.Range.Start);
        Assert.Equal(new LspPosition(4, 0), projected.Range.End);
    }

    [Fact]
    public void Project_CarriesCodeAndMessageVerbatim()
    {
        var projected = ProjectOne(
            Located(code: "DLG2001", message: "Two scenes resolve to the same anchor '#chapter'."));

        Assert.Equal("DLG2001", projected.Code);
        Assert.Equal("Two scenes resolve to the same anchor '#chapter'.", projected.Message);
    }

    [Fact]
    public void Project_TagsEveryDiagnosticWithTheDialogueDownSource()
    {
        var projected = ProjectOne(Located());

        Assert.Equal("dialoguedown", projected.Source);
    }

    [Fact]
    public void Project_PreservesReportOrder()
    {
        var first = Located(code: "DLG1001");
        var second = Located(code: "DLG2001");

        var projected = _projection.Project([first, second]);

        Assert.Collection(
            projected,
            diagnostic => Assert.Equal("DLG1001", diagnostic.Code),
            diagnostic => Assert.Equal("DLG2001", diagnostic.Code));
    }

    [Fact]
    public void Project_NoFixes_LeavesFixesNull() =>
        Assert.Null(ProjectOne(Located()).Fixes);

    [Fact]
    public void Project_CarriesFixesWithOffsetsRelativeToTheDiagnostic()
    {
        var projected = ProjectOne(Located(
            startOffset: 10,
            fixes: [new LocatedFix("Escape as literal text", [new LocatedEdit(10, 10, "\\")])]));

        var fix = Assert.Single(projected.Fixes!);
        Assert.Equal("Escape as literal text", fix.Title);
        var edit = Assert.Single(fix.Edits);
        Assert.Equal(0, edit.Start);
        Assert.Equal(0, edit.End);
        Assert.Equal("\\", edit.NewText);
    }

    [Fact]
    public void Project_FixEditsInsideTheDiagnostic_AreOffsetFromItsStart()
    {
        var projected = ProjectOne(Located(
            startOffset: 10,
            fixes: [new LocatedFix("Replace", [new LocatedEdit(12, 14, "x")])]));

        var edit = Assert.Single(Assert.Single(projected.Fixes!).Edits);
        Assert.Equal(2, edit.Start);
        Assert.Equal(4, edit.End);
    }

    private static LocatedDiagnostic Located(
        DiagnosticSeverity severity = DiagnosticSeverity.Error,
        LinePosition? start = null,
        LinePosition? end = null,
        string code = "DLG0001",
        string message = "Something went wrong.",
        int startOffset = 0,
        int endOffset = 1,
        IReadOnlyList<LocatedFix>? fixes = null) =>
        new(
            code,
            severity,
            DiagnosticCategory.Syntax,
            message,
            start ?? new LinePosition(1, 1),
            end ?? new LinePosition(1, 2),
            startOffset,
            endOffset)
        {
            Fixes = fixes ?? [],
        };

    private LspDiagnostic ProjectOne(LocatedDiagnostic diagnostic) =>
        Assert.Single(_projection.Project([diagnostic]));
}
