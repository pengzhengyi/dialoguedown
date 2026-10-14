using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCodesTests
{
    [Fact]
    public void All_DescribesEveryCatalogDescriptor_InCodeOrder()
    {
        var expected = DiagnosticCatalog.All
            .Select(descriptor => new DiagnosticCodeInfo(
                descriptor.Code, descriptor.Title, descriptor.Category, descriptor.DefaultSeverity))
            .OrderBy(info => info.Code, StringComparer.Ordinal);

        Assert.Equal(expected, DiagnosticCodes.All);
    }

    [Theory]
    [InlineData("DLG3002")]
    [InlineData("dlg3002")]
    public void Find_AKnownCodeInAnyCase_ReturnsItsUpperCaseDescription(string code)
    {
        var info = DiagnosticCodes.Find(code);

        Assert.Equal(
            new DiagnosticCodeInfo(
                "DLG3002", "Deeply nested choice branch", DiagnosticCategory.Style, DiagnosticSeverity.Warning),
            info);
    }

    [Theory]
    [InlineData("DLG302")]
    [InlineData("DLG9999")]
    [InlineData(" DLG3002")]
    [InlineData("")]
    public void Find_AnUnknownCode_ReturnsNull(string code) =>
        Assert.Null(DiagnosticCodes.Find(code));

    [Fact]
    public void Find_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DiagnosticCodes.Find(null!));

    [Theory]
    [InlineData("DLG1114", true)] // info
    [InlineData("DLG3002", true)] // warning
    [InlineData("DLG2001", false)] // error
    [InlineData("DLG9999", false)] // unknown
    public void IsOptional_IsTrueOnlyForAKnownInfoOrWarningCode(string code, bool expected) =>
        Assert.Equal(expected, DiagnosticCodes.IsOptional(code));
}
