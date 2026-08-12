using DialogueDown.Configuration;
namespace DialogueDown.Markdown;

/// <summary>
/// The default handling policy: ignore authoring aids that are not dialogue (code
/// blocks, thematic breaks, tables) and keep everything else.
/// </summary>
internal sealed class DefaultUnmodeledNodeHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    private DefaultUnmodeledNodeHandlingPolicy()
    {
    }

    public static DefaultUnmodeledNodeHandlingPolicy Instance { get; } = new();

    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => kind switch
    {
        UnmodeledNodeKind.CodeBlock
            or UnmodeledNodeKind.ThematicBreak
            or UnmodeledNodeKind.Table => UnmodeledNodeHandling.Ignore,
        _ => UnmodeledNodeHandling.Keep,
    };
}
