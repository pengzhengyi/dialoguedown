using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a <see cref="SceneHeading"/> nested inside a control branch or choice option. Scene
/// construction reads headings only from the document body, so a nested heading would otherwise
/// create neither a scene nor a jump target.
/// </summary>
internal sealed class SceneHeadingPlacementRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.SceneHeadingInsideBranch;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var heading in nodes.OfType<SceneHeading>())
        {
            if (nodes.AncestorsOf(heading).Any(IsBranchBodyOwner))
            {
                report(heading.Span);
            }
        }
    }

    private static bool IsBranchBodyOwner(ScriptNode node) =>
        node is Branch or Choice or RandomOption;
}
