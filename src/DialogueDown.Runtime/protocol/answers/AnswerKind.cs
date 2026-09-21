namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What kind of answer a question needs.
/// </summary>
/// <remarks>
/// A guard is answered by a truth, and a query standing in a line is answered by words. The world
/// is asked about a key once and answers it once, so a run has to know which of the two that one
/// answer was meant to be before it can use it.
/// </remarks>
internal enum AnswerKind
{
    /// <summary>A yes or a no, as a condition needs.</summary>
    Boolean,

    /// <summary>Words, as a query standing in a line needs.</summary>
    Text,
}
