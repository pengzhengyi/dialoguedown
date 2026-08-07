using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Resolves each <c>Jump</c>'s target against the <see cref="AnchorTable"/>, producing a
/// per-jump <see cref="JumpResolution"/>. A local anchor resolves to its scene, or — when no scene
/// slugs to it — is reported and left unresolved; a target outside this script is reported and
/// deferred; an empty target is left unresolved.
/// </summary>
internal static class JumpResolver
{
    /// <summary>
    /// Resolves every jump in <paramref name="jumps"/> against <paramref name="anchors"/>, reporting
    /// a jump to a missing local anchor into <paramref name="diagnostics"/>.
    /// </summary>
    public static JumpResolutionTable Resolve(
        IEnumerable<Jump> jumps, AnchorTable anchors, IDiagnosticSink diagnostics) =>
        new(jumps.ToDictionary(jump => jump, jump => Resolve(jump, anchors, diagnostics)));

    private static JumpResolution Resolve(Jump jump, AnchorTable anchors, IDiagnosticSink diagnostics)
    {
        var target = JumpTarget.Parse(jump.Target);

        if (target.HasFilePart)
        {
            // TODO(cross-file, #59): resolve the file part against other documents, including a
            // path that names the current file; until then a file-scoped target is deferred, and
            // the writer is warned that it wires no flow rather than being left to wonder.
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
