using DialogueDown.Runtime.Positions;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests;

public sealed class RunnerTests
{
    [Fact]
    public void Step_Start_SaysTheFirstLine()
    {
        var context = PlayContext.Of(Playbooks.TwoLines());

        var result = Runner.Step(context, PlayState.Initial, new Start());

        var said = Assert.IsType<Said>(Assert.Single(result.Events));
        Assert.Equal("Alice", said.Speaker);
        Assert.Equal("Hello.", Text(said));
    }

    [Fact]
    public void Step_Start_StandsAtTheEntry()
    {
        var context = PlayContext.Of(Playbooks.TwoLines());

        var position = Assert.IsType<AtNode>(Started(context).Position);

        Assert.Equal(0, position.Node);
    }

    [Fact]
    public void Step_Next_SaysTheLineThatFollows()
    {
        var context = PlayContext.Of(Playbooks.TwoLines());

        var result = Runner.Step(context, Started(context), new Next());

        var said = Assert.IsType<Said>(Assert.Single(result.Events));
        Assert.Equal("Bob", said.Speaker);
        Assert.Equal("Goodbye.", Text(said));
    }

    [Fact]
    public void Step_NextPastTheLastLine_EndsTheRun()
    {
        var context = PlayContext.Of(Playbooks.OneLine());

        var result = Runner.Step(context, Started(context), new Next());

        Assert.IsType<Ended>(Assert.Single(result.Events));
        Assert.IsType<AtEnd>(result.State.Position);
    }

    [Fact]
    public void Step_StartAgainMidRun_BeginsOver()
    {
        // State is a value, so a fresh one costs nothing to make: starting over needs no way to
        // abort what is already running.
        var context = PlayContext.Of(Playbooks.TwoLines());
        var onward = Runner.Step(context, Started(context), new Next()).State;

        var result = Runner.Step(context, onward, new Start());

        Assert.Equal(0, Assert.IsType<AtNode>(result.State.Position).Node);
        Assert.Equal("Alice", Assert.IsType<Said>(Assert.Single(result.Events)).Speaker);
    }

    [Fact]
    public void Step_StartAtAnEndedRun_BeginsOver()
    {
        var context = PlayContext.Of(Playbooks.OneLine());
        var ended = Runner.Step(context, Started(context), new Next()).State;

        var result = Runner.Step(context, ended, new Start());

        Assert.Equal(0, Assert.IsType<AtNode>(result.State.Position).Node);
    }

    [Fact]
    public void Step_NextBeforeTheRunHasStarted_IsRefused()
    {
        var context = PlayContext.Of(Playbooks.OneLine());

        var result = Runner.Step(context, PlayState.Initial, new Next());

        Assert.IsType<Refused>(Assert.Single(result.Events));
    }

    [Fact]
    public void Step_NextAtAnEndedRun_IsRefusedAndGoesNowhere()
    {
        var context = PlayContext.Of(Playbooks.OneLine());
        var ended = Runner.Step(context, Started(context), new Next()).State;

        var result = Runner.Step(context, ended, new Next());

        Assert.IsType<Refused>(Assert.Single(result.Events));
        Assert.Equal(ended, result.State);
    }

    [Fact]
    public void Step_ALineWithNoNamedSpeaker_SaysItWithoutOne()
    {
        var context = PlayContext.Of(Playbooks.Anonymous());

        var said = Assert.IsType<Said>(Assert.Single(Runner.Step(context, PlayState.Initial, new Start()).Events));

        Assert.Null(said.Speaker);
    }

    private static PlayState Started(PlayContext context) =>
        Runner.Step(context, PlayState.Initial, new Start()).State;

    private static string Text(Said said) =>
        string.Concat(said.Speech.OfType<DialogueDown.Playbook.Speech.TextFragment>().Select(f => f.Text));
}
