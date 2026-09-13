namespace DialogueDown.Visualization.Display;

/// <summary>
/// A speaker tag as the report draws it: its <see cref="Name"/>, an optional <see cref="Value"/>,
/// and whether it is <see cref="Reserved"/> (a name DialogueDown owns, such as <c>default</c>)
/// rather than one the writer invented.
/// </summary>
/// <remarks>
/// Shared by every surface that shows a tag — the Config tab, the Semantic Model, and the
/// Playbook — so one tag is drawn the same capsule wherever a reader meets it. Keeping the name
/// and value apart is what lets the client color by identity and copy the tag as written.
/// </remarks>
public sealed record TagView(string Name, string? Value, bool Reserved);
