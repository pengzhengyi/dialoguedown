namespace DialogueDown.Diagnostics;

/// <summary>
/// A one-based position in the source text: a <see cref="Line"/> and a <see cref="Column"/>,
/// counted in UTF-16 code units. It formats as <c>line,column</c>, which a tool prefixes with the
/// file to read as <c>file(line,column)</c>.
/// </summary>
public readonly record struct LinePosition(int Line, int Column)
{
    /// <summary>Formats the position as <c>line,column</c>.</summary>
    public override string ToString() => $"{Line},{Column}";
}
