using System.Text;
using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

public sealed class NumberingTests
{
    [Fact]
    public void Assign_EachNewThing_GetsTheNextNumber()
    {
        var numbering = new Numbering<string>();

        Assert.Equal(0, numbering.Assign("one"));
        Assert.Equal(1, numbering.Assign("two"));
        Assert.Equal(2, numbering.Assign("three"));
    }

    [Fact]
    public void Assign_SomethingSeenBefore_GetsTheSameNumberBack()
    {
        var numbering = new Numbering<string>();
        numbering.Assign("one");
        numbering.Assign("two");

        Assert.Equal(0, numbering.Assign("one"));
    }

    [Fact]
    public void InOrder_HoldsEachThingOnce_InTheOrderItWasFirstSeen()
    {
        var numbering = new Numbering<string>();
        numbering.Assign("one");
        numbering.Assign("two");
        numbering.Assign("one");

        Assert.Equal(["one", "two"], numbering.InOrder);
    }

    [Fact]
    public void Assign_WithAReferenceComparer_TellsEqualThingsApart()
    {
        // Identity is the object, not the value: two speakers may share a name and be two people.
        var numbering = new Numbering<StringBuilder>(ReferenceEqualityComparer.Instance);
        var first = new StringBuilder("Alice");
        var second = new StringBuilder("Alice");

        Assert.Equal(0, numbering.Assign(first));
        Assert.Equal(1, numbering.Assign(second));
    }

    [Fact]
    public void Of_ASequence_NumbersItInTheOrderItComes()
    {
        var numbering = Numbering<string>.Of(["one", "two", "one"]);

        Assert.Equal(["one", "two"], numbering.InOrder);
        Assert.True(numbering.TryPosition("two", out var position));
        Assert.Equal(1, position);
    }

    [Fact]
    public void Of_NoSequenceAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => Numbering<string>.Of(null!));
    }

    [Fact]
    public void TryPosition_SomethingNumbered_SaysWhereItSits()
    {
        var numbering = new Numbering<string>();
        numbering.Assign("one");

        Assert.True(numbering.TryPosition("one", out var position));
        Assert.Equal(0, position);
    }

    [Fact]
    public void TryPosition_SomethingUnnumbered_DoesNotNumberIt()
    {
        var numbering = new Numbering<string>();

        Assert.False(numbering.TryPosition("one", out _));
        Assert.Empty(numbering.InOrder);
    }

    [Fact]
    public void InOrder_BeforeAnythingIsSeen_IsEmpty()
    {
        Assert.Empty(new Numbering<string>().InOrder);
    }

    [Fact]
    public void Assign_NothingAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Numbering<string>().Assign(null!));
    }

    [Fact]
    public void TryPosition_NothingAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Numbering<string>().TryPosition(null!, out _));
    }
}
