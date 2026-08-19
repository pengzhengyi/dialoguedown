using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlConfigurationLoaderTests
{
    private const string SourceName = "dialogue.toml";

    [Fact]
    public void Parse_NullToml_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TomlConfigurationLoader.Parse(null!, SourceName));
    }

    [Fact]
    public void Parse_NullSourceName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TomlConfigurationLoader.Parse("", null!));
    }

    [Fact]
    public void Parse_NoSpeakers_ReturnsDefaultOptions()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse(string.Empty, SourceName);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Parse_WithSpeakers_WrapsThemInOptions()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            id = "A"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml, SourceName);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
    }

    [Fact]
    public void Parse_WithMode_SetsIt()
    {
        var toml = """
            mode = "best-effort"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml, SourceName);

        Assert.Equal(CompilationMode.BestEffort, options.Mode);
    }

    [Fact]
    public void Parse_NoMode_KeepsTheDefaultMode()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml, SourceName);

        Assert.Equal(CompilationMode.StageBoundary, options.Mode);
    }

    [Fact]
    public void Parse_WithModeAndSpeakers_AppliesBoth()
    {
        var toml = """
            mode = "best-effort"

            [[speakers]]
            name = "Alice"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml, SourceName);

        Assert.Equal(CompilationMode.BestEffort, options.Mode);
        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Parse_InvalidMode_Throws()
    {
        var toml = """
            mode = "turbo"
            """;

        Assert.Throws<DialogueConfigurationException>(
            () => TomlConfigurationLoader.Parse(toml, SourceName));
    }

    [Fact]
    public void Load_NullPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TomlConfigurationLoader.Load(null!));
    }

    [Fact]
    public void Load_ReadsFileIntoOptions()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            """;
        string path = Path.Combine(Path.GetTempPath(), $"dialogue-{Guid.NewGuid():N}.toml");
        File.WriteAllText(path, toml);

        try
        {
            CompilerOptions options = TomlConfigurationLoader.Load(path);

            ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
            Assert.Equal("Alice", speaker.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_WithUnmodeledHandling_WrapsItInOptions()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse("""
            [markdown.unmodeled]
            table      = "keep"
            code-block = "ignore"
            """, SourceName);

        Assert.Equal(
            UnmodeledNodeHandling.Keep, options.UnmodeledMarkdown[UnmodeledNodeKind.Table]);
        Assert.Equal(
            UnmodeledNodeHandling.Ignore, options.UnmodeledMarkdown[UnmodeledNodeKind.CodeBlock]);
    }

    [Fact]
    public void Parse_NoUnmodeledSection_KeepsTheDefaults() =>
        Assert.Empty(TomlConfigurationLoader.Parse("""
            [[speakers]]
            name = "Alice"
            """, SourceName).UnmodeledMarkdown);

    [Fact]
    public void Parse_UnmodeledWithSpeakersAndMode_AppliesAll()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse("""
            mode = "best-effort"

            [[speakers]]
            name = "Alice"

            [markdown.unmodeled]
            table = "keep"
            """, SourceName);

        Assert.Equal(CompilationMode.BestEffort, options.Mode);
        Assert.Single(options.Speakers);
        Assert.Equal(
            UnmodeledNodeHandling.Keep, options.UnmodeledMarkdown[UnmodeledNodeKind.Table]);
    }

    [Fact]
    public void Parse_InvalidUnmodeledKind_Throws() =>
        Assert.Throws<DialogueConfigurationException>(() => TomlConfigurationLoader.Parse("""
            [markdown.unmodeled]
            footnote = "ignore"
            """, SourceName));
}
