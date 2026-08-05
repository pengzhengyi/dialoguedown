using DialogueDown.Graph.Passes;

namespace DialogueDown.Tests.Support;

/// <summary>Object Mother for a <see cref="GraphBuildContext"/> over analyzed source.</summary>
internal static class GraphBuildContextFactory
{
    public static GraphBuildContext Context(string source) =>
        new(Pipeline.UntilAnalyzed(source), DiagnosticsContextFactory.Context(source));
}
