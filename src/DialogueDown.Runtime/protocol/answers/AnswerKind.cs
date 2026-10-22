namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What kind of answer a question needs.
/// </summary>
/// <remarks>
/// A guard is answered by a truth, a query in a line or an option's label by words, and a key
/// weight by a number. The world is asked about a key once and answers it once, so a run has to
/// know which of these that one answer was meant to be before it can use it.
/// </remarks>
internal enum AnswerKind
{
    /// <summary>A yes or a no, as a condition needs.</summary>
    Boolean,

    /// <summary>Words, as a query in a line or an option's label needs.</summary>
    Text,

    /// <summary>A number, as a key weight needs.</summary>
    Number,
}
