using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Transpiler;

/// <summary>
/// End-to-end checks that a construct written with an unquoted key compiles to the same key its
/// quoted form would, driven through the real Markdown parser and transpiler (and desugar for a
/// jump, where the condition binds). Recognition itself lives in the reader unit tests; these
/// prove the whole pipeline agrees on the unquoted form.
/// </summary>
public sealed class UnquotedKeyTranspilationTests
{
    private readonly ScriptTranspiler _transpiler = TranspilerBuilderFactory.ScriptTranspiler();

    [Fact]
    public void UnquotedCondition_OnALine_GuardsItWithThePlainKey()
    {
        var script = Transpile("`Angry?` Guard: You again? Get out.");

        AssertCondition(AssertLine(Assert.Single(script.Body)).Condition!, "Angry");
    }

    [Fact]
    public void UnquotedCondition_KeepsItsSpaces()
    {
        var script = Transpile("`Is Alice happy?` Alice smiles and waves you in.");

        AssertCondition(AssertLine(Assert.Single(script.Body)).Condition!, "Is Alice happy");
    }

    [Fact]
    public void UnquotedCondition_OnAChoiceOption_GuardsTheWholeOption()
    {
        var script = Transpile(
            """
            - `HasKey?` Use the key on the lock.
            - Search for another way in.
            """);

        var choices = AssertChoices(Assert.Single(script.Body), isOrdered: false);
        AssertCondition(choices.Options[0].Condition!, "HasKey");
        Assert.Null(choices.Options[1].Condition);
    }

    [Fact]
    public void UnquotedConditionAndUnquotedWeight_OnRandomOptions()
    {
        var script = Transpile(
            """
            - `IsAngry?` `50%` The guard glares and blocks your path.
            - `Luck%` The guard waves you through.
            """);

        var random = AssertRandomChoices(Assert.Single(script.Body));
        AssertCondition(random.Options[0].Condition!, "IsAngry");
        AssertNumberWeight(random.Options[0], 50);
        AssertQueryWeight(random.Options[1], "Luck");
    }

    [Fact]
    public void UnquotedCondition_OnAJump_BindsToItAfterDesugar()
    {
        var desugared = Pipeline.UntilDesugared("`Rainy?` => [Wait it out](#the-inn)");

        var control = AssertControlLine(Assert.Single(desugared.Document.Body));
        AssertCondition(AssertJump(Assert.Single(control.Effects), "#the-inn").Condition!, "Rainy");
    }

    private ScriptDocument Transpile(string source) =>
        _transpiler.Transpile(
            MarkdownParserFactory.MarkdownParser().Parse(source),
            DiagnosticsContextFactory.Context(source));
}
