using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What arriving at each kind of node does, asked of that node alone.
/// </summary>
public sealed class ArrivalTests
{
    [Fact]
    public void At_ALine_SaysItWithItsSpeakersName()
    {
        var context = Playbooks.Of([Playbooks.Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)], ["Alice"]);

        AssertSaid(Arrival.At(context, 0), speaker: "Alice", text: "Hello.");
    }

    [Fact]
    public void At_ALine_StandsThere()
    {
        var context = Playbooks.Of([Playbooks.Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)], ["Alice"]);

        AssertAt(Arrival.At(context, 0), 0);
    }

    [Fact]
    public void At_ALineSaidByTheDefaultSpeaker_NamesNobody()
    {
        // The anonymous speaker has no name, which is why a said carries none rather than an
        // empty one: there is a difference between nobody and somebody called "".
        var context = Playbooks.Of([Playbooks.Line(0, speaker: 0, "Nobody said this.", next: 1), new EndNode(1)], [null]);

        AssertSaid(Arrival.At(context, 0), speaker: null, text: "Nobody said this.");
    }

    [Fact]
    public void At_TheEnd_EndsTheRun()
    {
        var context = Playbooks.Of([new EndNode(0)]);

        AssertEnded(Arrival.At(context, 0));
    }

    [Fact]
    public void At_AKindThisBuildCannotPlay_SaysSoRatherThanStalling()
    {
        // Silence here would leave a run standing at a node forever, which reads as a hang rather
        // than as a construct nobody has taught the runner yet.
        var context = Playbooks.NotYetPlayable();

        AssertRefused(Arrival.At(context, 0), "ChoiceNode");
    }
}
