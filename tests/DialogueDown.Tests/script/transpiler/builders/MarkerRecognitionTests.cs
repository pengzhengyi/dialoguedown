using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using static DialogueDown.Tests.Support.MarkdownAstFactory;
using MarkdownInline = DialogueDown.Markdown.MarkdownInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class MarkerRecognitionTests
{
    [Fact]
    public void Read_IfWithCondition_YieldsAnIfMarkerWithTheConditionAndNoRemainder()
    {
        MarkdownInline[] inlines = [CodeSpan("if"), Text(" "), CodeSpan("Rich?")];

        var marker = Assert.IsType<BranchMarker>(MarkerRecognition.Read(inlines));

        Assert.Equal(BranchKind.If, marker.Kind);
        Assert.Equal("Rich", Assert.IsType<Condition>(marker.Condition).Key);
        Assert.Empty(marker.Remainder);
    }

    [Fact]
    public void Read_ElseIfWithCondition_YieldsAnElseIfMarker()
    {
        MarkdownInline[] inlines = [CodeSpan("elseif"), Text(" "), CodeSpan("Poor?")];

        var marker = Assert.IsType<BranchMarker>(MarkerRecognition.Read(inlines));

        Assert.Equal(BranchKind.ElseIf, marker.Kind);
        Assert.Equal("Poor", Assert.IsType<Condition>(marker.Condition).Key);
    }

    [Fact]
    public void Read_BareElse_YieldsAnElseMarkerWithNoCondition()
    {
        MarkdownInline[] inlines = [CodeSpan("else")];

        var marker = Assert.IsType<BranchMarker>(MarkerRecognition.Read(inlines));

        Assert.Equal(BranchKind.Else, marker.Kind);
        Assert.Null(marker.Condition);
        Assert.Empty(marker.Remainder);
    }

    [Fact]
    public void Read_ElseWithACondition_RecognizesTheElseAndCapturesTheStrayCondition()
    {
        // Lenient recognition: an `else` with a condition is still an else marker; the stray
        // condition is captured (not swallowed) so the validator can report it.
        MarkdownInline[] inlines = [CodeSpan("else"), Text(" "), CodeSpan("Rich?")];

        var marker = Assert.IsType<BranchMarker>(MarkerRecognition.Read(inlines));

        Assert.Equal(BranchKind.Else, marker.Kind);
        Assert.Equal("Rich", Assert.IsType<Condition>(marker.Condition).Key);
        Assert.Empty(marker.Remainder);
    }

    [Fact]
    public void Read_MarkerFusedWithTrailingSpeech_RecognizesItAndKeepsTheSpeechAsRemainder()
    {
        // A fused marker (a missing quoted blank line) is recognized with a non-empty remainder,
        // which the validator reports as "a marker must stand alone".
        MarkdownInline[] inlines =
            [CodeSpan("if"), Text(" "), CodeSpan("Rich?"), Text(" Guard: hi")];

        var marker = Assert.IsType<BranchMarker>(MarkerRecognition.Read(inlines));

        Assert.Equal(BranchKind.If, marker.Kind);
        Assert.Equal("Rich", Assert.IsType<Condition>(marker.Condition).Key);
        Assert.NotEmpty(marker.Remainder);
    }

    [Fact]
    public void Read_IfWithoutACondition_RecognizesTheIfWithANullCondition()
    {
        MarkdownInline[] inlines = [CodeSpan("if")];

        var marker = Assert.IsType<BranchMarker>(MarkerRecognition.Read(inlines));

        Assert.Equal(BranchKind.If, marker.Kind);
        Assert.Null(marker.Condition);
    }

    [Fact]
    public void Read_OneSpanIfCondition_IsNotAMarker()
    {
        // `if Rich?` in one span is a condition on the key "if Rich", not a marker keyword.
        MarkdownInline[] inlines = [CodeSpan("if Rich?")];

        Assert.Null(MarkerRecognition.Read(inlines));
    }

    [Fact]
    public void Read_PlainLine_IsNotAMarker()
    {
        MarkdownInline[] inlines = [Text("Alice: hello")];

        Assert.Null(MarkerRecognition.Read(inlines));
    }
}
