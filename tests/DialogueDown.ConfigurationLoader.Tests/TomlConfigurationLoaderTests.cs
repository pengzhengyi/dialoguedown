using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.TestSupport;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfiguredSpeakerAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlConfigurationLoaderTests
{
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

        var speaker = AssertOnlySpeaker(options.Speakers);
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
        var speaker = AssertOnlySpeaker(options.Speakers);
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
        using var tree = new TempTree();
        var path = tree.File("dialogue.toml", """
            [[speakers]]
            name = "Alice"
            """);

        CompilerOptions options = TomlConfigurationLoader.Load(path);

        var speaker = AssertOnlySpeaker(options.Speakers);

        Assert.Equal("Alice", speaker.Name);
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
    public void Parse_NoUnmodeledSection_KeepsTheDefaults()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse("""
            [[speakers]]
            name = "Alice"
            """, SourceName);

        Assert.Empty(options.UnmodeledMarkdown);
    }

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
        AssertOnlySpeaker(options.Speakers);
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
