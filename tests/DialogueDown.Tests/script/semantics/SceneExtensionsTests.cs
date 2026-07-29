using System.Linq;
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
}
