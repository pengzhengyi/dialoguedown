using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The analyzed artifact: the desugared tree together with what analysis resolved about it —
/// the speaker table, the scene tree and its anchors, and the per-jump resolutions. Like
/// Roslyn's <c>SemanticModel</c>, it is bound to the tree it analyzed: the AST stays immutable
/// and the model annotates it through side tables keyed by its nodes. The graph builder takes
/// this model, so analysis cannot be skipped.
/// </summary>
internal sealed class SemanticModel
{
    internal SemanticModel(
        DesugaredScriptDocument desugared,
        SpeakerTable speakers,
        Scene sceneRoot,
        AnchorTable anchors,
        JumpResolutionTable jumps)
    {
        ArgumentNullException.ThrowIfNull(desugared);
        ArgumentNullException.ThrowIfNull(speakers);
        ArgumentNullException.ThrowIfNull(sceneRoot);
        ArgumentNullException.ThrowIfNull(anchors);
        ArgumentNullException.ThrowIfNull(jumps);
        Desugared = desugared;
        Speakers = speakers;
        SceneRoot = sceneRoot;
        Anchors = anchors;
        Jumps = jumps;
    }

    /// <summary>The desugared tree this model was analyzed from and annotates.</summary>
    public DesugaredScriptDocument Desugared { get; }

    /// <summary>Resolves a line's speaker prefix to its symbol; the table owns the rules.</summary>
    public SpeakerTable Speakers { get; }

    /// <summary>The document's implicit root scene, the top of the scene tree.</summary>
    public Scene SceneRoot { get; }

    /// <summary>Maps a scene's anchor slug to its scene.</summary>
    public AnchorTable Anchors { get; }

    /// <summary>What each jump resolved to, keyed by the jump node.</summary>
    public JumpResolutionTable Jumps { get; }
}
