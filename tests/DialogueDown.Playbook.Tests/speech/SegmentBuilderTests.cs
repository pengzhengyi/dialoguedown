using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using DialogueDown.TestSupport;
using static DialogueDown.Playbook.Tests.Support.PlaybookFactory;

namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SegmentBuilderTests
{
    /// <summary>One fragment of every kind the playbook format defines.</summary>
    public static ImmutableArray<SpeechFragment> OneOfEveryFragmentKind() =>
    [
        Text("words"),
        Query("HeroName"),
        LineBreak(),
        Tag("mood", "wry"),
        Bold("emphasized"),
        Link("#the-inn", Text("the inn")),
        Image("art/key.png", Text("a key")),
        DefaultCommand("waves"),
        CustomCommand("PlayBgm", "closer"),
    ];

    [Fact]
    public void Take_DecidesAboutEveryFragmentKind()
    {
        var samples = OneOfEveryFragmentKind();

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the format and left out here would go undivided and unsaid with nothing to say
        // so.
        UnionCoverageAssert.AssertCoversEveryMember<SpeechFragment>(samples);

        foreach (var fragment in samples)
        {
            Assert.NotEmpty(SegmentBuilder.Of([fragment]).Freeze());
        }
    }

    [Fact]
    public void Take_RefusesAKindItWasNeverTaught() =>
        Assert.Throws<NotSupportedException>(
            () => new SegmentBuilder().Take(new UntaughtFragment()));

    [Fact]
    public void IsDivided_IsTrueOnceACommandHasClosedASegment()
    {
        var builder = new SegmentBuilder();
        builder.Take(Text("Hello."));

        Assert.False(builder.IsDivided);

        builder.Take(DefaultCommand("waves"));

        Assert.True(builder.IsDivided);
    }

    [Fact]
    public void Freeze_WithNothingTaken_IsOneSilentSegment()
    {
        var only = Assert.Single(new SegmentBuilder().Freeze());

        Assert.False(only.Speaks);
        Assert.Null(only.Command);
    }

    [Fact]
    public void Take_OneFragmentAtATime_ReadsTheSameAsTakingThemAll()
    {
        SpeechFragment[] speech =
        [
            Text("a "), DefaultCommand("winks"),
            Bold(Text("b "), DefaultCommand("nods"), Text(" c")), Text(" d"),
        ];

        var apart = new SegmentBuilder();
        foreach (var fragment in speech)
        {
            apart.Take(fragment);
        }

        Assert.Equal(SegmentBuilder.Of([.. speech]).Freeze(), apart.Freeze());
    }

    [Fact]
    public void Freeze_DoesNotConsumeWhatWasTaken()
    {
        var builder = SegmentBuilder.Of([Text("Hello."), DefaultCommand("waves")]);

        Assert.Equal(builder.Freeze(), builder.Freeze());
    }

    [Fact]
    public void Take_RejectsNothing() =>
        Assert.Throws<ArgumentNullException>(() => new SegmentBuilder().Take(null!));

    /// <summary>A fragment kind the builder has never been taught, for the refusal case.</summary>
    private sealed record UntaughtFragment : SpeechFragment;
}
