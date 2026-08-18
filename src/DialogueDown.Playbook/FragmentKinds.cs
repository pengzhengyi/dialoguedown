namespace DialogueDown.Playbook;

/// <summary>
/// The <c>kind</c> values that tag a speech fragment on the wire.
/// </summary>
/// <remarks>
/// A wire value is part of the format contract, so it lives here rather than being spelled at
/// each use: renaming one is then a single edit, and a reader, a test, and the schema can all
/// name the same constant instead of repeating a string literal.
/// </remarks>
public static class FragmentKinds
{
    /// <summary>Plain words, exactly as the writer typed them.</summary>
    public const string Text = "text";

    /// <summary>Emphasis wrapping more speech.</summary>
    public const string Styled = "styled";

    /// <summary>A link, carrying the speech that stands in for it.</summary>
    public const string Link = "link";

    /// <summary>An image, carrying the speech that describes it.</summary>
    public const string Image = "image";

    /// <summary>A hard wrap the writer asked for.</summary>
    public const string Break = "break";

    /// <summary>A read of game state spliced into speech.</summary>
    public const string Query = "query";

    /// <summary>A command written as a plain phrase.</summary>
    public const string DefaultCommand = "default-command";

    /// <summary>A named command with arguments.</summary>
    public const string CustomCommand = "custom-command";

    /// <summary>An annotation attached at a point in a line.</summary>
    public const string Tag = "tag";
}
