using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Runs a composed set of structural <see cref="IDiagnosticRule"/>s over a desugared document.
/// It builds the type index once and hands it to every rule, so all rules share one traversal and
/// report into the same sink.
/// </summary>
internal sealed class StructuralValidator : IStructuralValidator
{
    private readonly IReadOnlyList<IDiagnosticRule> _rules;

    public StructuralValidator(IReadOnlyList<IDiagnosticRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
    }

    public void Validate(DesugaredScriptDocument document, IDiagnosticSink diagnostics)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var nodes = DialogueTreeIndex.Build(document);
        foreach (var rule in _rules)
        {
            rule.Check(nodes, diagnostics);
        }
    }
}
