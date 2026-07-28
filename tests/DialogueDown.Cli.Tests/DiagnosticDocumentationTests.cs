namespace DialogueDown.Cli.Tests;

public sealed class DiagnosticDocumentationTests
{
    [Theory]
    [InlineData("DLG1102", "#dlg1102")]
    [InlineData("DLG2001", "#dlg2001")]
    [InlineData("DLG1003", "#dlg1003")]
    public void UrlFor_DeepLinksToTheLowercaseAnchorOnTheErrorCodesPage(string code, string anchor)
    {
        var url = DiagnosticDocumentation.UrlFor(code);

        Assert.StartsWith(
            "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html",
            url,
            StringComparison.Ordinal);
        Assert.EndsWith(anchor, url, StringComparison.Ordinal);
    }

    [Fact]
    public void UrlFor_NullOrEmptyCode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DiagnosticDocumentation.UrlFor(null!));
        Assert.Throws<ArgumentException>(() => DiagnosticDocumentation.UrlFor(string.Empty));
    }
}
