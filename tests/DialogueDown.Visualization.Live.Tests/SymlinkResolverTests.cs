using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class SymlinkResolverTests
{
    [Fact]
    public void Resolve_PlainExistingFile_ReturnsItUnchanged()
    {
        using var tree = new TempTree();
        var file = tree.File("doc.dialogue.md", "# Scene");

        var resolved = SymlinkResolver.Resolve(file);

        Assert.Equal(Path.GetFullPath(file), resolved);
    }

    [Fact]
    public void Resolve_MissingPlainPath_ReturnsItUnchangedForCreateOnEdit()
    {
        using var tree = new TempTree();
        var missing = Path.Combine(tree.Root, "new.dialogue.md");

        var resolved = SymlinkResolver.Resolve(missing);

        Assert.Equal(Path.GetFullPath(missing), resolved);
        Assert.False(File.Exists(resolved)); // resolution never creates the target
    }

    [Fact]
    public void Resolve_Symlink_ReturnsTheRealTargetFile()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# Real");
        var link = Symlink(tree, "link.dialogue.md", real);

        var resolved = SymlinkResolver.Resolve(link);

        Assert.NotEqual(Path.GetFullPath(link), resolved);
        Assert.Equal("# Real", File.ReadAllText(resolved));
        Assert.Null(new FileInfo(resolved).LinkTarget); // the resolved path is the real file, not a link
    }

    [Fact]
    public void Resolve_SymlinkChain_FollowsToTheFinalTarget()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# Real");
        var first = Symlink(tree, "first.dialogue.md", real);
        var second = Symlink(tree, "second.dialogue.md", first);

        var resolved = SymlinkResolver.Resolve(second);

        Assert.Equal("# Real", File.ReadAllText(resolved));
        Assert.Null(new FileInfo(resolved).LinkTarget);
    }

    [Fact]
    public void Resolve_BrokenSymlink_Throws()
    {
        using var tree = new TempTree();
        var missingTarget = Path.Combine(tree.Root, "gone.dialogue.md");
        var link = Symlink(tree, "broken.dialogue.md", missingTarget);

        Assert.Throws<IOException>(() => SymlinkResolver.Resolve(link));
    }

    [Fact]
    public void Resolve_CyclicSymlink_Throws()
    {
        using var tree = new TempTree();
        var a = Path.Combine(tree.Root, "a.dialogue.md");
        var b = Path.Combine(tree.Root, "b.dialogue.md");
        Symlinks.Create(a, b);
        Symlinks.Create(b, a);

        Assert.Throws<IOException>(() => SymlinkResolver.Resolve(a));
    }

    // Creates the symbolic link `relative` -> `target` inside `tree`, skipping the test when the
    // platform (e.g. Windows without the privilege) refuses to create one.
    private static string Symlink(TempTree tree, string relative, string target)
    {
        var link = Path.Combine(tree.Root, relative);
        Symlinks.Create(link, target);
        return link;
    }
}
