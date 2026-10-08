namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Input to a parser: the <see cref="Text"/> to read, and the absolute
/// <see cref="Position"/> that its first character occupies in the source.
/// Parsing always begins at the start of <see cref="Text"/>; <see cref="Position"/>
/// is only an anchor, so matches report their range in absolute source coordinates.
/// </summary>
internal readonly record struct ParseInput(string Text, int Position)
{
    /// <summary>The full range this input covers in the source.</summary>
    public TextRange Range => new(Position, Text.Length);

    /// <summary>
    /// The input remaining after consuming <paramref name="by"/> characters: the
    /// text past that point, anchored at the advanced position. Composites use this
    /// to run the next parser so ranges stay absolute. <paramref name="by"/> must be
    /// within <c>[0, Text.Length]</c>; anything outside throws, so a miscounted parser
    /// fails at once instead of being clamped.
    /// </summary>
    public ParseInput Advance(int by) => by switch
    {
        < 0 => throw new ArgumentOutOfRangeException(
            nameof(by), by, "Cannot advance by a negative number of characters."),
        0 => this,
        _ when by > Text.Length => throw new ArgumentOutOfRangeException(
            nameof(by), by, $"Cannot advance {by} characters past the {Text.Length}-character input."),
        _ => new ParseInput(Text[by..], Position + by),
    };
}
