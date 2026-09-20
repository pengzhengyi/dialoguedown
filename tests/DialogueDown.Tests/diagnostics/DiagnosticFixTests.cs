using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticFixTests
{
    [Fact]
    public void Constructor_ExposesTheTitleAndEdits()
    {
        var edit = DiagnosticsFactory.Edit(newText: "#");

        var fix = DiagnosticsFactory.Fix(title: "Make it literal", edits: [edit]);

        Assert.Equal("Make it literal", fix.Title);
        Assert.Equal(edit, Assert.Single(fix.Edits));
    }

    [Fact]
    public void Equality_EqualEditContentInSeparateLists_AreEqual()
    {
        var one = DiagnosticsFactory.Fix(edits: [DiagnosticsFactory.Edit(span: SourceSpanFactory.Span(2, 0))]);
        var two = DiagnosticsFactory.Fix(edits: [DiagnosticsFactory.Edit(span: SourceSpanFactory.Span(2, 0))]);

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
    }

    [Fact]
    public void Constructor_NullTitle_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new DiagnosticFix(null!, [DiagnosticsFactory.Edit()]));

    [Fact]
    public void Constructor_EmptyTitle_Throws() =>
        Assert.Throws<ArgumentException>(
            () => new DiagnosticFix(string.Empty, [DiagnosticsFactory.Edit()]));

    [Fact]
    public void Constructor_NullEdits_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DiagnosticFix("Escape as literal text", null!));

    [Fact]
    public void Constructor_NoEdits_Throws() =>
        Assert.Throws<ArgumentException>(() => new DiagnosticFix("Escape as literal text", []));
}
