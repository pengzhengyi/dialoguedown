namespace DialogueDown.Graph.Builder;

/// <summary>Creates the default sequential node-id builder.</summary>
internal sealed class IndexNodeIdBuilderFactory : INodeIdBuilderFactory
{
    public INodeIdBuilder Create() => new IndexNodeIdBuilder();
}
