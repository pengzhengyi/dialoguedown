using DialogueDown.Graph.Builder;

namespace DialogueDown.Tests.Support;

/// <summary>Object Mother for an empty graph draft with the default node-id strategy.</summary>
internal static class GraphDraftFactory
{
    public static GraphDraft Draft() => new(new IndexNodeIdBuilder());
}
