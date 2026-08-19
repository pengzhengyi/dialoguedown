using System.Reflection;
using DialogueDown.Compilation;
using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization;
using DialogueDown.Visualization.Live;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Shared vocabulary for the architecture suite: the assemblies each rule scans
/// (anchored by one type apiece) and the namespace names that define the
/// project's boundaries. Centralizing them keeps every rule reading in one
/// consistent language.
/// </summary>
internal static class Architecture
{
    // Assembly-boundary namespaces (project roots).
    public const string Cli = "DialogueDown.Cli";
    public const string Visualization = "DialogueDown.Visualization";
    public const string VisualizationLive = "DialogueDown.Visualization.Live";
    public const string ConfigurationLoader = "DialogueDown.ConfigurationLoader";

    // Core internal layers, in pipeline order.
    public const string Common = "DialogueDown.Common";
    public const string Configuration = "DialogueDown.Configuration";
    public const string Diagnostics = "DialogueDown.Diagnostics";
    public const string Markdown = "DialogueDown.Markdown";
    public const string Graph = "DialogueDown.Graph";
    public const string Script = "DialogueDown.Script";
    public const string ScriptAst = "DialogueDown.Script.Ast";
    public const string ScriptDesugar = "DialogueDown.Script.Desugar";
    public const string ScriptValidation = "DialogueDown.Script.Validation";
    public const string ScriptSemantics = "DialogueDown.Script.Semantics";
    public const string ScriptTranspiler = "DialogueDown.Script.Transpiler";
    public const string Compilation = "DialogueDown.Compilation";

    // External presentation/host libraries the core must never reach for.
    public const string Markdig = "Markdig";
    public const string SpectreConsole = "Spectre.Console";
    public const string Godot = "Godot";
    public const string SystemConsole = "System.Console";

    /// <summary>The engine-agnostic compiler core (source to dialogue graph).</summary>
    public static readonly Assembly CoreAssembly = typeof(IScriptCompiler).Assembly;

    /// <summary>The static visualization layer built on the core.</summary>
    public static readonly Assembly VisualizationAssembly = typeof(CompilationVisualizer).Assembly;

    /// <summary>The live (browser) visualization layer built on visualization.</summary>
    public static readonly Assembly VisualizationLiveAssembly = typeof(IVisualizeRunner).Assembly;

    /// <summary>The TOML configuration-loader satellite built on the core.</summary>
    public static readonly Assembly ConfigurationLoaderAssembly =
        typeof(TomlConfigurationLoader).Assembly;

    /// <summary>
    /// The command-line front-end. Loaded by name because the CLI exposes no public
    /// type to anchor with <c>typeof</c> — even its entry point is the internal
    /// <c>Program</c> that top-level statements generate. The project reference puts
    /// the assembly beside this one, so the name resolves.
    /// </summary>
    public static readonly Assembly CliAssembly = Assembly.Load(Cli);

    /// <summary>Every assembly this suite scans, for rules that sweep them uniformly.</summary>
    public static readonly IReadOnlyList<Assembly> AllAssemblies =
    [
        CoreAssembly,
        ConfigurationLoaderAssembly,
        VisualizationAssembly,
        VisualizationLiveAssembly,
        CliAssembly,
    ];
}
