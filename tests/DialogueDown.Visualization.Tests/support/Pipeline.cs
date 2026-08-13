using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Graph;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// Compiles script text through the default compiler and exposes the stage artifacts a projection
/// test reads — the transpiled Dialogue AST (<see cref="Document"/>) and the semantic model
/// (<see cref="Model"/>) — so a test reads the same real stages the report shows instead of a
/// hand-built tree.
/// </summary>
internal static class Pipeline
{
    public static CompilationResult Compilation(string source) =>
        Compile(source, CompilerOptions.Default);

    public static CompilationResult Compilation(string source, CompilerOptions options) =>
        Compile(source, options);

    public static ScriptDocument Document(string source) => Compile(source, CompilerOptions.Default).Script;

    public static SemanticModel Model(string source) => Model(source, CompilerOptions.Default);

    /// <summary>The dialogue graph a clean compile produced — the graph tab's input.</summary>
    public static DialogueGraph Graph(string source) =>
        Compile(source, CompilerOptions.Default) is CompilationSuccess success
            ? success.Graph
            : throw new InvalidOperationException(
                $"A projection test needs a compiled graph, but this source did not compile: {source}");

    public static SemanticModel Model(string source, CompilerOptions options) =>
        Compile(source, options) is CompilationSuccess success
            ? success.Semantics
            : throw new InvalidOperationException(
                $"A projection test needs a compiled model, but this source did not compile: {source}");

    private static CompilationResult Compile(string source, CompilerOptions options) =>
        ScriptCompilerFactory.CreateDefault(options).Compile(source);
}
