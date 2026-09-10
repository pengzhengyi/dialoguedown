using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Drives a runner for one session, buffering the replies nobody has checked yet.</summary>
/// <remarks>
/// Owns the only state a session run needs to track between one session entry and the next: where
/// the run stands, and what it has said that the session has not yet consumed.
/// </remarks>
internal sealed class SessionOperator
{
    private readonly PlayContext _context;
    private readonly Queue<Event> _pending = new();

    /// <summary>Initializes a new instance of the <see cref="SessionOperator"/> class.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    public SessionOperator(PlayContext context) => _context = context;

    /// <summary>Gets where the run currently stands.</summary>
    public PlayState State { get; private set; } = PlayState.Initial;

    /// <summary>Gets how many replies are queued but not yet checked.</summary>
    public int PendingCount => _pending.Count;

    /// <summary>Starts the run, queuing whatever it says.</summary>
    public void Start() => Act(new Start());

    /// <summary>
    /// Recognizes a session's <c>send</c> and steps the runner, queuing the reply.
    /// </summary>
    /// <param name="send">The session entry naming what to send.</param>
    /// <returns>
    /// An outcome only when the message itself is not yet something anything can send;
    /// <see langword="null"/> when the runner was stepped.
    /// </returns>
    public SessionOutcome? Send(Send send)
    {
        if (Commands.Read(send.Message) is not { } command)
        {
            return SessionOutcome.NotYetRunnable($"nothing sends {send.Message.ToJsonString()} yet");
        }

        Act(command);
        return null;
    }

    /// <summary>The next reply nobody has checked yet.</summary>
    /// <returns>The reply, or <see langword="null"/> if the run has fallen silent.</returns>
    public Event? NextReply() => _pending.TryDequeue(out var result) ? result : null;

    private void Act(Command command)
    {
        var step = Runner.Step(_context, State, command);
        State = step.State;
        foreach (var reply in step.Events)
        {
            _pending.Enqueue(reply);
        }
    }
}
