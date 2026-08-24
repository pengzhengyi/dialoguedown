namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// A conformance fixture is malformed, which is a bug in the corpus rather than in a runtime.
/// </summary>
/// <remarks>
/// Kept distinct from <see cref="InvalidPlaybookException"/> so a failing run says whether the
/// corpus is wrong or the reader is. Conflating the two would let a broken fixture masquerade as
/// a conformance failure, which is the one thing a corpus must never do.
/// </remarks>
public sealed class InvalidFixtureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFixtureException"/> class.
    /// </summary>
    /// <param name="message">What is wrong, naming the offending value.</param>
    public InvalidFixtureException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFixtureException"/> class.
    /// </summary>
    /// <param name="message">What is wrong, naming the offending value.</param>
    /// <param name="innerException">The parse failure underneath, when there was one.</param>
    public InvalidFixtureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
