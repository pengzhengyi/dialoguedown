namespace DialogueDown.Playbook;

/// <summary>
/// Thrown when a playbook cannot be played as written.
/// </summary>
/// <remarks>
/// A message names the offending value when there is one, since it reaches whoever ran a compile
/// or launched a game and "invalid playbook" alone tells them nothing they can act on.
/// </remarks>
public sealed class InvalidPlaybookException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPlaybookException"/> class.
    /// </summary>
    /// <param name="message">What is wrong, naming the offending value.</param>
    public InvalidPlaybookException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPlaybookException"/> class.
    /// </summary>
    /// <param name="message">What is wrong, naming the offending value.</param>
    /// <param name="innerException">The parse failure underneath, when there was one.</param>
    public InvalidPlaybookException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
