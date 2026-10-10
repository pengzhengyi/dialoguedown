namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Why a parse failed: the grammar's own message, or a short reason such as
/// <c>no speaker prefix</c>.
/// </summary>
/// <remarks>
/// A failure is usually not a mistake in the script. A parser tries one shape, and when it
/// fails the caller tries the next, so the reason is never shown to a writer. It is kept for
/// whoever debugs the transpiler, where it says which shape failed and at what position.
/// </remarks>
internal readonly record struct ParseError(string Detail);
