namespace DialogueDown.Conformance.Authoring;

/// <summary>
/// A readable refusal's leading <c>broken:</c> block: the note naming the deliberate edit and the
/// valid script below it.
/// </summary>
/// <remarks>
/// Part of this repository's authoring checks, not the portable readable contract: a port reads a
/// fixture and the document it names, never the source. The block names the edit on its first
/// line, then a blank line, then the evidence:
/// <code>
/// &lt;!-- broken: it requires a capability no version-0 runtime offers
///
///      "requires": [ "core", "detour" ]
/// --&gt;
/// # The Inn
///
/// Alice: Hello.
/// </code>
/// </remarks>
/// <param name="Note">The one line naming the edit, after <c>broken:</c>.</param>
/// <param name="Script">The valid script below the block, whose compiled document the case edits.</param>
public sealed record BrokenBlock(string Note, string Script)
{
    /// <summary>The marker that opens the block, at the very start of the source.</summary>
    public const string Marker = "<!-- broken:";

    /// <summary>The marker that closes it; the first one in the source ends the block.</summary>
    public const string Closer = "-->";

    /// <summary>
    /// Parses the leading <c>broken:</c> block and returns it with the script below.
    /// </summary>
    /// <param name="source">The source file's text.</param>
    /// <returns>The block's note and the script below it.</returns>
    /// <exception cref="MalformedBrokenBlockException">
    /// The source does not open with a well-formed block.
    /// </exception>
    public static BrokenBlock Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.StartsWith(Marker, StringComparison.Ordinal))
        {
            throw new MalformedBrokenBlockException(
                $"A refused source must open with '{Marker}', but this one opens with '{Head(source)}'.");
        }

        var close = source.IndexOf(Closer, Marker.Length, StringComparison.Ordinal);

        if (close < 0)
        {
            throw new MalformedBrokenBlockException(
                $"A refused source's '{Marker}' block must close with '{Closer}'.");
        }

        var body = source[Marker.Length..close];

        if (body.Contains("<!--", StringComparison.Ordinal))
        {
            throw new MalformedBrokenBlockException(
                "A refused source's block must not hold '<!--', or the parser would nest a comment.");
        }

        var newline = body.IndexOf('\n');

        if (newline < 0 || !IsBlankLine(body[(newline + 1)..]))
        {
            throw new MalformedBrokenBlockException(
                "A refused source's block must name the edit, then a blank line, then its evidence.");
        }

        return new BrokenBlock(body[..newline].Trim(), source[(close + Closer.Length)..].TrimStart('\r', '\n'));
    }

    private static bool IsBlankLine(string afterNote) =>
        afterNote.StartsWith('\n') || afterNote.StartsWith("\r\n", StringComparison.Ordinal);

    private static string Head(string source)
    {
        if (source.Length == 0)
        {
            return "<empty>";
        }

        var lineEnd = source.IndexOf('\n');

        return lineEnd < 0 ? source : source[..lineEnd].TrimEnd('\r');
    }
}
