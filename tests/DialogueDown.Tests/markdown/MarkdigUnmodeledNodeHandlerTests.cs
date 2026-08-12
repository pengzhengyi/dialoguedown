using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
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

    private static MarkdigUnmodeledNodeHandler Handler(
        string source, IUnmodeledNodeHandlingPolicy? policy = null) =>
        new(source, policy ?? DefaultUnmodeledNodeHandlingPolicy.Instance);
}
