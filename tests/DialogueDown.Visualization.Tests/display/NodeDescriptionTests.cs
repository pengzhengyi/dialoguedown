using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization.Tests.Display;

public sealed class NodeDescriptionTests
{
    [Fact]
    public void Constructor_NullLabel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NodeDescription(null!));
    }

    [Fact]
    public void Constructor_EmptyLabel_Throws()
    {
        Assert.Throws<ArgumentException>(() => new NodeDescription(string.Empty));
    }

    [Fact]
    public void Constructor_WithoutAttributes_DefaultsToEmpty()
    {
        var description = new NodeDescription("Heading");

        Assert.Equal("Heading", description.Label);
        Assert.Empty(description.Attributes);
    }

    [Fact]
    public void Constructor_KeepsLabelAndAttributes()
    {
        var attributes = new[] { new DisplayAttribute("level", "2") };

        var description = new NodeDescription("Heading", attributes);

        Assert.Equal("Heading", description.Label);
        Assert.Same(attributes, description.Attributes);
    }
}
