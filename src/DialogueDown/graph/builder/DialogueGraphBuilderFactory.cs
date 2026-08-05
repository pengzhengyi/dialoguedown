using DialogueDown.Graph.Passes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// Creates a graph builder with DialogueDown's built-in passes, in dependency order. Every
/// composition root that needs the graph stage builds it here, so they all run the same passes.
/// </summary>
internal static class DialogueGraphBuilderFactory
{
    public static DialogueGraphBuilder CreateDefault() =>
        new(
            new IndexNodeIdBuilderFactory(),
            [
                new NodeCreationPass(),
                new DivertPass(),
                new ChoicePass(),
                new SuccessionPass(),
                new RegionPass(),
            ]);
}
