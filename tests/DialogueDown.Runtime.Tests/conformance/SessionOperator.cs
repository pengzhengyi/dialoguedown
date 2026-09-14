using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Drives a runner for one session, holding the events nobody has read yet.</summary>
/// <remarks>
/// Owns the only state a session run needs to track between one session entry and the next: where
/// the run stands, and what it has said that the session has not yet read.
/// </remarks>
internal sealed class SessionOperator
{
    private readonly PlayContext _context;
    private readonly Queue<Event> _unread = new();

    /// <summary>Initializes a new instance of the <see cref="SessionOperator"/> class.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    public SessionOperator(PlayContext context) => _context = context;

    /// <summary>Gets where the run currently stands.</summary>
    public PlayState State { get; private set; } = PlayState.Initial;

    /// <summary>Gets how many events the run has said that nobody has read yet.</summary>
    public int UnreadEventCount => _unread.Count;

    /// <summary>Starts the run, queuing whatever it says.</summary>
    public void Start() => Act(new Start());

    /// <summary>
    /// Recognizes a session's <c>send</c> and steps the runner, queuing whatever it says.
    /// </summary>
    /// <param name="send">The session entry naming what to send.</param>
    /// <returns>
    /// A conforming outcome once the runner was stepped, or what stopped the send when the message
    /// is not yet something anything can send.
    /// </returns>
    public SessionOutcome Send(Send send)
    {
        if (Commands.Read(send) is not { } command)
        {
            return SessionOutcome.NotYetRunnable($"nothing sends {send.Message.ToJsonString()} yet");
        }

        Act(command);
        return SessionOutcome.Conformed();
    }

    /// <summary>Reads the next event nobody has read yet.</summary>
    /// <returns>The event, or <see langword="null"/> if the run has fallen silent.</returns>
    public Event? NextEvent() => _unread.TryDequeue(out var result) ? result : null;

    private void Act(Command command)
    {
        var step = Runner.Step(_context, State, command);
        State = step.State;
        foreach (var happened in step.Events)
        {
            _unread.Enqueue(happened);
        }
    }
}
