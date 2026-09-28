using CsCheck;
using DialogueDown.Playbook.Checking;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What must hold for <em>every</em> playbook a walk can take, not only the ones an example names.
/// </summary>
/// <remarks>
/// The suite's example tests each pin one playbook to one expected step, which is the right way to
/// specify behavior. What they cannot state is an invariant quantified over all playbooks, and a
/// run that walks off the document it is playing is exactly that kind of defect: no single example
/// is wrong, but some unwritten one would be.
/// <para>
/// Sample counts are deliberately modest: these run in the ordinary suite, and a property that
/// makes the suite slow stops being run at all.
/// </para>
/// </remarks>
public sealed class RunnerWalkPropertyTests
{
    private const int Samples = 200;

    // A drawn playbook may loop, so a walk is cut off rather than run to an end. Passing a node can
    // take three steps: answering what playing it needs, moving on, and answering which way out to
    // take. So this is long enough to pass through every node a playbook of this size can hold,
    // several times over.
    private const int MostSteps = 120;

    // Why a run turns an answer away. An answer can also lead the walk somewhere the run refuses,
    // such as into a ring, and that refusal is about where the walk went, not about the answer.
    private static readonly RefusalReason[] _answerRefusals =
        [RefusalReason.UnansweredKey, RefusalReason.UnaskedKey, RefusalReason.WrongAnswerKind];

    /// <summary>
    /// A run only ever stands at a node the playbook has.
    /// </summary>
    /// <remarks>
    /// A position is an index, and it is read straight — <c>Nodes[position]</c>, with no guard,
    /// because a reader has already refused a playbook that points where it does not have. The
    /// runner is what turns one position into the next, so it is the runner that must not
    /// manufacture an index out of a document that never held one.
    /// </remarks>
    [Fact]
    public void AWalkOnlyEverStandsWhereThePlaybookHasANode() =>
        ForEveryWalk((context, stepped) => AssertAddressable(context, stepped.State.Situation));

    /// <summary>
    /// Every answer a walk gives is one the run accepts.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. A run that turns an answer away keeps waiting
    /// where it asked, so a walk whose answers were turned away would spend every step there. It
    /// would cover nothing past the first question, and the walk above would still pass.
    /// </remarks>
    [Fact]
    public void EveryAnswerAWalkGives_IsOneTheRunAccepts() =>
        ForEveryWalk(
            (_, stepped) =>
            {
                var turnedAway = stepped.Events.OfType<Refused>()
                    .FirstOrDefault(refused => _answerRefusals.Contains(refused.Reason));

                Assert.True(
                    turnedAway is null,
                    $"The run turned away what the world answered: {turnedAway?.Explanation}");
            });

    /// <summary>
    /// What this draws is what a reader accepts.
    /// </summary>
    /// <remarks>
    /// The walk above is quantified over the playbooks a runtime can be handed, which are the ones
    /// the reader passes. Asking the reader itself, rather than trusting the generator to have
    /// encoded its rules, is what keeps the two from drifting apart in silence.
    /// </remarks>
    [Fact]
    public void EveryDrawnPlaybookIsOneTheReaderAccepts() =>
        PlaybookGen.Valid().Sample(
            playbook => PlaybookCheckerFactory.CreateDefault().Check(playbook), iter: Samples);

    // Walks every drawn playbook from its start, answering its questions from a world drawn beside
    // it. Every step the walk takes is checked, not only the one it stops at.
    private static void ForEveryWalk(Action<PlayContext, StepResult> everyStepHolds) =>
        Gen.Select(PlaybookGen.Valid(), PlaybookGen.Worlds()).Sample(
            (playbook, world) =>
            {
                var context = PlayContext.Of(playbook);
                var state = PlayState.Initial;
                Command? command = new Start();

                for (var taken = 0; taken < MostSteps && command is not null; taken++)
                {
                    var stepped = Runner.Step(context, state, command);

                    everyStepHolds(context, stepped);

                    state = stepped.State;
                    command = MovesOnFrom(state.Situation, world);
                }
            },
            iter: Samples);

    // The walk sends whatever the stage it reached calls for, so a run that stopped to hand the
    // host work, or to ask the world something, carries on rather than ending the walk there.
    private static Command? MovesOnFrom(Situation situation, DrawnWorld world) => situation switch
    {
        AtNode => new Next(),
        AwaitingDone => new Done(),
        AwaitingSupply waiting => world.Answering(waiting.Keys),
        _ => null,
    };

    private static void AssertAddressable(PlayContext context, Situation situation)
    {
        // Three stages name a node, and a walk standing outside the document at any of them is the
        // same defect.
        var node = situation switch
        {
            AtNode at => at.Node,
            AwaitingDone waiting => waiting.Node,
            AwaitingSupply waiting => waiting.Node,
            _ => (int?)null,
        };

        if (node is { } addressable)
        {
            Assert.InRange(addressable, 0, context.Playbook.Nodes.Length - 1);
        }
    }
}
