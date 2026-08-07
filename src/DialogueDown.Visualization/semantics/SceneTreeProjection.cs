using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Semantics;

/// <summary>
/// Projects the analyzer's <see cref="Scene"/> tree for display: the implicit root scene, the
/// scenes nested under it, and — under each scene — the script blocks it owns. A scene is
/// labeled by its heading text and carries the cross-link key <c>scene:&lt;anchor&gt;</c>, so
/// hovering it highlights the same scene in the anchor and jump tables. Each scene's script
/// blocks are described by the shared <see cref="DialogueAstProjection"/>, so they read exactly
/// like the Desugared AST tab; a speaker mention or a jump that resolves to a scene additionally
/// carries a <see cref="NodeDescription.RefKey"/>, so hovering it lights up the matching speaker
/// or anchor/jump rows.
/// </summary>
internal sealed class SceneTreeProjection : INodeProjection<object>
{
    internal const string StageTitle = "Semantic Model";

    internal const string StageDescription =
        "The semantic model the analyzer resolves — the scene tree and its script blocks beside "
        + "the speaker, anchor, and jump-resolution tables, cross-linked so hovering a scene, "
        + "speaker, or jump highlights it everywhere.";

    private const string StructureCategory = "structure";
    private const string DocumentCategory = "document";

    private readonly SemanticModel _model;
    private readonly string _source;
    private readonly DialogueAstProjection _blocks;

    /// <summary>
    /// Projects <paramref name="model"/>'s scene tree, slicing block source from
    /// <paramref name="source"/> the same way the Desugared AST tab does.
    /// </summary>
    public SceneTreeProjection(SemanticModel model, string source)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(source);
        _model = model;
        _source = source;
        _blocks = new DialogueAstProjection(source);
    }

    public string Title => StageTitle;

    public string Description => StageDescription;

    public NodeDescription Describe(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node is Scene scene ? DescribeScene(scene) : DescribeBlock(node);
    }

    public IEnumerable<object> Neighbors(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node is Scene scene ? SceneNeighbors(scene) : _blocks.Neighbors(node);
    }

    // A scene owns its script blocks first, then its nested subscenes — document order.
    private static IEnumerable<object> SceneNeighbors(Scene scene)
    {
        foreach (var block in scene.Blocks)
        {
            yield return block;
        }

        foreach (var child in scene.ChildScenes)
        {
            yield return child;
        }
    }

    private NodeDescription DescribeScene(Scene scene)
    {
        if (scene.Anchor is null)
        {
            return new NodeDescription(
                "Document root", category: DocumentCategory, typeName: "Document");
        }

        return new NodeDescription(
            SceneEntity.Label(scene),
            [
                new DisplayAttribute("anchor", $"#{scene.Anchor}"),
                new DisplayAttribute("level", $"{scene.Level}"),
            ],
            source: scene.Heading is null ? null : Slice(scene.Heading.Span),
            category: StructureCategory,
            entityKey: SceneEntity.Key(scene),
            typeName: "Scene")
        {
            // A scene maps to its heading, so a client can jump to the line that opens it.
            Span = scene.Heading is null ? null : ToSpan(scene.Heading.Span),
        };
    }

    // The clamped structured span of a scene's heading, so its detail panel can jump to the
    // source. Mirrors Slice's clamping so a scene's span and its sliced source always agree.
    private DisplaySpan ToSpan(SourceSpan span)
    {
        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return new DisplaySpan(start, end);
    }

    // The original heading text a scene was opened by, so its detail panel shows real source.
    private string? Slice(SourceSpan span)
    {
        if (span.IsEmpty)
        {
            return null;
        }

        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return _source[start..end];
    }

    // A block reads exactly as on the Desugared AST tab, plus a ref key when it names a speaker
    // or resolves to a scene, so hovering it cross-highlights the matching table rows.
    private NodeDescription DescribeBlock(object node)
    {
        var described = _blocks.Describe(node);
        var refKey = RefKeyFor(node);
        return refKey is null
            ? described
            : new NodeDescription(
                described.Label,
                described.Attributes,
                described.Source,
                described.Category,
                described.EntityKey,
                described.TypeName,
                refKey);
    }

    private string? RefKeyFor(object node) => node switch
    {
        Speaker speaker => SpeakerEntity.Key(_model.Speakers.Resolve(speaker)),
        Jump jump => _model.Jumps.Resolve(jump) is SceneJump scene ? SceneEntity.Key(scene.Scene) : null,
        _ => null,
    };
}
