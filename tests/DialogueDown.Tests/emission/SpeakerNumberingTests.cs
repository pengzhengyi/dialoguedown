using DialogueDown.Emission;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Emission;

public sealed class SpeakerNumberingTests
{
    [Fact]
    public void Of_EachSpeaker_SitsWhereTheyFirstSpeak()
    {
        var alice = SpeakerSymbol.ForName("Alice");
        var bob = SpeakerSymbol.ForName("Bob");
        var numbering = SpeakerNumbering.Of([LineNode(0, alice), LineNode(1, bob)]);

        Assert.Equal(0, numbering.Position(alice));
        Assert.Equal(1, numbering.Position(bob));
    }

    [Fact]
    public void Of_ASpeakerWhoSaysSeveralThings_IsListedOnce()
    {
        var alice = SpeakerSymbol.ForName("Alice");

        var numbering = SpeakerNumbering.Of([LineNode(0, alice), LineNode(1, alice)]);

        Assert.Equal("Alice", Assert.Single(numbering.Speakers).Name);
    }

    [Fact]
    public void Of_TwoSpeakersOfTheSameName_StayApart()
    {
        // A symbol is enriched in place as its declarations are met, so identity is the object
        // rather than the name. Two objects mean two people, whatever they are called.
        var first = SpeakerSymbol.ForName("Alice");
        var second = SpeakerSymbol.ForName("Alice");

        var numbering = SpeakerNumbering.Of([LineNode(0, first), LineNode(1, second)]);

        Assert.Equal(2, numbering.Speakers.Length);
        Assert.NotEqual(numbering.Position(first), numbering.Position(second));
    }

    [Fact]
    public void Of_AGraphWithNoLines_ListsNobody()
    {
        Assert.Empty(SpeakerNumbering.Of([EndNode(0)]).Speakers);
    }

    [Fact]
    public void Position_SomebodyWhoSaysNothingHere_IsRejected()
    {
        var numbering = SpeakerNumbering.Of([LineNode(0, SpeakerSymbol.ForName("Alice"))]);

        var error = Assert.Throws<ArgumentException>(
            () => numbering.Position(SpeakerSymbol.ForName("Bob")));

        Assert.Contains("Bob", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_NoNodesAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _ = SpeakerNumbering.Of(null!));
    }
}
