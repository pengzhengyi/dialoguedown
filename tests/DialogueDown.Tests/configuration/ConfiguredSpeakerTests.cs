using DialogueDown.Configuration;
using static DialogueDown.Tests.Support.ConfigurationFactory;

namespace DialogueDown.Tests.Configuration;

public sealed class ConfiguredSpeakerTests
{
    [Fact]
    public void Constructor_NullCustomTags_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new ConfiguredSpeaker("Alice", null, null!, []));

    [Fact]
    public void Constructor_NullReservedTags_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new ConfiguredSpeaker("Alice", null, [], null!));

    [Fact]
    public void Constructor_SnapshotsCustomTags()
    {
        var source = new List<ConfiguredTag> { ConfiguredTag("mood", "calm") };
        var speaker = new ConfiguredSpeaker("Alice", null, source, []);

        source.Add(ConfiguredTag("quest", "started"));

        Assert.Equal(ConfiguredTag("mood", "calm"), Assert.Single(speaker.CustomTags));
    }

    [Fact]
    public void Constructor_SnapshotsReservedTags()
    {
        var source = new List<ConfiguredTag> { DefaultTag() };
        var speaker = new ConfiguredSpeaker("Alice", null, [], source);

        source.Add(ConfiguredTag("voice", "alto"));

        Assert.Equal(DefaultTag(), Assert.Single(speaker.ReservedTags));
    }

    [Fact]
    public void Constructor_SourceMutationDoesNotChangeHashCode()
    {
        var source = new List<ConfiguredTag> { ConfiguredTag("mood", "calm") };
        var speaker = new ConfiguredSpeaker("Alice", null, source, []);
        int hash = speaker.GetHashCode();

        source.Add(ConfiguredTag("quest", "started"));

        Assert.Equal(hash, speaker.GetHashCode());
    }

    [Fact]
    public void Deconstruct_ReturnsEveryConfiguredPart()
    {
        var speaker = ConfiguredSpeaker(
            "Alice", "alice",
            customTags: [ConfiguredTag("mood", "calm")],
            reservedTags: [DefaultTag()]);

        var (name, id, customTags, reservedTags) = speaker;

        Assert.Equal("Alice", name);
        Assert.Equal("alice", id);
        Assert.Equal(ConfiguredTag("mood", "calm"), Assert.Single(customTags));
        Assert.Equal(DefaultTag(), Assert.Single(reservedTags));
    }

    [Fact]
    public void With_DefaultCustomTags_Throws()
    {
        var speaker = ConfiguredSpeaker("Alice");

        Assert.Throws<ArgumentException>(() => speaker with { CustomTags = default });
    }

    [Fact]
    public void With_DefaultReservedTags_Throws()
    {
        var speaker = ConfiguredSpeaker("Alice");

        Assert.Throws<ArgumentException>(() => speaker with { ReservedTags = default });
    }

    [Fact]
    public void Equality_EquivalentSeparatelyAllocatedSpeakers_AreEqual()
    {
        var left = ConfiguredSpeaker(
            "Alice", "alice",
            customTags: [ConfiguredTag("mood", "calm")],
            reservedTags: [DefaultTag()]);
        var right = ConfiguredSpeaker(
            "Alice", "alice",
            customTags: [ConfiguredTag("mood", "calm")],
            reservedTags: [DefaultTag()]);

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentNameOrId_IsUnequal()
    {
        var baseline = ConfiguredSpeaker("Alice", "alice");

        Assert.NotEqual(baseline, ConfiguredSpeaker("Bob", "alice"));
        Assert.NotEqual(baseline, ConfiguredSpeaker("Alice", "other"));
    }

    [Fact]
    public void Equality_CustomTagOrder_IsSignificant()
    {
        var first = ConfiguredTag("first");
        var second = ConfiguredTag("second");

        Assert.NotEqual(
            ConfiguredSpeaker("Alice", customTags: [first, second]),
            ConfiguredSpeaker("Alice", customTags: [second, first]));
    }

    [Fact]
    public void Equality_ReservedTagOrder_IsSignificant()
    {
        var first = ConfiguredTag("first");
        var second = ConfiguredTag("second");

        Assert.NotEqual(
            ConfiguredSpeaker("Alice", reservedTags: [first, second]),
            ConfiguredSpeaker("Alice", reservedTags: [second, first]));
    }

    [Fact]
    public void Equality_DifferentTagPartition_IsUnequal()
    {
        var tag = ConfiguredTag("default");

        Assert.NotEqual(
            ConfiguredSpeaker("Alice", customTags: [tag]),
            ConfiguredSpeaker("Alice", reservedTags: [tag]));
    }
}
