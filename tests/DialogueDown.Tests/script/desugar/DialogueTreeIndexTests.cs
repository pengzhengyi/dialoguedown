using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DialogueTreeIndexTests
{
    [Fact]
    public void OfType_ReturnsNodesOfAConcreteType()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        Assert.Single(index.OfType<Jump>());
        Assert.Single(index.OfType<SceneHeading>());
        Assert.Equal(2, index.OfType<Line>().Count()); // the spoken line and the choice's line
    }

    [Fact]
    public void OfType_ReturnsNodesOfABaseType()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        // Speaker is abstract; the query still finds the concrete reference under it.
        var speaker = Assert.Single(index.OfType<Speaker>());
        Assert.IsType<SpeakerNameReference>(speaker);
    }

    [Fact]
    public void OfType_KeepsDocumentOrder()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        var text = index.OfType<Text>().Select(t => t.Content).ToArray();
        Assert.Equal(["Scene", "hi", "go", "pick"], text);
    }

    [Fact]
    public void OfType_IncludesEveryNodeUnderScriptNode()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        // "Scene" + heading + spoken line + speaker + "hi" + jump + "go" + choices
        // + choice + choice's line + "pick" = 11 nodes.
        Assert.Equal(11, index.OfType<ScriptNode>().Count());
    }

    [Fact]
    public void OfType_UnindexedType_IsEmpty()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        Assert.Empty(index.OfType<Query>());
    }

    [Fact]
    public void Build_EmptyDocument_HasNoNodes()
    {
        var index = DialogueTreeIndex.Build(new DesugaredScriptDocument(new ScriptDocument([])));

        Assert.Empty(index.OfType<ScriptNode>());
    }

    [Fact]
    public void AncestorsOf_NestedChoiceGroup_ReturnsNearestParentFirst()
    {
        var nested = Choices(Choice(Line(Text("nested"))));
        var outerChoice = Choice(nested);
        var outer = Choices(outerChoice);
        var index = Build(outer);

        var ancestors = index.AncestorsOf(nested);

        Assert.Equal([outerChoice, outer], ancestors);
    }

    [Fact]
    public void AncestorsOf_StructurallyEqualNodes_UsesReferenceIdentity()
    {
        IReadOnlyList<Choice> sharedOptions = [];
        var first = new Choices(false, sharedOptions, SourceSpanFactory.Span());
        var second = new Choices(false, sharedOptions, SourceSpanFactory.Span());
        var firstParent = Choice(first);
        var secondParent = Choice(second);
        var index = Build(Choices(firstParent, secondParent));
        Assert.Equal(first, second);
        Assert.NotSame(first, second);

        var firstAncestors = index.AncestorsOf(first);
        var secondAncestors = index.AncestorsOf(second);

        Assert.Same(firstParent, firstAncestors.First());
        Assert.Same(secondParent, secondAncestors.First());
    }

    [Fact]
    public void AncestorsOf_RootBlock_ReturnsEmpty()
    {
        var root = Choices(Choice(Line(Text("root"))));
        var index = Build(root);

        var ancestors = index.AncestorsOf(root);

        Assert.Empty(ancestors);
    }

    [Fact]
    public void AncestorsOf_NodeOutsideIndex_Throws()
    {
        var index = Build(Line(Text("indexed")));
        var outside = Line(Text("outside"));

        var exception = Assert.Throws<ArgumentException>(() => index.AncestorsOf(outside));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void Build_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DialogueTreeIndex.Build(null!));

    private static DialogueTreeIndex Build(params ScriptBlock[] blocks) =>
        DialogueTreeIndex.Build(new DesugaredScriptDocument(new ScriptDocument(blocks)));

    // ## Scene
    // Alice: hi => [go](#play)
    // - pick
    private static DesugaredScriptDocument SampleDocument()
    {
        var heading = SceneHeading("Scene", 1);
        var spoken = new Line(
            SpeakerNameReference("Alice"),
            [Text("hi"), Jump("#play", Text("go"))],
            SourceSpanFactory.Span());
        var choices = Choices(Choice(Line(Text("pick"))));
        return new DesugaredScriptDocument(new ScriptDocument([heading, spoken, choices]));
    }
}
