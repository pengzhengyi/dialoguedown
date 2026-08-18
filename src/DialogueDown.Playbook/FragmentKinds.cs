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
}
