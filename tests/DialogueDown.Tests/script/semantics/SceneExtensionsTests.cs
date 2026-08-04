using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SceneExtensionsTests
{
    [Fact]
    public void DocumentOrder_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((Scene)null!).DocumentOrder());

    [Fact]
    public void DocumentOrder_VisitsAScenesBlocksThenItsChildrenSubtreesInPreOrder()
    {
        // `## B` nests under `# A`; `# C` is a top-level sibling. Pre-order (not level-order)
        // yields a1, b1, c1 — strictly ascending source spans, which level-order would not.
        var model = Pipeline.UntilAnalyzed("""
            # A

            Alice: a1

            ## B

            Alice: b1

            # C

            Alice: c1
            """);

        var blocks = model.SceneRoot.DocumentOrder();

        Assert.Equal(3, blocks.Count);
        Assert.All(blocks, block => Assert.IsType<Line>(block));
        var starts = blocks.Select(block => block.Span.Start).ToArray();
        Assert.Equal(starts.OrderBy(start => start), starts);
    }

    [Fact]
    public void EntryBlocks_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((Scene)null!).EntryBlocks());

    [Fact]
    public void EntryBlocks_ASceneWithContent_EntersItsOwnFirstBlock()
    {
        var model = Pipeline.UntilAnalyzed("""
            # A

            Alice: a1

            Alice: a2
            """);

        AssertEntersLineAt(model, "a", model.SceneRoot.DocumentOrder()[0]);
    }

    [Fact]
    public void EntryBlocks_AHeadingWithOnlyChildScenes_EntersTheFirstChildsBlock()
    {
        var model = Pipeline.UntilAnalyzed("""
            # Parent

            ## Child

            Alice: only
            """);

        // The parent owns no blocks, so reaching it lands on the child's content.
        var child = model.SceneRoot.DocumentOrder()[0];
        AssertEntersLineAt(model, "parent", child);
        AssertEntersLineAt(model, "child", child);
    }

    [Fact]
    public void EntryBlocks_AnEmptySceneFollowedByAnother_FallsThroughToTheNextBlock()
    {
        var model = Pipeline.UntilAnalyzed("""
            # Empty

            # Next

            Alice: next
            """);

        AssertEntersLineAt(model, "empty", model.SceneRoot.DocumentOrder()[0]);
    }

    [Fact]
    public void EntryBlocks_AnEmptySceneWithNothingAfterIt_HasNoEntryBlock()
    {
        var model = Pipeline.UntilAnalyzed("""
            Alice: before

            # Empty
            """);

        Assert.Null(model.SceneRoot.EntryBlocks()[SceneAt(model, "empty")]);
    }

    [Fact]
    public void EntryBlocks_ADocumentOfOnlyHeadings_LeavesEverySceneWithoutAnEntry()
    {
        var model = Pipeline.UntilAnalyzed("""
            # Heading 1

            ## Heading 2

            ### Heading 3

            ## Another Heading 2
            """);

        var entries = model.SceneRoot.EntryBlocks();

        // Nothing in the document holds content, so reaching any scene ends the run.
        Assert.All(
            new[] { "heading-1", "heading-2", "heading-3", "another-heading-2" },
            anchor => Assert.Null(entries[SceneAt(model, anchor)]));
    }

    private static void AssertEntersLineAt(SemanticModel model, string anchor, ScriptBlock expected) =>
        Assert.Same(expected, model.SceneRoot.EntryBlocks()[SceneAt(model, anchor)]);

    private static Scene SceneAt(SemanticModel model, string anchor)
    {
        Assert.True(model.Anchors.TryResolve(anchor, out var scene));
        return scene;
    }
}
