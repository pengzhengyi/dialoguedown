using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using NSubstitute;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DesugarerTests
{
    [Fact]
    public void Desugar_RunsEachRuleInOrder_FeedingOutputToTheNext()
    {
        var afterFirst = new ScriptDocument([Line(Text("first"))]);
        var afterSecond = new ScriptDocument([Line(Text("second"))]);
        var first = Substitute.For<IDesugarRule>();
        var second = Substitute.For<IDesugarRule>();
        first.Apply(Arg.Any<ScriptDocument>()).Returns(afterFirst);
        second.Apply(Arg.Any<ScriptDocument>()).Returns(afterSecond);
        var input = new ScriptDocument([]);

        var result = new Desugarer([first, second]).Desugar(input);

        Received.InOrder(() =>
        {
            first.Apply(input);
            second.Apply(afterFirst); // the second rule sees the first rule's output
        });
        Assert.Same(afterSecond, result);
    }

    [Fact]
    public void Desugar_NoRules_ReturnsTheDocumentUnchanged()
    {
        var input = new ScriptDocument([Line(Text("hi"))]);

        Assert.Same(input, new Desugarer([]).Desugar(input));
    }

    [Fact]
    public void Constructor_NullRules_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new Desugarer(null!));

    [Fact]
    public void Desugar_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new Desugarer([]).Desugar(null!));
}
