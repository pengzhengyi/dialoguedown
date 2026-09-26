namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Telling the kinds of answer apart, and saying which is which.
/// </summary>
internal static class AnswerKindExtensions
{
    /// <summary>The kind of answer this is.</summary>
    /// <param name="answer">The answer to read.</param>
    /// <returns>The kind it carries.</returns>
    /// <exception cref="NotSupportedException">
    /// The answer is of a kind nothing has been taught to tell apart.
    /// </exception>
    public static AnswerKind Kind(this Answer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return answer switch
        {
            BooleanAnswer => AnswerKind.Boolean,
            TextAnswer => AnswerKind.Text,

            // Every kind is named above, so a kind added to the protocol arrives here as a failure
            // rather than as an answer nothing can tell apart from the others.
            _ => throw new NotSupportedException($"No kind is named for {answer.GetType().Name}."),
        };
    }

    /// <summary>What a kind is called, for a run explaining itself.</summary>
    /// <param name="kind">The kind to word.</param>
    /// <returns>The words a refusal uses for it, reading as part of a sentence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">No wording is written for the kind.</exception>
    public static string Describe(this AnswerKind kind) => kind switch
    {
        AnswerKind.Boolean => "a truth",
        AnswerKind.Text => "words",
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "No wording is written for this kind."),
    };
}
