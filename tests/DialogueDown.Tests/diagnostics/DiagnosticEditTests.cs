using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticEditTests
{
    [Fact]
    public void Constructor_ExposesTheSpanAndTheReplacement()
    {
        var span = SourceSpanFactory.Span(3, 2);

        var edit = DiagnosticsFactory.Edit(span, "#");

        Assert.Equal(span, edit.Span);
        Assert.Equal("#", edit.NewText);
    }

    [Fact]
    public void Constructor_AnEmptySpan_RepresentsAnInsertion()
    {
        // An insertion writes before the span's start, so its span is empty; the factory's default
        // new text is the escape's backslash.
        var edit = DiagnosticsFactory.Edit(span: SourceSpanFactory.Span(4, 0));

        Assert.Equal(0, edit.Span.Length);
        Assert.Equal("\\", edit.NewText);
    }

    [Fact]
    public void Constructor_NullText_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new DiagnosticEdit(SourceSpanFactory.Span(), null!));
}
