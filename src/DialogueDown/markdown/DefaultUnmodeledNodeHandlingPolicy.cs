using DialogueDown.Configuration;
namespace DialogueDown.Markdown;

/// <summary>
/// The default handling policy: ignore authoring aids that are not dialogue (code
/// blocks, thematic breaks, tables, link reference definitions) and keep everything else.
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
            or UnmodeledNodeKind.Table
            or UnmodeledNodeKind.LinkReferenceDefinition => UnmodeledNodeHandling.Ignore,
        _ => UnmodeledNodeHandling.Keep,
    };
}
