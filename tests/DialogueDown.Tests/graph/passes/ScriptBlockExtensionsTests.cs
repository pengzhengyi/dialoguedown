using DialogueDown.Graph.Passes;
using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class ScriptBlockExtensionsTests
{
    [Fact]
    public void WithoutHeadings_DropsAHeadingAndKeepsTheRestInOrder()
    {
        var first = Line(Text("one"));
        var second = Line(Text("two"));

        var blocks = new ScriptBlock[] { first, SceneHeading("Upstairs", 1), second }.WithoutHeadings();

        Assert.Equal([first, second], blocks);
    }

    [Fact]
    public void WithoutHeadings_NoHeading_KeepsEveryBlock()
    {
        var only = Line(Text("one"));

        Assert.Equal([only], new ScriptBlock[] { only }.WithoutHeadings());
    }

    [Fact]
    public void WithoutHeadings_OnlyHeadings_IsEmpty() =>
        Assert.Empty(new ScriptBlock[] { SceneHeading("Upstairs", 1) }.WithoutHeadings());

    [Fact]
    public void WithoutHeadings_EmptySequence_IsEmpty() =>
        Assert.Empty(Array.Empty<ScriptBlock>().WithoutHeadings());

    [Fact]
    public void WithoutHeadings_NullSequence_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((ScriptBlock[])null!).WithoutHeadings());
}
