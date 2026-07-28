using DialogueDown.Script.Transpiler.Builders;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class QueryKeyReaderTests
{
    [Theory]
    [InlineData("\"Rainy\"", "Rainy")]                              // quoted -> inner text
    [InlineData("\"Alice.FavoriteColor\"", "Alice.FavoriteColor")]  // quoted dotted key
    [InlineData("\"\"", "")]                                        // quoted empty -> empty key
    [InlineData("Rainy", "Rainy")]                                  // unquoted -> verbatim
    [InlineData("Is Alice happy", "Is Alice happy")]                // spaces kept
    [InlineData("Alice.FavoriteColor", "Alice.FavoriteColor")]      // unquoted dotted key
    [InlineData("\"a\" \"b\"", "\"a\" \"b\"")]                      // not one clean quoted string -> raw
    [InlineData("\"a\"b", "\"a\"b")]                                // mixed quotes -> raw
    public void Read_QuotedOrUnquoted_YieldsTheKey(string text, string expected) =>
        Assert.Equal(expected, QueryKeyReader.Read(text));

    [Fact]
    public void Read_EmptyText_YieldsNull() =>
        Assert.Null(QueryKeyReader.Read(""));
}
