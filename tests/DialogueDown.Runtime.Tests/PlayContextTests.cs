using DialogueDown.Playbook.Nodes;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What a run needs and never changes, and how a playbook is read through it.
/// </summary>
public sealed class PlayContextTests
{
    [Fact]
    public void Of_NoPlaybook_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => PlayContext.Of(null!));
    }

    [Fact]
    public void Entry_APlaybook_IsWhereItSaysToBegin()
    {
        var context = PlayContextFactory.Of(
            [End(0), Line(1, speaker: 0, "Hello.", next: 0)],
            ["Alice"],
            entry: 1);

        Assert.Equal(1, context.Entry);
    }

    [Fact]
    public void NodeAt_APosition_IsTheNodeStandingThere()
    {
        var context = PlayContextFactory.TwoLines();

        Assert.Equal(1, Assert.IsType<LineNode>(context.NodeAt(1)).Id);
    }

    [Fact]
    public void SpeakerName_ANamedSpeaker_IsWhatTheyAreCalled()
    {
        var context = PlayContextFactory.TwoLines();

        Assert.Equal("Bob", context.SpeakerName(1));
    }

    [Fact]
    public void SpeakerName_TheAnonymousSpeaker_IsNobody()
    {
        // The default speaker has no name, which is not the same as being called nothing.
        var context = PlayContextFactory.Of([End(0)], [null]);

        Assert.Null(context.SpeakerName(0));
    }

    [Fact]
    public void Playbook_AContext_IsTheOneItWasMadeFrom()
    {
        var context = PlayContextFactory.OneLine();

        Assert.Equal("a-script.dialogue.md", context.Playbook.Script);
    }
}
