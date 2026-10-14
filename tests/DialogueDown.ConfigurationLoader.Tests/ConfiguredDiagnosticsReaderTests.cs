using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Readers;
using DialogueDown.ConfigurationLoader.Tests.Support;
using DialogueDown.Diagnostics;
using Tomlyn.Syntax;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredDiagnosticsReaderTests
{
    [Fact]
    public void Read_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ConfiguredDiagnosticsReader().Read(null!));

    [Fact]
    public void Read_EmptyDocument_DisablesNothing() =>
        Assert.Empty(Read(string.Empty));

    [Fact]
    public void Read_TableWithoutDisable_DisablesNothing() =>
        Assert.Empty(Read("[diagnostics]"));

    [Fact]
    public void Read_EmptyDisable_DisablesNothing() =>
        Assert.Empty(Read("""
            [diagnostics]
            disable = []
            """));

    [Fact]
    public void Read_DisableList_ReturnsTheCodesInOrder() =>
        Assert.Equal(
            ["DLG3002", "DLG1114"],
            Read("""
                [diagnostics]
                disable = ["DLG3002", "DLG1114"]
                """));

    [Fact]
    public void Read_LowerCaseCode_IsReturnedUpperCase() =>
        Assert.Equal(
            ["DLG3002"],
            Read("""
                [diagnostics]
                disable = ["dlg3002"]
                """));

    [Fact]
    public void Read_UnknownCode_ThrowsTheCheckProblemAtTheCode()
    {
        var exception = AssertRejects("""
            [diagnostics]
            disable = ["DLG302"]
            """);

        AssertRejectedAt(exception, line: 2);
        Assert.Equal(12, exception.Location.Column); // the opening quote of "DLG302"
        Assert.Equal(DisabledDiagnosticCodes.Check("DLG302").Problem, exception.Message);
    }

    [Fact]
    public void Read_ErrorCode_ThrowsTheCheckProblemAtTheCode()
    {
        var exception = AssertRejects("""
            [diagnostics]
            disable = ["DLG3002", "DLG2001"]
            """);

        AssertRejectedAt(exception, line: 2);
        Assert.Equal(23, exception.Location.Column); // the opening quote of "DLG2001"
        Assert.Equal(DisabledDiagnosticCodes.Check("DLG2001").Problem, exception.Message);
    }

    [Fact]
    public void Read_NonStringCode_Throws()
    {
        var exception = AssertRejects("""
            [diagnostics]
            disable = [3002]
            """);

        AssertRejectedAt(exception, line: 2, "must be a string");
    }

    [Fact]
    public void Read_DisableNotAnArray_Throws()
    {
        var exception = AssertRejects("""
            [diagnostics]
            disable = "DLG3002"
            """);

        AssertRejectedAt(exception, line: 2, "'disable' must be an array");
    }

    [Fact]
    public void Read_UnknownKey_Throws()
    {
        var exception = AssertRejects("""
            [diagnostics]
            ignore = ["DLG3002"]
            """);

        AssertRejectedAt(exception, line: 2, "Unknown key 'ignore'", "'disable'");
    }

    [Theory]
    [InlineData("""diagnostics.disable = ["DLG3002"]""")]
    [InlineData("""diagnostics = { disable = ["DLG3002"] }""")]
    public void Read_DiagnosticsOutsideATable_Throws(string toml)
    {
        // Either shape would otherwise be skipped by a reader that only looks for the table, so the
        // codes would silently stay enabled.
        var exception = AssertRejects(toml);

        AssertRejectedAt(exception, line: 1, "[diagnostics]");
    }

    [Theory]
    [InlineData("[[diagnostics]]")]
    [InlineData("[diagnostics.style]")]
    public void Read_DiagnosticsAsAnotherTable_Throws(string header)
    {
        var exception = AssertRejects($"""
            {header}
            disable = ["DLG3002"]
            """);

        AssertRejectedAt(exception, line: 1, "[diagnostics]");
    }

    private static IReadOnlyList<string> Read(string toml) =>
        TomlConfigReading.Read(toml, ReadDiagnostics);

    private static DialogueConfigurationException AssertRejects(string toml) =>
        TomlConfigReading.AssertRejects(toml, ReadDiagnostics);

    private static IReadOnlyList<string> ReadDiagnostics(DocumentSyntax document) =>
        new ConfiguredDiagnosticsReader().Read(document);
}
