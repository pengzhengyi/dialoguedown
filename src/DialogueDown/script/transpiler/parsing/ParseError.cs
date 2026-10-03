namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Why a parse failed, typically the underlying grammar's rendered message. It is the
/// technical detail, kept apart from any author-facing message.
/// </summary>
internal readonly record struct ParseError(string Detail);
