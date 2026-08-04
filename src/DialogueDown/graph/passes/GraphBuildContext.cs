using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// The immutable input shared by every graph-construction pass: the semantic model, diagnostic
/// context, and the reading-order views of the scene tree computed once per build — the
/// document-order blocks and each scene's entry block.
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
        Blocks = semantics.SceneRoot.DocumentOrder();
        _entryBlockByScene = semantics.SceneRoot.EntryBlocks();
    }

    /// <summary>The analyzed script being lowered.</summary>
    public SemanticModel Semantics { get; }

    /// <summary>The diagnostic context for this graph build.</summary>
    public DiagnosticsContext Diagnostics { get; }

    /// <summary>The script blocks in document order, computed once for this graph build.</summary>
    public IReadOnlyList<ScriptBlock> Blocks { get; }

    /// <summary>Resolves a line's speaker prefix to its resolved symbol.</summary>
    public SpeakerSymbol ResolveSpeaker(Speaker speaker) => Semantics.Speakers.Resolve(speaker);

    /// <summary>Resolves a jump to what it points at.</summary>
    public JumpResolution ResolveJump(Jump jump) => Semantics.Jumps.Resolve(jump);

    /// <summary>
    /// The block reaching <paramref name="scene"/> lands on, or null when nothing follows it.
    /// </summary>
    public ScriptBlock? EntryBlockOf(Scene scene) => _entryBlockByScene[scene];
}
