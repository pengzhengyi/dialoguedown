using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticDocumentationTests
{
    [Theory]
    [InlineData("DLG1102", "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1102")]
    [InlineData("dlg2001", "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg2001")]
    public void UrlFor_ACode_DeepLinksToItsLowerCaseAnchorOnTheErrorCodesPage(string code, string expected) =>
        Assert.Equal(expected, DiagnosticDocumentation.UrlFor(code));

    [Fact]
    public void UrlFor_NullCode_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DiagnosticDocumentation.UrlFor(null!));

    [Fact]
    public void UrlFor_EmptyCode_Throws() =>
        Assert.Throws<ArgumentException>(() => DiagnosticDocumentation.UrlFor(string.Empty));
}
