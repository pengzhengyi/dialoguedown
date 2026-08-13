using System.Collections.Immutable;
using DialogueDown.Configuration;
using static DialogueDown.Tests.Support.ConfigurationFactory;

namespace DialogueDown.Tests.Configuration;

public sealed class CompilerOptionsTests
{
    [Fact]
    public void Default_HasNoConfiguredSpeakers() =>
        Assert.Empty(CompilerOptions.Default.Speakers);

    [Fact]
    public void Default_UsesStageBoundaryMode() =>
        Assert.Equal(CompilationMode.StageBoundary, CompilerOptions.Default.Mode);

    [Fact]
    public void Mode_CanBeConfigured() =>
        Assert.Equal(
            CompilationMode.BestEffort,
            (CompilerOptions.Default with { Mode = CompilationMode.BestEffort }).Mode);

    [Fact]
    public void Speakers_AreEmptyOnAFreshInstance() =>
        Assert.Empty(new CompilerOptions().Speakers);

    [Fact]
    public void Collections_AreExposedAsImmutableTypes()
    {
        ImmutableArray<ConfiguredSpeaker> speakers = CompilerOptions.Default.Speakers;
        ImmutableDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> unmodeled =
            CompilerOptions.Default.UnmodeledMarkdown;

        Assert.Empty(speakers);
        Assert.Empty(unmodeled);
    }

    [Fact]
    public void Constructor_NullSpeakers_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CompilerOptions(
            CompilationMode.StageBoundary,
            null!,
            []));

    [Fact]
    public void Constructor_NullUnmodeledMarkdown_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CompilerOptions(
            CompilationMode.StageBoundary,
            [],
            null!));

    [Fact]
    public void Constructor_SnapshotsSourceCollectionsAndHash()
    {
        var speakers = new List<ConfiguredSpeaker> { ConfiguredSpeaker("Alice") };
        var unmodeled = new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
        {
            [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep,
        };
        var options = new CompilerOptions(CompilationMode.StageBoundary, speakers, unmodeled);
        int hash = options.GetHashCode();

        speakers.Add(ConfiguredSpeaker("Bob"));
        unmodeled[UnmodeledNodeKind.RawHtml] = UnmodeledNodeHandling.Ignore;

        Assert.Equal("Alice", Assert.Single(options.Speakers).Name);
        Assert.False(options.UnmodeledMarkdown.ContainsKey(UnmodeledNodeKind.RawHtml));
        Assert.Equal(hash, options.GetHashCode());
    }

    [Fact]
    public void Equality_EquivalentSeparatelyAllocatedOptions_AreEqual()
    {
        var left = new CompilerOptions(
            CompilationMode.BestEffort,
            [ConfiguredSpeaker("Alice", customTags: [ConfiguredTag("mood", "calm")])],
            new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
            {
                [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep,
                [UnmodeledNodeKind.RawHtml] = UnmodeledNodeHandling.Ignore,
            });
        var right = new CompilerOptions(
            CompilationMode.BestEffort,
            [ConfiguredSpeaker("Alice", customTags: [ConfiguredTag("mood", "calm")])],
            new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
            {
                [UnmodeledNodeKind.RawHtml] = UnmodeledNodeHandling.Ignore,
                [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep,
            });

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_SpeakerOrder_IsSignificant()
    {
        var alice = ConfiguredSpeaker("Alice");
        var bob = ConfiguredSpeaker("Bob");

        Assert.NotEqual(
            Options([alice, bob]),
            Options([bob, alice]));
    }

    [Fact]
    public void Equality_DifferentModeSpeakerOrOverride_IsUnequal()
    {
        var baseline = Options(
            [ConfiguredSpeaker("Alice")],
            new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
            {
                [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep,
            });

        Assert.NotEqual(
            baseline,
            baseline with { Mode = CompilationMode.BestEffort });
        Assert.NotEqual(
            baseline,
            Options([ConfiguredSpeaker("Bob")], baseline.UnmodeledMarkdown));
        Assert.NotEqual(
            baseline,
            Options(
                baseline.Speakers,
                new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
                {
                    [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Ignore,
                }));
    }

    [Fact]
    public void With_DefaultSpeakers_Throws() =>
        Assert.Throws<ArgumentException>(
            () => CompilerOptions.Default with { Speakers = default });

    [Fact]
    public void With_NullUnmodeledMarkdown_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => CompilerOptions.Default with { UnmodeledMarkdown = null! });

    [Fact]
    public void ForSemanticAnalyzer_ExposesTheConfiguredSpeakers()
    {
        var narrator = ConfiguredSpeaker(
            "Narrator", "narrator",
            customTags: [ConfiguredTag("mood", "happy")],
            reservedTags: [DefaultTag()]);
        var options = new CompilerOptions { Speakers = [narrator] };

        var configured = Assert.Single(options.ForSemanticAnalyzer().ConfiguredSpeakers);
        Assert.Equal("Narrator", configured.Name);
        Assert.Equal("narrator", configured.Id);
        Assert.Equal(ConfiguredTag("mood", "happy"), Assert.Single(configured.CustomTags));
        Assert.Equal(DefaultTag(), Assert.Single(configured.ReservedTags));
    }

    private static CompilerOptions Options(
        IEnumerable<ConfiguredSpeaker> speakers,
        IEnumerable<KeyValuePair<UnmodeledNodeKind, UnmodeledNodeHandling>>? unmodeled = null) =>
        new(
            CompilationMode.StageBoundary,
            speakers,
            unmodeled ?? []);
}
