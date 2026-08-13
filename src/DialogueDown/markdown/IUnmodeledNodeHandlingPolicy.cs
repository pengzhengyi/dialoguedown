using DialogueDown.Configuration;
namespace DialogueDown.Markdown;

/// <summary>
/// Decides how the front-end handles each kind of unmodeled Markdown construct —
/// keep it or ignore it. Supply a custom implementation to override
/// the <see cref="DefaultUnmodeledNodeHandlingPolicy"/> defaults.
/// </summary>
internal interface IUnmodeledNodeHandlingPolicy
{
    /// <summary>Returns the handling for a given unmodeled node kind.</summary>
    UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind);
}
