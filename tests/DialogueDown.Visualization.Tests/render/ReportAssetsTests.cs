using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class ReportAssetsTests
{
    [Fact]
    public void All_OffersTheClientScriptAndStylesheet()
    {
        var paths = ReportAssets.All.Select(asset => asset.Path).ToArray();

        Assert.Equal(2, paths.Length);
        Assert.Contains(paths, path => path.EndsWith(".js", StringComparison.Ordinal));
        Assert.Contains(paths, path => path.EndsWith(".css", StringComparison.Ordinal));
    }

    [Fact]
    public void All_NamesEachAssetsMediaTypeSoAServerNeedNotGuess()
    {
        foreach (var asset in ReportAssets.All)
        {
            var expected = asset.Path.EndsWith(".js", StringComparison.Ordinal)
                ? "text/javascript"
                : "text/css";
            Assert.Equal(expected, asset.ContentType);
            Assert.NotEmpty(asset.Content);
        }
    }

    [Fact]
    public void Find_ReturnsTheAssetAServedPageAsksFor()
    {
        var expected = ReportAssets.All[0];

        Assert.Same(expected, ReportAssets.Find(expected.Path));
    }

    [Theory]
    [InlineData("/assets/report.deadbeef.js")]
    [InlineData("/assets/../report.html")]
    [InlineData("")]
    public void Find_RefusesAPathItDoesNotOffer(string path)
    {
        // Only the two names the client was built under resolve, so the route cannot be talked
        // into reading anything else.
        Assert.Null(ReportAssets.Find(path));
    }
}
