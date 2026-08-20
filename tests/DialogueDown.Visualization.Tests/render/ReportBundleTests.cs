using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class ReportBundleTests
{
    private const string Page = """
        <!doctype html>
        <html><head>
        <script type="module" crossorigin>console.log("built client");</script>
        <style rel="stylesheet" crossorigin>.dd { color: red; }</style>
        </head><body>
        <script>
            window.__DD_REPORT__ = "__REPORT__";
        </script>
        </body></html>
        """;

    [Fact]
    public void From_SeparatesTheBuiltScriptAndStyleFromThePage()
    {
        var bundle = ReportBundle.From(Page);

        Assert.Equal("console.log(\"built client\");", bundle.Script);
        Assert.Equal(".dd { color: red; }", bundle.Style);
        Assert.DoesNotContain("built client", bundle.LinkedHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("color: red", bundle.LinkedHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void From_LinksEachAssetByAContentAddressedPath()
    {
        var bundle = ReportBundle.From(Page);

        Assert.Contains($"src=\"{bundle.ScriptPath}\"", bundle.LinkedHtml, StringComparison.Ordinal);
        Assert.Contains($"href=\"{bundle.StylePath}\"", bundle.LinkedHtml, StringComparison.Ordinal);
        Assert.EndsWith(".js", bundle.ScriptPath, StringComparison.Ordinal);
        Assert.EndsWith(".css", bundle.StylePath, StringComparison.Ordinal);
    }

    [Fact]
    public void From_RenamesAnAssetWhoseContentChanged()
    {
        // The name carries the content, so a rebuilt client cannot be served from a cache that
        // still holds the old one — which is what makes an immutable cache policy safe.
        var before = ReportBundle.From(Page);
        var after = ReportBundle.From(Page.Replace("built client", "rebuilt client", StringComparison.Ordinal));

        Assert.NotEqual(before.ScriptPath, after.ScriptPath);
        Assert.Equal(before.StylePath, after.StylePath);
    }

    [Fact]
    public void From_KeepsThePerDocumentPayloadInThePage()
    {
        var bundle = ReportBundle.From(Page);

        Assert.Contains("\"__REPORT__\"", bundle.LinkedHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void From_KeepsTheClientDeferredSoItStillRunsAfterThePayload()
    {
        // The payload is assigned by an inline script at the end of the body, so the client must
        // keep module semantics: an external classic script would run before the payload exists.
        var bundle = ReportBundle.From(Page);

        Assert.Contains("type=\"module\"", bundle.LinkedHtml, StringComparison.Ordinal);
        Assert.True(
            bundle.LinkedHtml.IndexOf("type=\"module\"", StringComparison.Ordinal)
                < bundle.LinkedHtml.IndexOf("__DD_REPORT__", StringComparison.Ordinal));
    }

    [Fact]
    public void Default_SplitsTheRealBuiltReport()
    {
        var bundle = ReportBundle.Default;

        Assert.Contains("--pico-", bundle.Style, StringComparison.Ordinal);
        Assert.True(bundle.Script.Length > 100_000, "the built client script should be substantial");
        Assert.DoesNotContain("--pico-", bundle.LinkedHtml, StringComparison.Ordinal);
    }
}
