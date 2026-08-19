using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Readers;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredModeReaderTests
{
    [Fact]
    public void Read_EmptyDocument_ReturnsNull() =>
        Assert.Null(Read(string.Empty));

    [Fact]
    public void Read_OnlySpeakers_ReturnsNull() =>
        Assert.Null(Read("""
            [[speakers]]
            name = "Alice"
            """));

    [Theory]
    [InlineData("stage-boundary", CompilationMode.StageBoundary)]
    [InlineData("best-effort", CompilationMode.BestEffort)]
    public void Read_ASettableMode_ReturnsIt(string value, CompilationMode expected) =>
        Assert.Equal(expected, Read($"mode = \"{value}\""));

    [Fact]
    public void Read_QuotedModeKey_IsEquivalentToBareKey() =>
        Assert.Equal(CompilationMode.BestEffort, Read("""
            "mode" = "best-effort"
            """));

    [Fact]
    public void Read_ModeBeforeSpeakers_IsFound() =>
        Assert.Equal(CompilationMode.BestEffort, Read("""
            mode = "best-effort"

            [[speakers]]
            name = "Alice"
            """));

    [Fact]
    public void Read_UnknownMode_ThrowsLocated()
    {
        var exception = Reject("""
            mode = "turbo"
            """);

        Assert.Equal(1, exception.Location.Line);
        Assert.Contains("turbo", exception.Message);
        Assert.Contains("stage-boundary", exception.Message);
    }

    [Fact]
    public void Read_FailFast_IsRejected()
    {
        // Fail-fast is an embedding contract that throws, not a settable reporting mode.
        var exception = Reject("""
            mode = "fail-fast"
            """);

        Assert.Contains("fail-fast", exception.Message);
    }

    [Fact]
    public void Read_NonStringMode_Throws()
    {
        var exception = Reject("""
            mode = 42
            """);

        Assert.Contains("string", exception.Message);
    }

    [Fact]
    public void Read_UnrelatedRootKey_IsIgnored() =>
        // Root keys other than 'mode' stay lenient so new settings can be added without breaking
        // older loaders.
        Assert.Null(Read("""
            title = "My project"
            """));

    [Fact]
    public void Read_DottedModeKey_IsNotReadAsMode() =>
        // A dotted key keeps its full name ('mode.strategy'), so it is an unrelated root key, not
        // the flat 'mode' setting — ignored rather than misread.
        Assert.Null(Read("""
            mode.strategy = "best-effort"
            """));

    private static CompilationMode? Read(string toml) =>
        TomlConfigReading.Read(toml, ReadMode);

    private static DialogueConfigurationException Reject(string toml) =>
        TomlConfigReading.Reject(toml, ReadMode);

    private static CompilationMode? ReadMode(DocumentSyntax document) =>
        new ConfiguredModeReader().Read(document);
}
