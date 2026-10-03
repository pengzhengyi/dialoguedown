using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Readers;
using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads a DialogueDown project's <c>dialogue.toml</c> into a <see cref="CompilerOptions"/> so the
/// engine-agnostic core never takes a TOML dependency. It reads the speakers, the compilation
/// mode, and how unmodeled Markdown is handled; a config that sets none of them yields
/// <see cref="CompilerOptions.Default"/>.
/// </summary>
public static class TomlConfigurationLoader
{
    /// <summary>
    /// Reads and parses the <c>dialogue.toml</c> at <paramref name="path"/>, which also names the
    /// source in diagnostics. A missing file throws the I/O exception from reading it, such as
    /// <see cref="FileNotFoundException"/>.
    /// </summary>
    public static CompilerOptions Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Parse(File.ReadAllText(path), path);
    }

    /// <summary>
    /// Parses <paramref name="toml"/> into a <see cref="CompilerOptions"/>. The required
    /// <paramref name="sourceName"/> names the source in diagnostics (a file path, or a synthetic
    /// name for in-memory config). Use this to parse an edited buffer without a disk round-trip.
    /// </summary>
    public static CompilerOptions Parse(string toml, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(toml);
        ArgumentNullException.ThrowIfNull(sourceName);

        DocumentSyntax document = new TomlDocumentParser(sourceName).Parse(toml);
        IReadOnlyList<ConfiguredSpeaker> speakers = new ConfiguredSpeakerReader().Read(document);
        CompilationMode? mode = new ConfiguredModeReader().Read(document);
        IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> unmodeled =
            new ConfiguredUnmodeledReader().Read(document);

        if (speakers.Count == 0 && mode is null && unmodeled.Count == 0)
        {
            return CompilerOptions.Default;
        }

        return new CompilerOptions(
            mode ?? CompilerOptions.Default.Mode,
            speakers,
            unmodeled);
    }
}
