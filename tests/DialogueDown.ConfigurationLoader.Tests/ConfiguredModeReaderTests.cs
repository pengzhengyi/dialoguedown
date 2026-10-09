using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Readers;
using DialogueDown.ConfigurationLoader.Tests.Support;
using Tomlyn.Syntax;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredModeReaderTests
{
    [Fact]
    public void Read_EmptyDocument_ReturnsNull() =>
        Assert.Null(Read(string.Empty));

    [Fact]
    public void Read_OnlySpeakers_ReturnsNull()
    {
        var mode = Read("""
            [[speakers]]
            name = "Alice"
            """);

        Assert.Null(mode);
    }

    [Theory]
    [InlineData("stage-boundary", CompilationMode.StageBoundary)]
    [InlineData("best-effort", CompilationMode.BestEffort)]
    public void Read_ASettableMode_ReturnsIt(string value, CompilationMode expected) =>
        Assert.Equal(expected, Read($"mode = \"{value}\""));

    [Fact]
    public void Read_QuotedModeKey_IsEquivalentToBareKey()
    {
        var mode = Read("""
            "mode" = "best-effort"
            """);

        Assert.Equal(CompilationMode.BestEffort, mode);
    }

    [Fact]
    public void Read_ModeBeforeSpeakers_IsFound()
    {
        var mode = Read("""
            mode = "best-effort"

            [[speakers]]
            name = "Alice"
            """);

        Assert.Equal(CompilationMode.BestEffort, mode);
    }

    [Fact]
    public void Read_UnknownMode_ThrowsLocated()
    {
        var exception = Reject("""
            mode = "turbo"
            """);

        AssertRejectedAt(exception, line: 1, "turbo", "stage-boundary");
    }

    [Fact]
    public void Read_FailFast_IsRejected()
    {
        // Fail-fast is an embedding contract that throws, not a settable reporting mode.
        var exception = Reject("""
            mode = "fail-fast"
            """);

        AssertMentions(exception, "fail-fast");
    }

    [Fact]
    public void Read_NonStringMode_Throws()
    {
        var exception = Reject("""
            mode = 42
            """);

        AssertMentions(exception, "string");
    }

    [Fact]
    public void Read_UnrelatedRootKey_IsIgnored()
    {
        var mode = Read("""
            title = "My project"
            """);

        // Root keys other than 'mode' stay lenient so new settings can be added without breaking
        // older loaders.
        Assert.Null(mode);
    }

    [Fact]
    public void Read_DottedModeKey_IsNotReadAsMode()
    {
        var mode = Read("""
            mode.strategy = "best-effort"
            """);

        // A dotted key keeps its full name ('mode.strategy'), so it is an unrelated root key, not
        // the flat 'mode' setting — ignored rather than misread.
        Assert.Null(mode);
    }

    private static CompilationMode? Read(string toml) =>
        TomlConfigReading.Read(toml, ReadMode);

    private static DialogueConfigurationException Reject(string toml) =>
        TomlConfigReading.Reject(toml, ReadMode);

    private static CompilationMode? ReadMode(DocumentSyntax document) =>
        new ConfiguredModeReader().Read(document);
}
