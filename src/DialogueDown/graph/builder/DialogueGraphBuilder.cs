using DialogueDown.Diagnostics;
using DialogueDown.Graph.Passes;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Builder;

/// <inheritdoc />
internal sealed class DialogueGraphBuilder : IDialogueGraphBuilder
{
    private readonly INodeIdBuilderFactory _idBuilderFactory;
    private readonly IReadOnlyList<IGraphBuildPass> _passes;

    public DialogueGraphBuilder(
        INodeIdBuilderFactory idBuilderFactory, IReadOnlyList<IGraphBuildPass> passes)
    {
        ArgumentNullException.ThrowIfNull(idBuilderFactory);
        ArgumentNullException.ThrowIfNull(passes);
        _idBuilderFactory = idBuilderFactory;
        _passes = passes;
    }

    public DialogueGraph Build(SemanticModel model, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        var buildContext = new GraphBuildContext(model, context);
        var draft = new GraphDraft(_idBuilderFactory.Create());
        foreach (var pass in _passes)
        {
            pass.Apply(draft, buildContext);
        }

        return draft.Freeze();
    }
}
