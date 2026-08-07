using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SceneBuilderTests
{
    [Fact]
    public void Build_EmptyDocument_IsAnEmptyRoot()
    {
        var (root, anchors) = Build();

        Assert.Null(root.Heading);
        Assert.Empty(root.ChildScenes);
        Assert.Empty(root.Blocks);
        Assert.False(anchors.TryResolve("anything", out _));
    }

    [Fact]
    public void Build_BlocksBeforeTheFirstHeading_OwnedByTheRoot()
    {
        var leading = Line(Text("intro"));

        var (root, _) = Build(leading, SceneHeading("Scene", 1));

        Assert.Equal([leading], root.Blocks);
        Assert.Single(root.ChildScenes);
    }

    [Fact]
    public void Build_TopLevelHeadings_AreRootChildren()
    {
        var (root, _) = Build(SceneHeading("A", 1), SceneHeading("B", 1));

        Assert.Equal(["a", "b"], root.ChildScenes.Select(scene => scene.Anchor));
    }

    [Fact]
    public void Build_DeeperHeading_NestsUnderThePrecedingShallowerOne()
    {
        var (root, _) = Build(SceneHeading("A", 1), SceneHeading("A one", 2));

        var a = Assert.Single(root.ChildScenes);
        var childAnchors = a.ChildScenes.Select(scene => scene.Anchor);
        Assert.Equal(["a-one"], childAnchors);
    }

    [Fact]
    public void Build_ShallowerHeading_ClosesDeeperScenes()
    {
        // ## A  /  ### A one  /  ## B  → A and B are siblings under root; A one nests in A.
        var (root, _) = Build(
            SceneHeading("A", 2),
            SceneHeading("A one", 3),
            SceneHeading("B", 2));

        Assert.Equal(["a", "b"], root.ChildScenes.Select(scene => scene.Anchor));
        Assert.Equal(["a-one"], root.ChildScenes[0].ChildScenes.Select(scene => scene.Anchor));
    }

    [Fact]
    public void Build_AScene_OwnsTheBlocksUntilTheNextHeading()
    {
        var inA = Line(Text("in a"));
        var inB = Line(Text("in b"));

        var (root, _) = Build(
            SceneHeading("A", 1), inA,
            SceneHeading("B", 1), inB);

        Assert.Equal([inA], root.ChildScenes[0].Blocks);
        Assert.Equal([inB], root.ChildScenes[1].Blocks);
    }

    [Fact]
    public void Build_IndexesEachSceneByItsAnchor()
    {
        var (_, anchors) = Build(SceneHeading("Play tennis", 1));

        Assert.True(anchors.TryResolve("play-tennis", out var scene));
        Assert.Equal("play-tennis", scene.Anchor);
    }

    [Fact]
    public void Build_DuplicateAnchor_Reports()
    {
        Build(out var diagnostics, SceneHeading("Play tennis", 1), SceneHeading("Play Tennis", 2));

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DuplicateAnchor);
    }

    [Fact]
    public void Build_HeadingThatSlugsToNothing_Reports()
    {
        Build(out var diagnostics, SceneHeading("!!!", 1));

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.HeadingWithoutAnchor);
    }

    private static (Scene Root, AnchorTable Anchors) Build(params ScriptBlock[] blocks) =>
        Build(out _, blocks);

    private static (Scene Root, AnchorTable Anchors) Build(
        out DiagnosticBag diagnostics, params ScriptBlock[] blocks)
    {
        diagnostics = new DiagnosticBag();
        return SceneBuilder.Build(new DesugaredScriptDocument(new ScriptDocument(blocks)), diagnostics);
    }
}
