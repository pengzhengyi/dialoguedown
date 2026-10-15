using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

public sealed class CharExtensionsTests
{
    [Theory]
    [InlineData(' ')]
    [InlineData('\t')]
    [InlineData('\n')]
    [InlineData('\r')]
    public void IsCollapsibleSpace_IsTrue_ForASpaceATabOrALineBreak(char character) =>
        Assert.True(character.IsCollapsibleSpace());

    [Theory]
    [InlineData(' ')]
    [InlineData(' ')]
    [InlineData('a')]
    public void IsCollapsibleSpace_IsFalse_ForANamedSpaceOrAnyOtherCharacter(char character) =>
        Assert.False(character.IsCollapsibleSpace());
}
