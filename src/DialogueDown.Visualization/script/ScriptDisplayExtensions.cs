using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Script;

/// <summary>
/// Ergonomic entry point for the dialogue stages: turns a transpiled
/// <see cref="ScriptDocument"/> or a normalized <see cref="DesugaredScriptDocument"/>
/// into a display graph without naming the projection. The <paramref name="source"/> is
/// the original script text, so each node can carry the snippet it was produced from.
/// The two stages share one projection over the same node vocabulary, under their own
/// tab titles.
/// </summary>
internal static class ScriptDisplayExtensions
{
    private const string DesugaredTitle = "Desugared AST";

    private const string DesugaredDescription =
        "The normalized dialogue tree the desugarer produces — jumps assembled from " +
        "an arrow and its link, and a default speaker filled on lines that name none.";

    public static DisplayGraph ToDisplayGraph(this ScriptDocument document, string source) =>
        GraphWalk.Walk<object>(document, new DialogueAstProjection(source));

    public static DisplayGraph ToDisplayGraph(this DesugaredScriptDocument document, string source) =>
        GraphWalk.Walk<object>(
            document.Document, new DialogueAstProjection(source, DesugaredTitle, DesugaredDescription));

    /// <summary>
    /// A placeholder for the Desugared AST stage when the compile halted before desugaring, so the
    /// normalized tree was never produced. It carries the stage's title and description with no graph.
    /// </summary>
    public static DisplayGraph DesugaredUnavailable(string reason) =>
        DisplayGraph.ForUnavailableStage(DesugaredTitle, DesugaredDescription, reason);
}
