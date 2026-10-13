using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCodeInfoTests
{
    [Theory]
    [InlineData(DiagnosticSeverity.Info, true)]
    [InlineData(DiagnosticSeverity.Warning, true)]
    [InlineData(DiagnosticSeverity.Error, false)]
    public void IsOptional_IsTrueExceptForAnError(DiagnosticSeverity severity, bool expected) =>
        Assert.Equal(
            expected,
            new DiagnosticCodeInfo("DLG3002", "Deeply nested choice branch", DiagnosticCategory.Style, severity)
                .IsOptional);
}
