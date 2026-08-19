using DialogueDown.Visualization.Live.Serving;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class ServeRootResolverTests
{
    [Fact]
    public void Resolve_ImagesInsideFolder_HostsDocumentFolderWithoutPrompting()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");
        var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(document))!;
        var error = new StringWriter();

        var serveRoot = ServeRootResolver.Resolve(
            document, ["assets/pic.png"], renderRoot: null, new ThrowingConsent(), error);

        Assert.NotNull(serveRoot);
        Assert.Equal(documentDirectory, serveRoot.Value.RootDirectory);
        Assert.Equal("/", serveRoot.Value.ReportPath);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void Resolve_NoImages_HostsDocumentFolder()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");

        var serveRoot = ServeRootResolver.Resolve(
            document, [], renderRoot: null, new ThrowingConsent(), new StringWriter());

        Assert.Equal("/", serveRoot!.Value.ReportPath);
    }

    [Fact]
    public void Resolve_ImageOutsideFolder_AllowedHostsCoveringFolderAtSubPath()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");
        var painting = tree.File("shared/painting.png");
        var consent = new StubConsent(allow: true);

        var serveRoot = ServeRootResolver.Resolve(
            document, ["../shared/painting.png"], renderRoot: null, consent, new StringWriter());

        Assert.NotNull(serveRoot);
        Assert.Equal(Path.GetFullPath(tree.Root), serveRoot.Value.RootDirectory);
        Assert.Equal("/proj/", serveRoot.Value.ReportPath);
        Assert.NotNull(consent.Received);
        Assert.Equal(Path.GetFullPath(tree.Root), consent.Received!.RootDirectory);
        Assert.Equal(Path.GetFullPath(document), consent.Received.DocumentPath);
        Assert.Contains(Path.GetFullPath(painting), consent.Received.OutsideImages);
    }

    [Fact]
    public void Resolve_ImageOutsideFolder_DeclinedFallsBackToDocumentFolder()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");
        var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(document))!;

        var serveRoot = ServeRootResolver.Resolve(
            document, ["../shared/painting.png"], renderRoot: null, new StubConsent(allow: false), new StringWriter());

        Assert.Equal(documentDirectory, serveRoot!.Value.RootDirectory);
        Assert.Equal("/", serveRoot.Value.ReportPath);
    }

    [Fact]
    public void Resolve_AbsoluteImagePathOutsideFolder_IsTreatedAsOutside()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");
        var painting = tree.File("gallery/p.jpg");
        var consent = new StubConsent(allow: true);

        var serveRoot = ServeRootResolver.Resolve(
            document, [painting], renderRoot: null, consent, new StringWriter());

        Assert.Equal(Path.GetFullPath(tree.Root), serveRoot!.Value.RootDirectory);
        Assert.NotNull(consent.Received);
    }

    [Fact]
    public void Resolve_ExplicitRenderRoot_HostsItWithoutPrompting()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");

        var serveRoot = ServeRootResolver.Resolve(
            document, ["../shared/x.png"], renderRoot: tree.Root, new ThrowingConsent(), new StringWriter());

        Assert.Equal(Path.GetFullPath(tree.Root), serveRoot!.Value.RootDirectory);
        Assert.Equal("/proj/", serveRoot.Value.ReportPath);
    }

    [Fact]
    public void Resolve_ExplicitRenderRoot_NotFound_ReturnsNullWithError()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");
        var missing = Path.Combine(tree.Root, "does-not-exist");
        var error = new StringWriter();

        var serveRoot = ServeRootResolver.Resolve(
            document, [], renderRoot: missing, new ThrowingConsent(), error);

        Assert.Null(serveRoot);
        Assert.Contains("Render root not found", error.ToString());
    }

    [Fact]
    public void Resolve_ExplicitRenderRoot_DoesNotContainDocument_ReturnsNullWithError()
    {
        using var tree = new TempTree();
        var document = tree.File("proj/scene.dialogue.md");
        var elsewhere = tree.Dir("elsewhere");
        var error = new StringWriter();

        var serveRoot = ServeRootResolver.Resolve(
            document, [], renderRoot: elsewhere, new ThrowingConsent(), error);

        Assert.Null(serveRoot);
        Assert.Contains("not inside the render root", error.ToString());
    }

    [Fact]
    public void LongestCommonAncestor_ReturnsDeepestSharedFolder()
    {
        var ancestor = ServeRootResolver.LongestCommonAncestor(
            [Path.GetFullPath("/foo/abc/x"), Path.GetFullPath("/foo/abc/y/z")]);

        Assert.Equal(Path.GetFullPath("/foo/abc"), ancestor);
    }

    [Fact]
    public void LongestCommonAncestor_DisjointTrees_ReturnsFilesystemRoot()
    {
        var ancestor = ServeRootResolver.LongestCommonAncestor(
            [Path.GetFullPath("/foo/abc"), Path.GetFullPath("/bar/efg")]);

        Assert.Equal(Path.GetFullPath("/"), ancestor);
    }

    private sealed class StubConsent(bool allow) : IHostConsent
    {
        public HostConsentRequest? Received { get; private set; }

        public bool AllowHosting(HostConsentRequest request)
        {
            Received = request;
            return allow;
        }
    }

    private sealed class ThrowingConsent : IHostConsent
    {
        public bool AllowHosting(HostConsentRequest request) =>
            throw new InvalidOperationException("consent must not be requested");
    }
}
