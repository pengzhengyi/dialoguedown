using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Toml;

/// <summary>
/// Maps a Tomlyn syntax span to a <see cref="ConfigurationSourceLocation"/>, converting Tomlyn's
/// zero-based line and column to the one-based location a reader expects, so the public location
/// type takes no Tomlyn dependency.
/// </summary>
internal static class TomlLocation
{
    public static ConfigurationSourceLocation From(SourceSpan span) =>
        new(span.FileName!, span.Start.Line + 1, span.Start.Column + 1);
}
