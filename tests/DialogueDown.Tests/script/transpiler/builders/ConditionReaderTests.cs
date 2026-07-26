using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class ConditionReaderTests
{
    [Theory]
    [InlineData("\"Rainy\"?", "Rainy")]
    [InlineData("\"Rainy?\"?", "Rainy?")]
    [InlineData(" \"Alice.Ready\" ? ", "Alice.Ready")]
    public void Read_AQueryFollowedByQuestionMark_YieldsACondition(string content, string expectedKey)
    {
        var condition = Assert.IsType<Condition>(
            ConditionReader.Read(content, new SourceSpan(0, content.Length)));

        Assert.Equal(expectedKey, condition.Key);
    }

    [Theory]
    [InlineData("\"Rainy\"")]     // a plain query, no ?
    [InlineData("?")]             // no query before the ?
    [InlineData("\"a\" \"b\"?")]  // not a single query before the ?
    [InlineData("(\"do\")?")]     // a command, not a query
    [InlineData("just words")]
    public void Read_AnythingElse_YieldsNull(string content) =>
        Assert.Null(ConditionReader.Read(content, new SourceSpan(0, content.Length)));
}
