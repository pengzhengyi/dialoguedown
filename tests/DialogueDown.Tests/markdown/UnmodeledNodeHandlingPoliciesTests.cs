using DialogueDown.Configuration;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Markdown;

/// <summary>
/// The policy the front-end reads for a project's configured unmodeled-Markdown handling: the
/// kinds a project named take the configured handling, and every kind it left out keeps its
/// built-in default.
/// </summary>
public sealed class UnmodeledNodeHandlingPoliciesTests
{
    [Fact]
    public void For_NoOverrides_IsTheSharedDefaultPolicy() =>
        // An unconfigured compile is the common case, so it allocates nothing.
        Assert.Same(DefaultUnmodeledNodeHandlingPolicy.Instance, For([]));

    [Fact]
    public void For_NoOverrides_KeepsEveryBuiltInDefault()
    {
        var policy = For([]);

        Assert.All(
            Enum.GetValues<UnmodeledNodeKind>(),
            kind => Assert.Equal(
                DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind),
                policy.HandlingFor(kind)));
    }

    [Fact]
    public void For_AnOverriddenKind_UsesTheOverride()
    {
        // A table is ignored by default; a project that writes dialogue in tables keeps it.
        var policy = For(new() { [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep });

        Assert.Equal(UnmodeledNodeHandling.Keep, policy.HandlingFor(UnmodeledNodeKind.Table));
    }

    [Fact]
    public void For_AnOmittedKind_FallsBackToItsDefault()
    {
        var policy = For(new() { [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep });

        // Untouched kinds keep the built-in defaults: a code block is still ignored, an autolink
        // is still kept.
        Assert.Equal(UnmodeledNodeHandling.Ignore, policy.HandlingFor(UnmodeledNodeKind.CodeBlock));
        Assert.Equal(UnmodeledNodeHandling.Keep, policy.HandlingFor(UnmodeledNodeKind.Autolink));
    }

    [Fact]
    public void For_AKindOverriddenToItsOwnDefault_KeepsThatHandling() =>
        // Restating a default is harmless, not a signal to fall through to something else.
        Assert.Equal(
            UnmodeledNodeHandling.Ignore,
            For(new() { [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Ignore })
                .HandlingFor(UnmodeledNodeKind.Table));

    [Fact]
    public void For_EveryKindOverridden_AppliesThemAll()
    {
        var overrides = Enum.GetValues<UnmodeledNodeKind>()
            .ToDictionary(kind => kind, _ => UnmodeledNodeHandling.Ignore);

        var policy = For(overrides);

        Assert.All(
            Enum.GetValues<UnmodeledNodeKind>(),
            kind => Assert.Equal(UnmodeledNodeHandling.Ignore, policy.HandlingFor(kind)));
    }

    [Fact]
    public void For_Overrides_IsNotTheSharedDefaultPolicy() =>
        Assert.NotSame(
            DefaultUnmodeledNodeHandlingPolicy.Instance,
            For(new() { [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep }));

    [Fact]
    public void For_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => UnmodeledNodeHandlingPolicies.For(null!));

    private static IUnmodeledNodeHandlingPolicy For(
        Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling> overrides) =>
        UnmodeledNodeHandlingPolicies.For(overrides);
}
