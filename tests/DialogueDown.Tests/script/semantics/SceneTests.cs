using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SceneTests
{
    [Fact]
    public void Root_HasNoHeadingLevelZeroAndNoAnchor()
    {
        var root = Scene.Root();

        Assert.Null(root.Heading);
        Assert.Equal(0, root.Level);
        Assert.Null(root.Anchor);
        Assert.Empty(root.ChildScenes);
        Assert.Empty(root.Blocks);
    }

    [Fact]
    public void ForHeading_TakesTheHeadingLevelAndAnchor()
    {
        var heading = SceneHeading("Play tennis", 2);

        var scene = Scene.ForHeading(heading, "play-tennis");

        Assert.Same(heading, scene.Heading);
        Assert.Equal(2, scene.Level);
        Assert.Equal("play-tennis", scene.Anchor);
    }

    [Fact]
    public void AddChild_AppendsInOrder()
    {
        var parent = Scene.Root();
        var first = Scene.ForHeading(SceneHeading("A", 1), "a");
        var second = Scene.ForHeading(SceneHeading("B", 1), "b");

        parent.AddChild(first);
        parent.AddChild(second);

        Assert.Equal([first, second], parent.ChildScenes);
    }

    [Fact]
    public void AddBlock_AppendsInOrder()
    {
        var scene = Scene.Root();
        var first = Line(Text("one"));
        var second = Line(Text("two"));

        scene.AddBlock(first);
        scene.AddBlock(second);

        Assert.Equal([first, second], scene.Blocks);
    }
}
