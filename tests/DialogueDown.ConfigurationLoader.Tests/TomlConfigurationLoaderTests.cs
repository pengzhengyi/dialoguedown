using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;
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
    public void Parse_EmptyFile_ReturnsDefaultOptions()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse(string.Empty, SourceName);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Parse_OnlyComments_ReturnsDefaultOptions()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse("""
            # mode = "best-effort"
            """, SourceName);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Parse_EverySection_AppliesAll()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse("""
            mode = "best-effort"

            [[speakers]]
            name = "Alice"

            [markdown.unmodeled]
            table = "keep"
            """, SourceName);

        Assert.Equal(CompilationMode.BestEffort, options.Mode);
        Assert.Equal("Alice", AssertOnlySpeaker(options.Speakers).Name);
        Assert.Equal(
            UnmodeledNodeHandling.Keep, options.UnmodeledMarkdown[UnmodeledNodeKind.Table]);
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

        Assert.Equal("Alice", AssertOnlySpeaker(options.Speakers).Name);
    }

    [Fact]
    public void Load_InvalidFile_NamesThePathInTheLocation()
    {
        using var tree = new TempTree();
        var path = tree.File("dialogue.toml", """
            mode = "turbo"
            """);

        var error = Assert.Throws<Errors.DialogueConfigurationException>(() => TomlConfigurationLoader.Load(path));

        Assert.Equal(new ConfigurationSourceLocation(path, 1, 8), error.Location);
    }

    [Fact]
    public void Parse_MalformedToml_IsRejectedAtTheSyntaxError()
    {
        // The value is missing after the '=', a TOML syntax error on line 3.
        var error = AssertRejects("""
            [[speakers]]
            name = "Alice"
            broken =
            """);

        Assert.Equal(3, error.Location.Line);
        Assert.NotEqual(string.Empty, error.Message);
    }
}
