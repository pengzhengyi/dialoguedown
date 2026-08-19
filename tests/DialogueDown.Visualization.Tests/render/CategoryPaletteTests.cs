using DialogueDown.Visualization.Render;
namespace DialogueDown.Visualization.Tests.Render;

public sealed class CategoryPaletteTests
{
    [Fact]
    public void ColorOf_KnownCategory_ReturnsItsHex()
    {
        Assert.Equal("#22c55e", CategoryPalette.ColorOf("speech"));
        Assert.Equal("#ef4444", CategoryPalette.ColorOf("call"));
    }

    [Fact]
    public void ColorOf_UnknownCategory_ReturnsTheNeutralDefault()
    {
        Assert.Equal("#94a3b8", CategoryPalette.ColorOf("no-such-category"));
    }
}
