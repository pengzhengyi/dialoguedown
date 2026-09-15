namespace DialogueDown.Conformance.Authoring;

/// <summary>
/// A refused case's <c>broken:</c> block is malformed, which is a bug in the corpus rather than in a
/// runtime.
/// </summary>
/// <remarks>
/// Kept distinct from the exception a reader throws, as <see cref="InvalidFixtureException"/> is, so
/// a failing run says whether the corpus is wrong or the reader is.
/// </remarks>
public sealed class MalformedBrokenBlockException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedBrokenBlockException"/> class.
    /// </summary>
    /// <param name="message">What is wrong, naming the offending value.</param>
    public MalformedBrokenBlockException(string message)
        : base(message)
    {
    }
}
