using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.MarkdigNodeFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigUnmodeledNodeHandlerTests
{
    [Fact]
    public void Handle_ABlockThePolicyKeeps_DegradesItToItsExactSourceText()
    {
        const string Source = "<div>hi</div>";

        var handled = Handler(Source).Handle(HtmlBlockNode(Whole(Source)));

        var paragraph = Assert.IsType<Paragraph>(handled);
        var text = Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
        Assert.Equal(Source, text.Text);
        Assert.Equal(0, paragraph.Span.Start);
        Assert.Equal(Source.Length, paragraph.Span.Length);
    }

    [Fact]
    public void Handle_ABlockThePolicyDrops_KeepsNothing()
    {
        const string Source = "---";

        Assert.Null(Handler(Source).Handle(ThematicBreak(Whole(Source))));
    }

    [Fact]
    public void Handle_AnInlineThePolicyKeeps_DegradesItToItsExactSourceText()
    {
        const string Source = "<https://example.com>";

        var handled = Handler(Source).Handle(Autolink(Whole(Source)));

        var text = Assert.IsType<TextInline>(handled);
        Assert.Equal(Source, text.Text);
        Assert.Equal(Source.Length, text.Span.Length);
    }

    [Fact]
    public void Handle_AnInlineThePolicyDrops_KeepsNothing()
    {
        const string Source = "<https://example.com>";
        var handler = Handler(
            Source, TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.Autolink));

        Assert.Null(handler.Handle(Autolink(Whole(Source))));
    }

    [Fact]
    public void Handle_SlicesTheConstructOutOfALongerSource()
    {
        // The handler reads the original text through the node's span, so a construct surrounded
        // by other content still degrades to exactly its own characters.
        const string Source = "see <b> here";

        var handled = Handler(Source).Handle(InlineHtml(Range(4, 3)));

        Assert.Equal("<b>", Assert.IsType<TextInline>(handled).Text);
    }

    [Fact]
    public void Handle_APolicyWithAHandlingThisCodeDoesNotKnow_Throws()
    {
        // Neither keep nor ignore: a new UnmodeledNodeHandling nothing here was taught to carry
        // out. Failing loudly beats guessing, which would drop or keep the writer's content at
        // random.
        var handler = Handler("---", new UnknownHandlingPolicy());

        var thrown = Assert.Throws<NotSupportedException>(() => handler.Handle(ThematicBreak()));

        Assert.Contains("ThematicBreakBlock", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Handle_AnInlineWithAHandlingThisCodeDoesNotKnow_Throws()
    {
        var handler = Handler("<b>", new UnknownHandlingPolicy());

        Assert.Throws<NotSupportedException>(() => handler.Handle(InlineHtml()));
    }

    [Fact]
    public void Handle_ADroppedBlock_IsNotedWithTheKindInAWritersWords()
    {
        const string Source = "---";
        var handler = Handler(Source, out var diagnostics);

        handler.Handle(ThematicBreak(Whole(Source)));

        var note = AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DroppedUnmodeledMarkdown);
        Assert.Equal(DiagnosticSeverity.Info, note.Severity);
        Assert.Equal("divider", Assert.Single(note.MessageArguments));
        Assert.Equal(Source.Length, note.Span.Length);
    }

    [Theory]
    [InlineData("code block")]
    [InlineData("table")]
    public void Handle_EachDroppedKind_IsNamedForTheWriter(string expected)
    {
        var handler = Handler("x", out var diagnostics);

        handler.Handle(expected == "table" ? PipeTable() : FencedCode());

        Assert.Equal(
            expected,
            Assert.Single(
                AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DroppedUnmodeledMarkdown)
                    .MessageArguments));
    }

    [Fact]
    public void Handle_ADroppedInline_IsNotedLikeABlock()
    {
        const string Source = "<https://example.com>";
        var handler = Handler(
            Source, out var diagnostics, TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.Autolink));

        handler.Handle(Autolink(Whole(Source)));

        Assert.Equal(
            "autolink",
            Assert.Single(
                AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DroppedUnmodeledMarkdown)
                    .MessageArguments));
    }

    [Fact]
    public void Handle_ADroppedKindTheDefaultPolicyKeeps_IsStillNamed()
    {
        // Only a configured policy drops raw HTML, so this names the kind for the projects that
        // choose to.
        const string Source = "<div>hi</div>";
        var handler = Handler(
            Source, out var diagnostics, TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.RawHtml));

        handler.Handle(HtmlBlockNode(Whole(Source)));

        Assert.Equal(
            "raw HTML",
            Assert.Single(
                AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DroppedUnmodeledMarkdown)
                    .MessageArguments));
    }

    [Fact]
    public void Handle_ADroppedConstructWithNoKindOfItsOwn_IsNamedGenerically()
    {
        var handler = Handler(
            "x", out var diagnostics, TestUnmodeledNodePolicy.Default.Ignore(UnmodeledNodeKind.Other));

        handler.Handle(UnrecognizedBlock());

        Assert.Equal(
            "piece of Markdown",
            Assert.Single(
                AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DroppedUnmodeledMarkdown)
                    .MessageArguments));
    }

    [Fact]
    public void Handle_AKeptConstruct_IsNotNoted()
    {
        const string Source = "<div>hi</div>";
        var handler = Handler(Source, out var diagnostics);

        handler.Handle(HtmlBlockNode(Whole(Source)));

        AssertNotReported(diagnostics.Diagnostics);
    }

    private static MarkdigUnmodeledNodeHandler Handler(
        string source, IUnmodeledNodeHandlingPolicy? policy = null) =>
        new(source, policy ?? DefaultUnmodeledNodeHandlingPolicy.Instance, new DiagnosticBag());

    private static MarkdigUnmodeledNodeHandler Handler(
        string source, out DiagnosticBag diagnostics, IUnmodeledNodeHandlingPolicy? policy = null)
    {
        diagnostics = new DiagnosticBag();
        return new(source, policy ?? DefaultUnmodeledNodeHandlingPolicy.Instance, diagnostics);
    }
}
