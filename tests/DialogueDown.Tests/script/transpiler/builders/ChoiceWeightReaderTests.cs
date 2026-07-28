using DialogueDown.Common;
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
        var weight = Assert.IsType<NumberWeight>(ChoiceWeightReader.Read(content, new SourceSpan(0, content.Length)));

        Assert.Equal(expected, weight.Percentage);
    }

    [Theory]
    [InlineData("%")]
    [InlineData(" % ")]
    public void Read_ABarePercent_YieldsAnAutoWeight(string content) =>
        Assert.IsType<AutoWeight>(ChoiceWeightReader.Read(content, new SourceSpan(0, content.Length)));

    [Theory]
    [InlineData("-10%")]   // a negative number is not a valid weight
    [InlineData("-0.5%")]
    public void Read_ANegativeNumber_YieldsNull(string content) =>
        Assert.Null(ChoiceWeightReader.Read(content, new SourceSpan(0, content.Length)));

    [Theory]
    [InlineData("\"Bob's Affection\"%", "Bob's Affection")]  // quoted key
    [InlineData("\"Guard.Suspicion\"%", "Guard.Suspicion")]
    [InlineData(" \"x\" % ", "x")]                            // whitespace around a quoted key
    [InlineData("\"50\"%", "50")]                             // a quoted numeric is a key, not a number
    [InlineData("Bob.Affection%", "Bob.Affection")]          // unquoted dotted key
    [InlineData("Alice's Luck%", "Alice's Luck")]            // unquoted key with a space and apostrophe
    [InlineData("abc%", "abc")]                               // any non-numeric text is a key
    [InlineData("%%", "%")]                                   // the key is everything before the final %
    public void Read_AKeyWeight_YieldsAQueryWeight(string content, string expectedKey)
    {
        var weight = Assert.IsType<QueryWeight>(
            ChoiceWeightReader.Read(content, new SourceSpan(0, content.Length)));

        Assert.Equal(expectedKey, weight.Key);
    }
}
