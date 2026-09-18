namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The world answers with words.
/// </summary>
/// <remarks>
/// What a query in speech is owed. A writer puts a key in the middle of a line —
/// <c>Alice: Hello, `"playerName"`.</c> — because the words belong to the running game rather
/// than to the script: the player chose them, or another scene set them.
/// <para>
/// The words take the query's place before the line is said, so a run whose <c>playerName</c> is
/// <c>Robin</c> says <c>Hello, Robin.</c> Everything that reads the line afterwards — a host
/// rendering it, a log replaying it, a fixture comparing it — sees that same sentence.
/// </para>
/// </remarks>
/// <param name="Value">The words the world gave.</param>
public sealed record TextAnswer(string Value) : Answer;
