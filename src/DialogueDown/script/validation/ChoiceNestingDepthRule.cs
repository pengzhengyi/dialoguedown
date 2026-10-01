using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Warns at the first choice group nested deeper than the recommended depth. A group is a player
/// <see cref="Choices"/> or a <see cref="RandomChoices"/>; both add source-indentation depth, so
/// both count. Groups nested further inside the warned one are not warned again; a sibling that
/// is also too deep is warned separately.
/// </summary>
internal sealed class ChoiceNestingDepthRule : DiagnosticRule
{
    private const int DefaultMaximumNestingLevel = 3;
    private readonly int _maximumNestingLevel;

    public ChoiceNestingDepthRule(int maximumNestingLevel = DefaultMaximumNestingLevel)
    {
        if (maximumNestingLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumNestingLevel),
                maximumNestingLevel,
                "Maximum choice nesting level must be positive.");
        }

        _maximumNestingLevel = maximumNestingLevel;
    }

    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.DeeplyNestedChoiceBranch;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var group in nodes.OfType<ChoiceGroup>())
        {
            var enclosingChoiceCount = nodes.AncestorsOf(group).Count(node => node is ChoiceGroup);
            if (enclosingChoiceCount == _maximumNestingLevel)
            {
                report(
                    SourceSpan.EmptyAt(group.Span.Start),
                    enclosingChoiceCount + 1,
                    _maximumNestingLevel);
            }
        }
    }
}
