using DialogueDown.Configuration;

namespace DialogueDown.Markdown;

/// <summary>
/// A handling policy built from a project's configuration: each kind the project named uses the
/// configured handling, and every kind it left out keeps its
/// <see cref="DefaultUnmodeledNodeHandlingPolicy"/> default. A partial configuration is therefore
/// an override list rather than a replacement, so a project states only what it wants to differ.
/// </summary>
internal sealed class ConfiguredUnmodeledNodeHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    private readonly IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> _overrides;

    public ConfiguredUnmodeledNodeHandlingPolicy(
        IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);
        _overrides = overrides;
    }

    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) =>
        _overrides.TryGetValue(kind, out var handling)
            ? handling
            : DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind);
}
