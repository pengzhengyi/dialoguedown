using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Runs a composed set of <see cref="IDesugarRule"/>s over a Dialogue AST, in order, each rule's
/// output feeding the next. Composing the rules here keeps desugaring open to new normalizations
/// without touching the pipeline.
/// </summary>
internal sealed class Desugarer
{
    private readonly IReadOnlyList<IDesugarRule> _rules;

    public Desugarer(IReadOnlyList<IDesugarRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
    }

    public ScriptDocument Desugar(ScriptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = document;
        foreach (var rule in _rules)
        {
            result = rule.Apply(result);
        }

        return result;
    }
}
