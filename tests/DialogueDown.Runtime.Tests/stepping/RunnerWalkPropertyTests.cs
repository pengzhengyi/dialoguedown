using CsCheck;
using DialogueDown.Playbook.Checking;
using DialogueDown.Runtime.Positions;
using DialogueDown.Runtime.Protocol;

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

    // A drawn playbook may loop, so a walk is cut off rather than run to an end. Long enough to
    // pass through every node a playbook of this size can hold, several times over.
    private const int MostSteps = 40;

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
        ForEveryPlaybook(
            context =>
            {
                var state = Step(context, PlayState.Initial, new Start());

                for (var taken = 0; taken < MostSteps && MovesOnFrom(state.Position) is { } command; taken++)
                {
                    state = Step(context, state, command);
                }

                AssertAddressable(context, state.Position);
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

    private static void ForEveryPlaybook(Action<PlayContext> invariantHolds) =>
        PlaybookGen.Valid().Sample(
            playbook => invariantHolds(PlayContext.Of(playbook)), iter: Samples);

    // Every position a walk passes through is asserted, not only the one it stops at.
    private static PlayState Step(PlayContext context, PlayState state, Command command)
    {
        var stepped = Runner.Step(context, state, command).State;

        AssertAddressable(context, stepped.Position);

        return stepped;
    }

    // The walk sends whatever the stage it reached calls for, so a run that stopped to hand the
    // host work carries on rather than ending the walk where the first effect is drawn.
    private static Command? MovesOnFrom(Position position) => position switch
    {
        AtNode => new Next(),
        AwaitingDone => new Done(),
        _ => null,
    };

    private static void AssertAddressable(PlayContext context, Position position)
    {
        // Two stages name a node, and a walk standing outside the document at either of them is
        // the same defect.
        var node = position switch
        {
            AtNode at => at.Node,
            AwaitingDone waiting => waiting.Node,
            _ => (int?)null,
        };

        if (node is { } addressable)
        {
            Assert.InRange(addressable, 0, context.Playbook.Nodes.Length - 1);
        }
    }
}
