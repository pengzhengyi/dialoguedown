namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Call-site sugar for driving a <see cref="SessionOperator"/> in tests.</summary>
internal static class SessionOperatorTestExtensions
{
    /// <summary>Sends a bare string command, e.g. <c>op.SendCommand("next")</c>.</summary>
    public static SessionOutcome SendCommand(this SessionOperator op, string command) =>
        op.Send(SessionEntries.SentCommand(command));
}
