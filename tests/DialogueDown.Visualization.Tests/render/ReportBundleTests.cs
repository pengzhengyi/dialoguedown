using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class ReportBundleTests
{
    private static readonly ReportBundle _bundle = ReportBundle.Default;

    [Fact]
    public void Default_CarriesTheClientItsStyleAndMermaidSeparately()
    {
        Assert.Contains("--pico-", _bundle.Style, StringComparison.Ordinal);
        Assert.True(_bundle.Script.Length > 100_000, "the built client script should be substantial");
        Assert.Contains("mermaid", _bundle.Mermaid, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Default_KeepsMermaidOutOfTheClientEveryReaderLoads()
    {
        // Mermaid is the largest thing the report can draw with and the rarest thing it needs.
        // Bundling it would put cytoscape, katex, and every diagram type in every page.
        Assert.True(
            _bundle.Script.Length < _bundle.Mermaid.Length,
            $"the client ({_bundle.Script.Length}) should be smaller than Mermaid ({_bundle.Mermaid.Length})");
    }

    [Fact]
    public void Default_AddressesEachAssetByItsContent()
    {
        foreach (var path in new[] { _bundle.ScriptPath, _bundle.StylePath, _bundle.MermaidPath })
        {
            Assert.StartsWith("/assets/", path, StringComparison.Ordinal);
        }

        Assert.EndsWith(".js", _bundle.ScriptPath, StringComparison.Ordinal);
        Assert.EndsWith(".css", _bundle.StylePath, StringComparison.Ordinal);
        Assert.EndsWith(".js", _bundle.MermaidPath, StringComparison.Ordinal);
        Assert.NotEqual(_bundle.ScriptPath, _bundle.MermaidPath);
    }

    [Fact]
    public void PathFor_RenamesContentThatChanged()
    {
        // The name carries the content, so a rebuilt client cannot be served from a cache that
        // still holds the old one — which is what makes an immutable cache policy safe.
        Assert.NotEqual(ReportBundle.PathFor("alpha", "js"), ReportBundle.PathFor("beta", "js"));
        Assert.Equal(ReportBundle.PathFor("alpha", "js"), ReportBundle.PathFor("alpha", "js"));
    }
}
