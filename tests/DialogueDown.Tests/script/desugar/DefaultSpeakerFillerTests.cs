using DialogueDown.Common;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DefaultSpeakerFillerTests
{
    [Fact]
    public void Fill_LineWithoutSpeaker_GetsDefaultSpeaker()
    {
        var line = Line(Text("Hello")) with { Span = SourceSpanFactory.Span(7, 5) };

        var filled = DefaultSpeakerFiller.Fill(line);

        var speaker = AssertDefaultSpeaker(filled.Speaker);
        Assert.Equal(SourceSpan.EmptyAt(7), speaker.Span);
        Assert.True(speaker.Span.IsEmpty);
        Assert.Same(line.Speech, filled.Speech);
    }

    [Fact]
    public void Fill_LineWithSpeaker_IsReturnedUnchanged()
    {
        var line = Line(SpeakerNameReference("Alice"), Text("Hello"));

        Assert.Same(line, DefaultSpeakerFiller.Fill(line));
    }

    [Fact]
    public void Fill_LoneCommandLine_GetsDefaultSpeaker()
    {
        // A silent command is a speaker-less line whose speech is a command; the same
        // fill covers it, with no special case.
        var line = Line(DefaultCommand("wave"));

        AssertDefaultSpeaker(DefaultSpeakerFiller.Fill(line).Speaker);
    }
}
