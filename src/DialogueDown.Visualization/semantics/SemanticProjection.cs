using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Semantics;

/// <summary>
/// Projects a <see cref="SemanticModel"/> into the Semantic tab's payload: the scene tree as a
/// display graph (via <see cref="SceneTreeProjection"/>) plus the speaker, anchor, and
/// jump-resolution tables. Everything shares cross-link keys — a scene node and its anchor and
/// jump rows all carry <c>scene:&lt;anchor&gt;</c> — so the report can highlight an entity
/// everywhere it appears. It reads the model through the friend-visible
/// <c>CompilationResult.Semantics</c>; the model itself is unchanged.
/// </summary>
internal sealed class SemanticProjection
{
    private const string StructureCategory = "structure";
    private const string SpeechCategory = "speech";
    private const string JumpCategory = "jump";

    /// <summary>
    /// A placeholder for the Semantic Model stage when the compile halted before analysis, so the
    /// model was never produced. It carries the stage's title and description with no graph.
    /// </summary>
    public static DisplayGraph Unavailable(string reason) =>
        DisplayGraph.ForUnavailableStage(
            SceneTreeProjection.StageTitle, SceneTreeProjection.StageDescription, reason);

    /// <summary>The scene tree as a graph, enriched with the three semantic tables.</summary>
    public DisplayGraph Project(SemanticModel model, string source)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(source);
        var graph = GraphWalk.Walk<object>(model.SceneRoot, new SceneTreeProjection(model, source));
        var index = DialogueTreeIndex.Build(model.Desugared);
        return graph with
        {
            Tables =
            [
                SpeakerTable(index, model),
                AnchorTable(model.SceneRoot),
                JumpTable(index, model),
            ],
        };
    }

    // Every distinct speaker the model resolved, in first-seen document order.
    private static SemanticTable SpeakerTable(DialogueTreeIndex index, SemanticModel model)
    {
        var seen = new HashSet<SpeakerSymbol>(ReferenceEqualityComparer.Instance);
        var rows = new List<SemanticRow>();
        foreach (var speaker in index.OfType<Speaker>())
        {
            var symbol = model.Speakers.Resolve(speaker);
            if (!seen.Add(symbol))
            {
                continue;
            }

            rows.Add(new SemanticRow(
                [
                    new SemanticCell(symbol.Name ?? "—", Category: SpeechCategory),
                    new SemanticCell(symbol.Id is not null ? $"@{symbol.Id}" : "—"),
                    new SemanticCell(TagsText(symbol)),
                    new SemanticCell(symbol.IsDefault ? "✓" : ""),
                ],
                EntityKey: SpeakerEntity.Key(symbol)));
        }

        return new SemanticTable(
            "Speakers", ["Name", "@id", "Tags", "Default"], rows, "No speakers.");
    }

    // Every anchored scene, walking the tree top-down (the root has no anchor).
    private static SemanticTable AnchorTable(Scene root)
    {
        var rows = new List<SemanticRow>();
        foreach (var scene in Descendants(root))
        {
            if (scene.Anchor is null)
            {
                continue;
            }

            rows.Add(new SemanticRow(
                [
                    new SemanticCell($"#{scene.Anchor}", Category: StructureCategory),
                    new SemanticCell(SceneEntity.Label(scene)),
                    new SemanticCell($"{scene.Level}"),
                ],
                EntityKey: SceneEntity.Key(scene)));
        }

        return new SemanticTable("Anchors", ["Anchor", "Scene", "Level"], rows, "No scenes.");
    }

    // Every analyzed jump paired with what it resolved to; a scene jump cross-links its scene.
    private static SemanticTable JumpTable(DialogueTreeIndex index, SemanticModel model)
    {
        var rows = new List<SemanticRow>();
        foreach (var jump in index.OfType<Jump>())
        {
            var (text, refKey) = ResolutionCell(model.Jumps.Resolve(jump));
            rows.Add(new SemanticRow(
                [
                    new SemanticCell(LabelText(jump), Category: JumpCategory),
                    new SemanticCell(jump.Target),
                    new SemanticCell(text, RefKey: refKey, Category: refKey is null ? null : StructureCategory),
                ]));
        }

        return new SemanticTable(
            "Jump resolutions", ["Jump", "Target", "Resolves to"], rows, "No jumps.");
    }

    // The resolution's display text and, for a scene jump, the scene's cross-link key.
    private static (string Text, string? RefKey) ResolutionCell(JumpResolution resolution) =>
        resolution switch
        {
            SceneJump scene => ($"=> {SceneEntity.Label(scene.Scene)}", SceneEntity.Key(scene.Scene)),
            TerminalJump => ("End sentinel", null),
            FileScopedJump file => ($"{file.File}{Anchor(file.Anchor)} (deferred)", null),
            UnresolvedJump => ("unresolved", null),
            _ => (resolution.ToString() ?? "", null),
        };

    private static string Anchor(string? anchor) => anchor is null ? "" : $"#{anchor}";

    private static string LabelText(Jump jump)
    {
        var label = InlineText.Of(jump.Label).Trim();
        return label.Length > 0 ? label : "(no label)";
    }

    private static string TagsText(SpeakerSymbol symbol) =>
        string.Join(" ", symbol.Tags.Select(tag => tag.Value is null ? $"#{tag.Name}" : $"#{tag.Name}={tag.Value}"));

    // Every scene at or below root, top-down (pre-order), so the anchor table reads in order.
    private static IEnumerable<Scene> Descendants(Scene scene)
    {
        yield return scene;
        foreach (var child in scene.Children)
        {
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
