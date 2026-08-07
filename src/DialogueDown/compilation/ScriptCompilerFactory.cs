using DialogueDown.Configuration;
using DialogueDown.Graph.Builder;
using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Validation;

namespace DialogueDown.Compilation;

/// <summary>
/// The container-free composition root for the default <see cref="IScriptCompiler"/>: it
/// wires the standard stages — the Markdig-based parser, the default transpiler, the
/// desugarer, the structural validator, the semantic analyzer, and the graph builder — into a
/// ready compiler in one
/// call, for callers that do not run a dependency injection container. Container callers use the
/// <c>AddDialogueDown</c> registration instead; both build the same graph.
/// </summary>
public static class ScriptCompilerFactory
{
    /// <summary>
    /// Creates the default compiler with its standard stage graph, configured by
    /// <paramref name="options"/> (the unconfigured <see cref="CompilerOptions.Default"/> when null).
    /// </summary>
    public static IScriptCompiler CreateDefault(CompilerOptions? options = null)
    {
        options ??= CompilerOptions.Default;
        return new ScriptCompiler(
            new MarkdigMarkdownParser(),
            ScriptTranspilerFactory.CreateDefault(),
            new ScriptDesugarer(),
            StructuralValidatorFactory.CreateDefault(),
            new SemanticAnalyzer(options.ForSemanticAnalyzer()),
            DialogueGraphBuilderFactory.CreateDefault(),
            options.Mode);
    }
}
