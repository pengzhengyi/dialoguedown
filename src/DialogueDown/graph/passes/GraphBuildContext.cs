using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// The immutable input shared by every graph-construction pass: the semantic model, diagnostic
/// context, and the reading-order views of the scene tree computed once per build — the blocks in
/// document order, both top-level and nested, and each scene's entry block.
/// </summary>
internal sealed class GraphBuildContext
{
    private readonly IReadOnlyDictionary<Scene, ScriptBlock?> _entryBlockByScene;

    public GraphBuildContext(SemanticModel semantics, DiagnosticsContext diagnostics)
    {
        ArgumentNullException.ThrowIfNull(semantics);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Semantics = semantics;
        Diagnostics = diagnostics;
        TopLevelBlocks = semantics.SceneRoot.DocumentOrder();
        AllBlocks = [.. TopLevelBlocks.SelectMany(block => block.DescendantsAndSelf().OfType<ScriptBlock>())];
        _entryBlockByScene = semantics.SceneRoot.EntryBlocks();
        DocumentEnd = new SourceSpan(
            TopLevelBlocks.Count > 0 ? TopLevelBlocks[^1].Span.End : 0, length: 0);
    }

    /// <summary>The analyzed script being lowered.</summary>
    public SemanticModel Semantics { get; }

    /// <summary>The diagnostic context for this graph build.</summary>
    public DiagnosticsContext Diagnostics { get; }

    /// <summary>
    /// The blocks the scenes own directly, in document order — the document's own sequence, which
    /// a pass chains against a continuation. It excludes blocks nested inside another block.
    /// </summary>
    public IReadOnlyList<ScriptBlock> TopLevelBlocks { get; }

    /// <summary>
    /// Every block in document order, adding those nested inside another block's body — a choice
    /// option's or a control branch's — to <see cref="TopLevelBlocks"/>. Each one becomes a node.
    /// </summary>
    public IReadOnlyList<ScriptBlock> AllBlocks { get; }

    /// <summary>
    /// A zero-width span just past the document's last block — where the End node belongs, since
    /// it is synthetic and owns no source text of its own.
    /// </summary>
    public SourceSpan DocumentEnd { get; }

    /// <summary>Resolves a line's speaker prefix to its resolved symbol.</summary>
    public SpeakerSymbol ResolveSpeaker(Speaker speaker) => Semantics.Speakers.Resolve(speaker);

    /// <summary>Resolves a jump to what it points at.</summary>
    public JumpResolution ResolveJump(Jump jump) => Semantics.Jumps.Resolve(jump);

    /// <summary>
    /// The block reaching <paramref name="scene"/> lands on, or null when nothing follows it.
    /// </summary>
    public ScriptBlock? EntryBlockOf(Scene scene) => _entryBlockByScene[scene];
}
