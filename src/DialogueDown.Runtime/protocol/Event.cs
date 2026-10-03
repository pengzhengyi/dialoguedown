namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// One thing that happened in a run.
/// </summary>
/// <remarks>
/// Most events report something that has already happened and are named in the past tense:
/// <see cref="Said"/>, <see cref="Continued"/>, <see cref="Ended"/>, <see cref="Refused"/>. A
/// <see cref="Request"/> (<see cref="Perform"/>, <see cref="Resolve"/>) asks the host for something
/// instead and is named for what it asks, because the run does not go past it until the host
/// answers.
/// </remarks>
public abstract record Event
{
    private protected Event()
    {
    }
}
