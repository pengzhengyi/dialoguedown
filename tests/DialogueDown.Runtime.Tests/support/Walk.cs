using CsCheck;
using DialogueDown.Playbook;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// A test driver: steps the runner through a sampled playbook from its start, answering everything
/// the run waits on, until the run ends or the step budget runs out.
/// </summary>
/// <param name="Playbook">The playbook walked.</param>
/// <param name="World">The world sampled beside it, answering every key the run asks about.</param>
/// <param name="Pick">Selects an option the player can take at every menu.</param>
/// <param name="Draw">The number from 0 to 1 sent whenever the run asks for a draw.</param>
internal sealed record Walk(PlaybookDocument Playbook, SampledWorld World, int Pick, double Draw)
{
    // A sampled playbook may loop, so a walk stops after this many steps. Passing one node can take
    // five steps: answering what playing it needs, the host finishing a command inside a line,
    // answering what the rest of the line needs, moving on, and answering which way out to take.
    // This passes every node of the largest sampled playbook at least twice.
    private const int MostSteps = 120;

    // Walks sampled per property.
    private const int Samples = 200;

    /// <summary>Walks over playbooks the default reader accepts.</summary>
    /// <returns>The generator.</returns>
    public static Gen<Walk> Generator() =>
        Gen.Select(
            PlaybookGen.Valid(),
            PlaybookGen.Worlds(),
            Gen.Int.NonNegative,
            Draws(),
            (playbook, world, pick, draw) => new Walk(playbook, world, pick, draw));

    /// <summary>Checks every step of every sampled walk, not only the last.</summary>
    /// <param name="holds">
    /// Asserts what must hold at a step, given the context the step ran in.
    /// </param>
    public static void ForEveryStep(Action<PlayContext, StepResult> holds) =>
        Generator().Sample(
            walk =>
            {
                foreach (var (context, stepped) in walk.Steps())
                {
                    holds(context, stepped);
                }
            },
            iter: Samples);

    /// <summary>Every step this walk takes from the playbook's start.</summary>
    /// <returns>
    /// Each step with the context it ran in, until the run ends or the step budget is used. Steps
    /// run lazily, so a caller that stops early runs no further steps.
    /// </returns>
    public IEnumerable<(PlayContext Context, StepResult Stepped)> Steps()
    {
        var context = PlayContext.Of(Playbook);
        var state = PlayState.Initial;
        Command? command = new Start();

        for (var taken = 0; taken < MostSteps && command is not null; taken++)
        {
            var stepped = Runner.Step(context, state, command);

            yield return (context, stepped);

            state = stepped.State;
            command = MovesOnFrom(state.Situation);
        }
    }

    // A number from 0 to 1. Both ends are sampled as cases of their own, because a host may send
    // either.
    private static Gen<double> Draws() => Gen.OneOf(Gen.Const(0.0), Gen.Const(1.0), Gen.Double[0.0, 1.0]);

    // The runner skips a menu with no option the player can take, so a waiting menu always has one.
    // The pick modulo their count selects among them.
    private static int OneThePlayerCanTake(AwaitingChoice waiting, int pick) =>
        waiting.Available[pick % waiting.Available.Length];

    // The command that answers what the run waits on, so the walk carries on past every stop, or
    // null once the run has ended.
    private Command? MovesOnFrom(Situation situation) =>
        situation switch
        {
            AtNode => new Next(),
            AwaitingDone => new Done(),
            AwaitingSupply waiting => World.Answering(waiting.Keys),
            AwaitingChoice waiting => new Choose(OneThePlayerCanTake(waiting, Pick)),
            AwaitingDraw => new Drawn(Draw),
            _ => null,
        };
}
