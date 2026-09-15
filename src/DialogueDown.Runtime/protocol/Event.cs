namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// One thing that happened in a run.
/// </summary>
/// <remarks>
/// Named as the conformance corpus names them, so a fixture, a harness, and the code that
/// satisfies them read in one vocabulary. Most report something that has already happened and are
/// named in the past tense. A <see cref="Request"/> is the one that does not: it asks the host
/// for something and is named for what it asks, because the run does not go past it until the
/// host answers.
/// <para>
/// The runner produces events in order and nothing more. Handing them to a view, a backlog, and a
/// log is consumption policy, and it belongs to whoever drives the run: a subscriber list here
/// would be state, and state is what stops a step returning the same result for the same
/// arguments.
/// </para>
/// </remarks>
public abstract record Event
{
    private protected Event()
    {
    }
}
