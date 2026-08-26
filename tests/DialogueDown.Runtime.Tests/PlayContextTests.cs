using DialogueDown.Playbook.Nodes;

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
        var context = Playbooks.Of(
            [new EndNode(0), Playbooks.Line(1, speaker: 0, "Hello.", next: 0)],
            ["Alice"],
            entry: 1);

        Assert.Equal(1, context.Entry);
    }

    [Fact]
    public void NodeAt_APosition_IsTheNodeStandingThere()
    {
        var context = Playbooks.TwoLines();

        Assert.Equal(1, Assert.IsType<LineNode>(context.NodeAt(1)).Id);
    }

    [Fact]
    public void SpeakerName_ANamedSpeaker_IsWhatTheyAreCalled()
    {
        var context = Playbooks.TwoLines();

        Assert.Equal("Bob", context.SpeakerName(1));
    }

    [Fact]
    public void SpeakerName_TheAnonymousSpeaker_IsNobody()
    {
        // The default speaker has no name, which is not the same as being called nothing.
        var context = Playbooks.Of([new EndNode(0)], [null]);

        Assert.Null(context.SpeakerName(0));
    }

    [Fact]
    public void Playbook_AContext_IsTheOneItWasMadeFrom()
    {
        var context = Playbooks.OneLine();

        Assert.Equal("a-script.dialogue.md", context.Playbook.Script);
    }
}
