using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Resolves each <c>Jump</c>'s target against the <see cref="AnchorTable"/>, producing a
/// per-jump <see cref="JumpResolution"/>. A local anchor resolves to its scene, or — when no scene
/// slugs to it — is reported and left unresolved; the reserved <c>#END</c> ends the dialogue; a
/// target outside this script is reported and deferred; an empty target is left unresolved.
/// </summary>
internal static class JumpResolver
{
    /// <summary>
    /// Resolves every jump in <paramref name="jumps"/> against <paramref name="anchors"/>, reporting
    /// a missing local anchor or a target outside this script into <paramref name="diagnostics"/>.
    /// </summary>
    public static JumpResolutionTable Resolve(
        IEnumerable<Jump> jumps, AnchorTable anchors, IDiagnosticSink diagnostics) =>
        new(jumps.ToDictionary<Jump, Jump, JumpResolution>(
            jump => jump,
            jump => Resolve(jump, anchors, diagnostics),
            ReferenceEqualityComparer.Instance));

    private static JumpResolution Resolve(Jump jump, AnchorTable anchors, IDiagnosticSink diagnostics)
    {
        var target = JumpTarget.Parse(jump.Target);

        if (target.HasFilePart)
        {
            // TODO(cross-file): resolve the file part against other documents, including a path
            // that names the current file. Until then a file-scoped target is deferred, and the
            // writer is warned that the jump leads nowhere.
            diagnostics.Report(
                new Diagnostic(DiagnosticCatalog.ExternalJumpNotResolved, jump.Span, [target.File!]));
            return new FileScopedJump(target.File!, target.Anchor);
        }

        if (!target.HasAnchor)
        {
            return new UnresolvedJump();
        }

        // The reserved terminator is recognized before slug lookup, so it never depends on — or
        // collides with — an author's scene anchors.
        if (target.Anchor == ReservedAnchors.End)
        {
            return new TerminalJump();
        }

        if (anchors.TryResolve(target.Anchor!, out var scene))
        {
            return new SceneJump(scene);
        }

        // A missing local anchor is recoverable: report it and leave the jump unresolved so the
        // rest of analysis keeps running.
        diagnostics.Report(new Diagnostic(DiagnosticCatalog.MissingScene, jump.Span, [target.Anchor!]));
        return new UnresolvedJump();
    }
}
