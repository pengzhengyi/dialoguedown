using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Semantics;

/// <summary>
/// Projects a <see cref="SemanticModel"/> into the editor's completable <see cref="SymbolSet"/>:
/// the validated jump targets (anchored scenes), and the resolved speakers, their canonical
/// <c>@id</c>s, and their merged tags. Unlike a text scan, this reads the analyzer's real
/// symbol table — so an id seen before its declaration still completes, and only anchors that
/// actually exist are offered. The browser merges these with a live document scan, so a name
/// being typed before recompilation still completes too.
/// </summary>
internal sealed class SymbolProjection
{
    public SymbolSet Project(SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var jumpTargets = new List<JumpTargetSymbol>();
        foreach (var scene in Descendants(model.SceneRoot))
        {
            if (scene.Anchor is not null)
            {
                jumpTargets.Add(new JumpTargetSymbol(scene.Anchor, SceneEntity.Label(scene)));
            }
        }
        jumpTargets.AddRange(SymbolSet.Baseline.JumpTargets);

        var speakers = new OrderedSet();
        var speakerIds = new OrderedSet();
        var tags = new OrderedSet();
        var seen = new HashSet<SpeakerSymbol>(ReferenceEqualityComparer.Instance);

        // Script speakers first, in document order, then any configured speaker a line never
        // used — so the whole cast completes, not only the names already typed.
        foreach (var speaker in DialogueTreeIndex.Build(model.Desugared).OfType<Speaker>())
        {
            Collect(model.Speakers.Resolve(speaker), seen, speakers, speakerIds, tags);
        }

        foreach (var symbol in model.Speakers.Symbols)
        {
            Collect(symbol, seen, speakers, speakerIds, tags);
        }

        return new SymbolSet(
            jumpTargets,
            speakers.Values,
            speakerIds.Values,
            tags.Values,
            SymbolSet.Baseline.ReservedTargets);
    }

    private static void Collect(
        SpeakerSymbol symbol, HashSet<SpeakerSymbol> seen,
        OrderedSet speakers, OrderedSet speakerIds, OrderedSet tags)
    {
        if (!seen.Add(symbol))
        {
            return;
        }

        if (symbol.Name is not null)
        {
            speakers.Add(symbol.Name);
        }

        if (symbol.Id is not null)
        {
            speakerIds.Add(symbol.Id);
        }

        foreach (var tag in symbol.Tags)
        {
            tags.Add(tag.Name);
        }
    }

    // Every scene at or below root, top-down (pre-order), matching the anchor table's order.
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

    // A small insertion-ordered, de-duplicating string collector (mirrors the browser scanner).
    private sealed class OrderedSet
    {
        private readonly HashSet<string> _seen = [];

        public List<string> Values { get; } = [];

        public void Add(string value)
        {
            if (_seen.Add(value))
            {
                Values.Add(value);
            }
        }
    }
}
