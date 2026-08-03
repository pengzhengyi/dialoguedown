namespace DialogueDown.Graph;

/// <summary>
/// Creates an isolated node-id assignment session for each graph build.
/// </summary>
internal interface INodeIdBuilderFactory
{
    /// <summary>Creates an empty node-id builder.</summary>
    INodeIdBuilder Create();
}
