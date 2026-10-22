using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Base class for a structural validation rule. It takes the boilerplate — the argument guards
/// and building each diagnostic from this rule's descriptor — so a concrete rule only walks the
/// indexed nodes and calls <c>report(span)</c> where it finds a problem, adding the message's
/// arguments and any fixes the problem has.
/// </summary>
internal abstract class DiagnosticRule : IDiagnosticRule
{
    /// <summary>
    /// Reports a finding of this rule at <paramref name="span"/>, with the values that fill its
    /// message and its fixes in preference order, as <see cref="Diagnostic"/> takes them.
    /// </summary>
    protected delegate void Reporter(
        SourceSpan span,
        IReadOnlyList<object>? arguments = null,
        IReadOnlyList<DiagnosticFix>? fixes = null);

    /// <summary>The kind of diagnostic this rule reports.</summary>
    protected abstract DiagnosticDescriptor Descriptor { get; }

    public void Check(DialogueTreeIndex nodes, IDiagnosticSink diagnostics)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        // The reporter closes over the sink so a rule reports with just what it found, and stays
        // stateless — the sink is never held on the rule, so a shared rule instance is safe across
        // concurrent compilations.
        Analyze(nodes, (span, arguments, fixes) =>
            diagnostics.Report(new Diagnostic(Descriptor, span, arguments ?? [], fixes: fixes)));
    }

    /// <summary>Inspects the indexed nodes and reports findings through <paramref name="report"/>.</summary>
    protected abstract void Analyze(DialogueTreeIndex nodes, Reporter report);
}
