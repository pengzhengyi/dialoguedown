using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Readers;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredModeReaderTests
{
    [Fact]
    public void Read_NullRoot_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ConfiguredModeReader().Read(null!));

    [Fact]
    public void Read_NoMode_KeepsTheDefaultMode()
    {
        var options = Parse("""
            [[speakers]]
            name = "Alice"
            """);

        Assert.Equal(CompilationMode.StageBoundary, options.Mode);
    }

    [Theory]
    [InlineData("stage-boundary", CompilationMode.StageBoundary)]
    [InlineData("best-effort", CompilationMode.BestEffort)]
    public void Read_ASettableMode_SetsIt(string value, CompilationMode expected) =>
        Assert.Equal(expected, Parse($"mode = \"{value}\"").Mode);

    [Fact]
    public void Read_QuotedModeKey_IsEquivalentToBareKey()
    {
        var options = Parse("""
            "mode" = "best-effort"
            """);

        Assert.Equal(CompilationMode.BestEffort, options.Mode);
    }

    [Fact]
    public void Read_ModeBeforeSpeakers_IsFound()
    {
        var options = Parse("""
            mode = "best-effort"

            [[speakers]]
            name = "Alice"
            """);

        Assert.Equal(CompilationMode.BestEffort, options.Mode);
    }

    [Fact]
    public void Read_UnknownMode_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            mode = "turbo"
            """,
            "Unknown mode 'turbo'. Use 'stage-boundary' or 'best-effort'.",
            line: 1,
            column: 8);

    [Fact]
    public void Read_FailFast_IsRejected() =>
        // Fail-fast is an embedding contract that throws, not a settable reporting mode.
        AssertRejects(
            """
            mode = "fail-fast"
            """,
            "Unknown mode 'fail-fast'. Use 'stage-boundary' or 'best-effort'.",
            line: 1,
            column: 8);

    [Fact]
    public void Read_NonStringMode_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            mode = 42
            """,
            "'mode' must be a string.",
            line: 1,
            column: 8);

    [Fact]
    public void Read_DottedModeKey_IsAModeThatIsNotAString() =>
        // A dotted key is a nested table, so this sets 'mode' to { strategy = "best-effort" }.
        AssertRejects(
            """
            mode.strategy = "best-effort"
            """,
            "'mode' must be a string.",
            line: 1,
            column: 1);
}
