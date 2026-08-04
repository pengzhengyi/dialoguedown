using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Visualization.Semantics;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SceneTreeProjectionTests
{
    // The scene-description tests never touch the model, so any valid one will do.
    private readonly SceneTreeProjection _projection = new(Pipeline.Model("Hi."), "Hi.");

    [Fact]
    public void Title_IsSemanticModel() => Assert.Equal("Semantic Model", _projection.Title);

    [Fact]
    public void Description_IsANonEmptyOneLiner()
    {
        Assert.False(string.IsNullOrWhiteSpace(_projection.Description));
        Assert.DoesNotContain('\n', _projection.Description);
    }

    [Fact]
    public void Describe_Root_HasNoEntityKey()
    {
        var description = _projection.Describe(Scene.Root());

        Assert.Equal("Document root", description.Label);
        Assert.Null(description.EntityKey);
        Assert.Equal("document", description.Category);
        Assert.Equal("Document", description.TypeName);
    }

    [Fact]
    public void Describe_AnchoredScene_CarriesLabelAnchorLevelAndKey()
    {
        var heading = new SceneHeading([new Text("The Market", new SourceSpan(0, 1))], 2, new SourceSpan(0, 1));
        var scene = Scene.ForHeading(heading, "the-market");

        var description = _projection.Describe(scene);

        Assert.Equal("The Market", description.Label);
        Assert.Equal("Scene", description.TypeName);
        Assert.Equal("structure", description.Category);
        Assert.Equal("scene:the-market", description.EntityKey);
        Assert.Contains(description.Attributes, attribute => attribute is { Name: "anchor", Value: "#the-market" });
        Assert.Contains(description.Attributes, attribute => attribute is { Name: "level", Value: "2" });
    }

    [Fact]
    public void Describe_Scene_CarriesItsHeadingSpan()
    {
        // A scene maps to its heading in the source, so its detail panel can jump there.
        var heading = new SceneHeading([new Text("Hi", new SourceSpan(0, 2))], 2, new SourceSpan(0, 2));
        var scene = Scene.ForHeading(heading, "hi");

        Assert.Equal(new DisplaySpan(0, 2), _projection.Describe(scene).Span);
    }

    [Fact]
    public void Describe_Scene_CarriesItsHeadingSource()
    {
        // A scene's detail panel shows the heading it was opened by, not a "no source" note.
        var source = """
            # The Market

            Alice: Fresh apples!
            """;
        var model = Pipeline.Model(source);
        var projection = new SceneTreeProjection(model, source);
        var scene = model.SceneRoot.ChildScenes[0];

        Assert.Contains("The Market", projection.Describe(scene).Source!);
    }

    [Fact]
    public void Describe_ADelegatedBlock_ReadsLikeTheDesugaredTab()
    {
        // A non-scene node is described by the shared Dialogue AST projection.
        var source = """
            # A Scene

            Alice: Hello there.
            """;
        var model = Pipeline.Model(source);
        var projection = new SceneTreeProjection(model, source);
        var line = model.SceneRoot.ChildScenes[0].Blocks[0];

        Assert.Equal("Line", projection.Describe(line).Label);
    }

    [Fact]
    public void Neighbors_YieldTheScenesOwnBlocksThenItsChildScenes()
    {
        // "A Scene" owns a line, then nests "Deeper" — the tree shows the block before the child.
        var source = """
            # A Scene

            Alice: Hello.

            ## Deeper

            Bob: Hi.
            """;
        var model = Pipeline.Model(source);
        var projection = new SceneTreeProjection(model, source);
        var scene = model.SceneRoot.ChildScenes[0];

        var neighbors = projection.Neighbors(scene).ToList();

        Assert.IsAssignableFrom<ScriptBlock>(neighbors[0]);
        Assert.Equal("deeper", Assert.IsType<Scene>(neighbors[^1]).Anchor);
    }
}
