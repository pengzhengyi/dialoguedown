namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What the world says about one key.
/// </summary>
/// <remarks>
/// The world can be asked more than one kind of question, and each kind wants a different kind of
/// answer. <c>Alice.HasKey</c>, guarding a line, wants a yes or a no; <c>playerName</c>, sitting
/// in the middle of a line, wants words. Keeping the kinds apart means a reader asks for the one
/// it needs and is told plainly when it was given another.
/// </remarks>
public abstract record Answer
{
    private protected Answer()
    {
    }
}
