using DialogueDown.Visualization.Live.Serving;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class ServeRootTests
{
    [Fact]
    public void For_SameFolder_ServesReportAtRoot()
    {
        using var tree = new TempTree();
        var documentDirectory = tree.Dir("proj");

        var serveRoot = ServeRoot.For(documentDirectory, documentDirectory);

        Assert.Equal(Path.GetFullPath(documentDirectory), serveRoot.RootDirectory);
        Assert.Equal("/", serveRoot.ReportPath);
    }

    [Fact]
    public void For_NestedDocumentFolder_ServesReportAtThatSubPath()
    {
        using var tree = new TempTree();
        var documentDirectory = tree.Dir("proj");

        var serveRoot = ServeRoot.For(tree.Root, documentDirectory);

        Assert.Equal(Path.GetFullPath(tree.Root), serveRoot.RootDirectory);
        Assert.Equal("/proj/", serveRoot.ReportPath);
    }

    [Fact]
    public void For_DeeplyNestedDocumentFolder_MirrorsTheWholeSubPath()
    {
        using var tree = new TempTree();
        var documentDirectory = tree.Dir("a/b");

        var serveRoot = ServeRoot.For(tree.Root, documentDirectory);

        Assert.Equal("/a/b/", serveRoot.ReportPath);
    }
}
