using System.Diagnostics.CodeAnalysis;

namespace DialogueDown.Diagnostics;

/// <summary>Whether a project may disable one diagnostic code, and why not when it may not.</summary>
/// <param name="Code">The code upper-case when the compiler reports it, otherwise as written.</param>
/// <param name="Problem">The sentence a user reads when the code cannot be disabled; null when it can.</param>
public readonly record struct DisabledCodeCheck(string Code, string? Problem)
{
    /// <summary>Whether the code can be disabled, which is when there is no <see cref="Problem"/>.</summary>
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsValid => Problem is null;
}
