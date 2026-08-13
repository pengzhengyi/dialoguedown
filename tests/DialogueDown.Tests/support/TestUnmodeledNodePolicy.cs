using DialogueDown.Configuration;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Test policy that starts from <see cref="DefaultUnmodeledNodeHandlingPolicy"/>
/// and overrides only the kinds a test cares about, so a test can express just its
/// deviation from the default. Instances are immutable; each override returns a new
/// policy.
/// </summary>
internal sealed class TestUnmodeledNodePolicy : IUnmodeledNodeHandlingPolicy
{
    private readonly IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> _overrides;

    private TestUnmodeledNodePolicy(
        IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> overrides) =>
        _overrides = overrides;

    /// <summary>Gets the default policy with no overrides applied.</summary>
    public static TestUnmodeledNodePolicy Default { get; } =
        new(new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>());

    /// <summary>Overrides <paramref name="kind"/> to keep it as raw text.</summary>
    public TestUnmodeledNodePolicy Keep(UnmodeledNodeKind kind) =>
        With(kind, UnmodeledNodeHandling.Keep);

    /// <summary>Overrides <paramref name="kind"/> to drop it.</summary>
    public TestUnmodeledNodePolicy Ignore(UnmodeledNodeKind kind) =>
        With(kind, UnmodeledNodeHandling.Ignore);

    /// <inheritdoc/>
    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) =>
        _overrides.TryGetValue(kind, out var handling)
            ? handling
            : DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind);

    private TestUnmodeledNodePolicy With(
        UnmodeledNodeKind kind, UnmodeledNodeHandling handling) =>
        new(new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>(_overrides)
        {
            [kind] = handling,
        });
}
