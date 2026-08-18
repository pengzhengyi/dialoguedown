using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class BrowseRootTests
{
    [Fact]
    public void At_MissingDirectory_Throws()
    {
        using var tree = new TempTree();

        Assert.Throws<DirectoryNotFoundException>(
            () => BrowseRoot.At(Path.Combine(tree.Root, "does-not-exist")));
    }

    [Fact]
    public void Resolve_NestedPath_ReturnsConfinedAbsolute()
    {
        using var tree = new TempTree();
        var source = tree.File("root/proj/scene.dialogue.md");
        var root = BrowseRoot.At(tree.Dir("root"));

        var resolved = root.Resolve("proj/scene.dialogue.md");

        Assert.Equal(Path.GetFullPath(source), resolved);
    }

    [Fact]
    public void Resolve_ParentTraversal_ReturnsNull()
    {
        using var tree = new TempTree();
        tree.File("secret.dialogue.md");
        var root = BrowseRoot.At(tree.Dir("root"));

        Assert.Null(root.Resolve("../secret.dialogue.md"));
    }

    [Fact]
    public void Resolve_AbsolutePathOutside_ReturnsNull()
    {
        using var tree = new TempTree();
        var outside = tree.File("outside/x.dialogue.md");
        var root = BrowseRoot.At(tree.Dir("root"));

        Assert.Null(root.Resolve(Path.GetFullPath(outside)));
    }

    [Fact]
    public void Resolve_SymlinkEscapingRoot_ReturnsNull()
    {
        using var tree = new TempTree();
        var outsideDirectory = tree.Dir("outside");
        var root = BrowseRoot.At(tree.Dir("root"));
        var link = Path.Combine(root.RootDirectory, "link");
        Directory.CreateSymbolicLink(link, outsideDirectory);

        Assert.Null(root.Resolve("link"));
    }

    [Fact]
    public void ResolveSource_ValidSource_ReturnsPath()
    {
        using var tree = new TempTree();
        var source = tree.File("root/proj/scene.dialogue.md");
        var root = BrowseRoot.At(tree.Dir("root"));

        Assert.Equal(Path.GetFullPath(source), root.ResolveSource("proj/scene.dialogue.md"));
    }

    [Fact]
    public void ResolveSource_WrongExtension_ReturnsNull()
    {
        using var tree = new TempTree();
        tree.File("root/notes.md");
        var root = BrowseRoot.At(tree.Dir("root"));

        Assert.Null(root.ResolveSource("notes.md"));
    }

    [Fact]
    public void ResolveSource_Missing_ReturnsNull()
    {
        using var tree = new TempTree();
        var root = BrowseRoot.At(tree.Dir("root"));

        Assert.Null(root.ResolveSource("gone.dialogue.md"));
    }

    [Fact]
    public void Browse_ListsSubdirectoriesAndDialogueSourcesOnly()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md");
        tree.File("root/notes.md");
        tree.Dir("root/sub");
        var root = BrowseRoot.At(tree.Dir("root"));

        var listing = root.Browse(string.Empty);

        Assert.NotNull(listing);
        Assert.Equal(string.Empty, listing!.Value.Path);
        Assert.Null(listing.Value.Parent);
        Assert.Equal(new[] { "sub" }, listing.Value.Directories);
        Assert.Equal(new[] { "a.dialogue.md" }, listing.Value.Sources);
    }

    [Fact]
    public void Browse_Subdirectory_ParentIsRoot()
    {
        using var tree = new TempTree();
        tree.File("root/proj/scene.dialogue.md");
        var root = BrowseRoot.At(tree.Dir("root"));

        var listing = root.Browse("proj");

        Assert.NotNull(listing);
        Assert.Equal("proj", listing!.Value.Path);
        Assert.Equal(string.Empty, listing.Value.Parent);
        Assert.Equal(new[] { "proj/scene.dialogue.md" }, listing.Value.Sources);
    }

    [Fact]
    public void Browse_OutsideRoot_ReturnsNull()
    {
        using var tree = new TempTree();
        tree.Dir("outside");
        var root = BrowseRoot.At(tree.Dir("root"));

        Assert.Null(root.Browse("../outside"));
    }
}
