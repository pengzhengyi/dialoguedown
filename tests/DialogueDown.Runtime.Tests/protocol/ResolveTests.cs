using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Protocol;

public sealed class ResolveTests
{
    [Fact]
    public void Keys_ListTheBooleanKeysThenTheTextKeysThenTheNumberKeys()
    {
        var resolve = new Resolve(
            BooleanKeys: ["Guard.IsAngry", "Hero.HasSword"], TextKeys: ["HeroName"], NumberKeys: ["Guard.Mood"]);

        Assert.Equal(["Guard.IsAngry", "Hero.HasSword", "HeroName", "Guard.Mood"], resolve.Keys);
    }

    [Fact]
    public void Keys_OfARequestNamingOneKind_AreThatKindsKeys() =>
        Assert.Equal(["HeroName"], new Resolve(BooleanKeys: [], TextKeys: ["HeroName"], NumberKeys: []).Keys);
}
