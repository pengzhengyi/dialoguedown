using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Visualization.Semantics;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SceneEntityTests
{
    [Fact]
    public void Key_PrefixesTheAnchor() =>
        Assert.Equal("scene:the-market", SceneEntity.Key(OnlyScene("# The Market")));

    [Fact]
    public void Label_IsTheHeadingText() =>
        Assert.Equal("The Market", SceneEntity.Label(OnlyScene("# The Market")));

    [Fact]
    public void Label_FallsBackToTheAnchorWhenHeadingTextIsEmpty()
    {
        // A scene's anchor is slugged from its heading text, so no script pairs a heading with no
        // text with an anchor such as "s"; the scene is built by hand.
        var scene = Scene.ForHeading(new SceneHeading([], 1, new SourceSpan(0, 1)), "s");
        Assert.Equal("#s", SceneEntity.Label(scene));
    }

    // The one scene the source's heading opens.
    private static Scene OnlyScene(string source) =>
        Assert.Single(Pipeline.Model(source).SceneRoot.ChildScenes);
}
