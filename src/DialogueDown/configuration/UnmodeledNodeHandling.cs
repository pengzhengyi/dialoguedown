namespace DialogueDown.Configuration;

/// <summary>
/// What becomes of an unmodeled Markdown construct — one of the two words a project writes in the
/// <c>[markdown.unmodeled]</c> section of its <c>dialogue.toml</c>.
/// </summary>
public enum UnmodeledNodeHandling
{
    /// <summary>
    /// The construct's source text becomes dialogue text, exactly as written. Its text is kept,
    /// not its structure: a kept table becomes the characters the author typed, because the
    /// front-end does not model a table.
    /// </summary>
    Keep,

    /// <summary>The construct is left out of the dialogue entirely, like a comment.</summary>
    Ignore,
}
