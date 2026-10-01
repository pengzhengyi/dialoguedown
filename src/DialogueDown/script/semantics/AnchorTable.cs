using System.Diagnostics.CodeAnalysis;
using DialogueDown.Common;
using DialogueDown.Diagnostics;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Maps each scene's slug <see cref="Scene.Anchor"/> to its <see cref="Scene"/>, so a
/// same-file jump resolves its target by anchor. An anchor is a jump target, so it must be
/// unambiguous: two scenes that slug the same are a conflict — the first is kept and the
/// duplicate reported, rather than the silent disambiguation GitHub applies to duplicate headings.
/// </summary>
internal sealed class AnchorTable
{
    private readonly Dictionary<string, Scene> _sceneByAnchor = [];

    /// <summary>
    /// Finds the scene for <paramref name="anchor"/>; false when no scene slugs to it.
    /// </summary>
    public bool TryResolve(string anchor, [MaybeNullWhen(false)] out Scene scene) =>
        _sceneByAnchor.TryGetValue(anchor, out scene);

    internal void Add(string anchor, Scene scene, SourceSpan span, IDiagnosticSink diagnostics)
    {
        if (!_sceneByAnchor.TryAdd(anchor, scene))
        {
            // Recovery: keep the first scene for the anchor; the duplicate stays in the tree but is
            // not a jump target.
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.DuplicateAnchor, span, [anchor]));
        }
    }
}
