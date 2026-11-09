using DialogueDown.ConfigurationLoader.Readers;
using DialogueDown.Diagnostics;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredDiagnosticsReaderTests
{
    [Fact]
    public void Read_NullRoot_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ConfiguredDiagnosticsReader().Read(null!));

    [Fact]
    public void Read_NoDiagnosticsSection_DisablesNothing() =>
        Assert.Empty(Parse("""
            [[speakers]]
            name = "Alice"
            """).DisabledDiagnostics);

    [Theory]
    [InlineData("[diagnostics]")]
    [InlineData("""
        [diagnostics]
        disable = []
        """)]
    public void Read_NoCodes_DisablesNothing(string toml) =>
        Assert.Empty(Parse(toml).DisabledDiagnostics);

    [Fact]
    public void Read_DisableList_DisablesTheCodes() =>
        Assert.Equal(
            ["DLG1114", "DLG3002"],
            Parse("""
                [diagnostics]
                disable = ["DLG3002", "DLG1114"]
                """).DisabledDiagnostics.Order());

    [Theory]
    [InlineData("""diagnostics.disable = ["DLG3002"]""")]
    [InlineData("""diagnostics = { disable = ["DLG3002"] }""")]
    public void Read_DiagnosticsWrittenInline_DisablesTheCodes(string toml) =>
        Assert.Equal(["DLG3002"], Parse(toml).DisabledDiagnostics);

    [Fact]
    public void Read_LowerCaseCode_IsDisabledUpperCase() =>
        Assert.Equal(
            ["DLG3002"],
            Parse("""
                [diagnostics]
                disable = ["dlg3002"]
                """).DisabledDiagnostics);

    [Fact]
    public void Read_UnknownCode_IsRejectedAtTheCode() =>
        AssertRejects(
            """
            [diagnostics]
            disable = ["DLG302"]
            """,
            "Unknown diagnostic code 'DLG302'. Check the code against the error-code reference: "
                + DiagnosticDocumentation.ReferencePage,
            line: 2,
            column: 12); // the opening quote of "DLG302"

    [Fact]
    public void Read_ErrorCode_IsRejectedAtTheCode() =>
        AssertRejects(
            """
            [diagnostics]
            disable = ["DLG3002", "DLG2001"]
            """,
            "DLG2001 is an error and cannot be disabled; only info and warning codes can. "
                + "Fix the problem it reports instead.",
            line: 2,
            column: 23); // the opening quote of "DLG2001"

    [Fact]
    public void Read_NonStringCode_IsRejectedAtTheCode() =>
        AssertRejects(
            """
            [diagnostics]
            disable = [3002]
            """,
            "Each code in 'disable' must be a string, such as \"DLG3002\".",
            line: 2,
            column: 12);

    [Fact]
    public void Read_DisableNotAnArray_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [diagnostics]
            disable = "DLG3002"
            """,
            "'disable' must be an array of diagnostic codes.",
            line: 2,
            column: 11);

    [Fact]
    public void Read_UnknownKey_IsRejectedAtTheKey() =>
        AssertRejects(
            """
            [diagnostics]
            ignore = ["DLG3002"]
            """,
            "Unknown diagnostics setting 'ignore'. Use 'disable'.",
            line: 2,
            column: 1);

    [Fact]
    public void Read_DiagnosticsAsAnArrayOfTables_IsRejected() =>
        AssertRejects(
            """
            [[diagnostics]]
            disable = ["DLG3002"]
            """,
            "'diagnostics' must be a table, such as a [diagnostics] line with disable = [\"DLG3002\"].",
            line: 1,
            column: 1);

    [Fact]
    public void Read_ANestedTable_IsAnUnknownKey() =>
        AssertRejects(
            """
            [diagnostics.style]
            disable = ["DLG3002"]
            """,
            "Unknown diagnostics setting 'style'. Use 'disable'.",
            line: 1,
            column: 14);
}
