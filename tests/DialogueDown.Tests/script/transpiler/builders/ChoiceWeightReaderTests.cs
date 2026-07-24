using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class ChoiceWeightReaderTests
{
    [Theory]
    [InlineData("50%")]
    [InlineData("%")]
    [InlineData(" 50% ")]
    [InlineData("33.3%")]
    [InlineData("abc%")]
    [InlineData("\"Bob's Affection\"%")]
    public void IsWeight_IsTrue_WhenTheContentEndsWithPercent(string content) =>
        Assert.True(ChoiceWeightReader.IsWeight(content));

    [Theory]
    [InlineData("50")]
    [InlineData("\"key\"")]
    [InlineData("(\"do something\")")]
    [InlineData("Name(\"arg\")")]
    public void IsWeight_IsFalse_ForAGameCallOrPlainCodeSpan(string content) =>
        Assert.False(ChoiceWeightReader.IsWeight(content));

    [Theory]
    [InlineData("50%", 50)]
    [InlineData("0%", 0)]
    [InlineData("100%", 100)]
    [InlineData("33.3%", 33.3)]
    [InlineData(" 70 % ", 70)]
    public void Read_ANumericWeight_YieldsANumberWeight(string content, double expected)
    {
        var weight = Assert.IsType<NumberWeight>(ChoiceWeightReader.Read(content));

        Assert.Equal(expected, weight.Percentage);
    }

    [Theory]
    [InlineData("%")]
    [InlineData(" % ")]
    public void Read_ABarePercent_YieldsAnAutoWeight(string content) =>
        Assert.IsType<AutoWeight>(ChoiceWeightReader.Read(content));

    [Theory]
    [InlineData("-10%")]
    [InlineData("abc%")]
    [InlineData("\"Bob's Affection\"%")]
    [InlineData("%%")]
    public void Read_AnInvalidWeight_YieldsNull(string content) =>
        Assert.Null(ChoiceWeightReader.Read(content));
}
