using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DisabledDiagnosticCodesTests
{
    [Theory]
    [InlineData("DLG1114", "DLG1114")] // an info
    [InlineData("DLG3002", "DLG3002")] // a warning
    [InlineData("dlg3002", "DLG3002")]
    public void Check_AnInfoOrWarningCodeInAnyCase_IsValidAndUpperCase(string code, string expected) =>
        Assert.Equal(new DisabledCodeCheck(expected, Problem: null), DisabledDiagnosticCodes.Check(code));

    [Theory]
    [InlineData("DLG2001")]
    [InlineData("dlg2001")]
    public void Check_AnErrorCodeInAnyCase_IsUpperCaseAndSaysWhyItCannotBeDisabled(string code) =>
        Assert.Equal(
            new DisabledCodeCheck(
                "DLG2001",
                "DLG2001 is an error and cannot be disabled; only info and warning codes can. "
                    + "Fix the problem it reports instead."),
            DisabledDiagnosticCodes.Check(code));

    [Theory]
    [InlineData("DLG302")]
    [InlineData("DLG9999")]
    [InlineData(" DLG3002")] // not trimmed
    [InlineData("")]
    public void Check_AnUnknownCode_KeepsTheCodeAsWrittenAndPointsAtTheErrorCodesPage(string code) =>
        Assert.Equal(
            new DisabledCodeCheck(
                code,
                $"Unknown diagnostic code '{code}'. Check the code against the error-code reference: "
                    + DiagnosticDocumentation.ReferencePage),
            DisabledDiagnosticCodes.Check(code));

    [Fact]
    public void Check_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DisabledDiagnosticCodes.Check(null!));
}
