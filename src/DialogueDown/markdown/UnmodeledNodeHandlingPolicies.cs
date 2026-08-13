using DialogueDown.Configuration;

namespace DialogueDown.Markdown;

/// <summary>
/// Builds the handling policy the front-end reads from a project's configured overrides. With no
/// overrides this is the shared <see cref="DefaultUnmodeledNodeHandlingPolicy"/>, so an
/// unconfigured compile allocates nothing. Lives here rather than on
/// <see cref="CompilerOptions"/> because configuration is a foundation layer that must not depend
/// on the Markdown front-end.
/// </summary>
internal static class UnmodeledNodeHandlingPolicies
{
    public static IUnmodeledNodeHandlingPolicy For(
        IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);
        return overrides.Count == 0
            ? DefaultUnmodeledNodeHandlingPolicy.Instance
            : new ConfiguredUnmodeledNodeHandlingPolicy(overrides);
    }
}
