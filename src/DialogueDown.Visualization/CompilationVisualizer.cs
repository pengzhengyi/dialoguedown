using System.Text;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Markdown;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Semantics;

namespace DialogueDown.Visualization;

/// <summary>
/// The entry facade for compilation visualization: it compiles a script string,
/// projects each available stage into a display graph, and assembles a single
/// self-contained, multi-tab HTML report. The live-visualization server consumes
/// the same facade to render and serialize a document.
/// </summary>
/// <remarks>
/// <see cref="BuildStages"/> projects the stages the <see cref="IScriptCompiler"/> seam
/// produces — the parsed Markdown AST, the transpiled Dialogue AST, the desugarer's
/// normalized Desugared AST, and the analyzer's Semantic Model (a scene-tree graph beside
/// its cross-linked tables) — each one a projection over the shared walk, model, and
/// renderers.
/// </remarks>
public sealed class CompilationVisualizer
{
    // Shown on a disabled stage tab when a halted compile did not produce that stage. The actual
    // diagnostics are surfaced separately (a planned report overlay), not here.
    private const string StageUnavailableReason =
        "This stage is unavailable due to compilation errors.";

    private readonly IScriptCompiler _compiler;
    private readonly AppliedConfiguration? _configuration;

    /// <summary>Creates a visualizer using the default compiler pipeline.</summary>
    public CompilationVisualizer()
        : this(CompilerOptions.Default)
    {
    }

    /// <summary>
    /// Creates a visualizer whose compiler is configured with <paramref name="options"/>, so the
    /// report — including the editor's completion symbols — reflects the project's speakers.
    /// </summary>
    public CompilationVisualizer(CompilerOptions options)
        : this(ScriptCompilerFactory.CreateDefault(ForVisualization(options)))
    {
    }

    /// <summary>
    /// Creates a visualizer for a report that shows its applied <paramref name="configuration"/>
    /// in the Config tab: the compiler is configured with the configuration's options, and the
    /// file and configured speakers are projected into the report payload.
    /// </summary>
    public CompilationVisualizer(AppliedConfiguration configuration)
        : this(ScriptCompilerFactory.CreateDefault(ForVisualization(
            (configuration ?? throw new ArgumentNullException(nameof(configuration))).Options)))
    {
        _configuration = configuration;
    }

    /// <summary>Creates a visualizer over a specific <paramref name="compiler"/>.</summary>
    internal CompilationVisualizer(IScriptCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        _compiler = compiler;
    }

    /// <summary>Compiles the source and projects each stage into a display graph.</summary>
    public IReadOnlyList<DisplayGraph> BuildStages(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return BuildContent(source).Stages;
    }

    /// <summary>
    /// Compiles the source and renders every stage as text in the given
    /// <paramref name="format"/> (Mermaid or DOT), joined with a per-stage header
    /// comment so a multi-stage emit is self-describing. For embedding a stage's graph
    /// elsewhere — the report itself stays HTML.
    /// </summary>
    public string RenderText(string source, EmitFormat format)
    {
        ArgumentNullException.ThrowIfNull(source);
        var renderer = RendererFor(format);
        var comment = CommentPrefixFor(format);
        var builder = new StringBuilder();
        var stages = BuildStages(source);
        for (var i = 0; i < stages.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(comment).Append(' ').Append(stages[i].Title).Append('\n');
            builder.Append(renderer.Render(stages[i]));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the local image references in the document, in document order — the
    /// sources of <c>![alt](src)</c> images that name a file rather than a web
    /// resource. Web and data URLs (<c>http:</c>, <c>https:</c>, <c>//</c>,
    /// <c>data:</c>) are excluded. The live server uses these to decide which folder
    /// it must serve so the report's images resolve.
    /// </summary>
    public IReadOnlyList<string> LocalImageReferences(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var projection = new MarkdownAstProjection(source);
        var references = new List<string>();
        CollectLocalImages(_compiler.Compile(source).Markdown, projection, references);
        return references;
    }

    /// <summary>
    /// Compiles the source and renders the static, multi-tab HTML report. When
    /// <paramref name="documentPath"/> is given it is shown in the report (the file
    /// being visualized); it does not make the report live.
    /// </summary>
    public string RenderHtmlReport(string source, string? documentPath = null)
    {
        var content = BuildContent(source);
        return HtmlTemplate.RenderPage(
            content.Stages, source, VisualizationMode.Static, documentPath, content.Symbols,
            content.Configuration, content.Diagnostics, content.SemanticTokens);
    }

    /// <summary>
    /// Compiles the source and renders the HTML report as a <b>live</b> report: the
    /// injected payload carries the given <paramref name="mode"/> (watch or live) and
    /// <paramref name="documentPath"/>, so the client opens a live session
    /// (subscribes for hot-reload pushes) instead of showing a static report.
    /// </summary>
    public string RenderLiveReport(
        string documentPath, string source, string mode, ConfigStatusOverlay? configOverlay = null,
        ReportProject? project = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        var content = BuildContent(source);
        return HtmlTemplate.RenderPage(
            content.Stages, source, mode, documentPath, content.Symbols, content.Configuration,
            content.Diagnostics, content.SemanticTokens, configOverlay, project);
    }

    /// <summary>
    /// Renders the served shell's <b>empty state</b>: a report carrying only the project (so the
    /// client mounts the Explorer over <paramref name="root"/>) with no active document, source,
    /// or stages. The reader opens a script from the tree or creates the first one.
    /// </summary>
    public string RenderEmptyShell(string root, string mode)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(mode);
        return HtmlTemplate.RenderPage(
            Array.Empty<DisplayGraph>(), mode: mode, project: new ReportProject(root, null));
    }

    /// <summary>
    /// Compiles the source and serializes the current document payload
    /// (<c>{ mode, path, source, stages }</c>) as JSON, for the live server's
    /// document API and its hot-reload push events.
    /// </summary>
    public string SerializeDocument(
        string documentPath, string source, string mode, ConfigStatusOverlay? configOverlay = null,
        ReportProject? project = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        var content = BuildContent(source);
        return DisplayGraphJson.SerializeDocument(
            mode, documentPath, source, content.Stages, content.Symbols, content.Configuration,
            content.Diagnostics, content.SemanticTokens, configOverlay, project);
    }

    private static IDisplayRenderer RendererFor(EmitFormat format) => format switch
    {
        EmitFormat.Mermaid => new MermaidRenderer(),
        EmitFormat.Dot => new DotRenderer(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown emit format."),
    };

    // The stage-header comment leader in each format's syntax.
    private static string CommentPrefixFor(EmitFormat format) => format switch
    {
        EmitFormat.Mermaid => "%%",
        EmitFormat.Dot => "//",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown emit format."),
    };

    private static void CollectLocalImages(
        object node, MarkdownAstProjection projection, List<string> references)
    {
        if (node is ImageInline image && IsLocalReference(image.Source))
        {
            references.Add(image.Source);
        }

        foreach (var child in projection.Neighbors(node))
        {
            CollectLocalImages(child, projection, references);
        }
    }

    private static bool IsLocalReference(string source) =>
        !source.StartsWith("//", StringComparison.Ordinal)
        && !(Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "data" or "ftp" or "mailto");

    // A visualization renders whatever the compiler produced. It compiles stage-boundary, so a
    // valid script yields every stage and a broken one halts: the transpiler's later material is
    // unreliable, so its stages are unavailable (disabled tabs) rather than projected from noise.
    // Fail-fast is never used — it throws instead of returning a renderable result.
    private static CompilerOptions ForVisualization(CompilerOptions options) =>
        options with { Mode = CompilationMode.StageBoundary };

    // Compiles the source once and projects both the stage graphs and the editor's resolved
    // symbols, so the report and the live document API share a single compilation.
    private ReportContent BuildContent(string source)
    {
        var result = _compiler.Compile(source);
        IReadOnlyList<DisplayGraph> stages = result.IsComplete
            ?
            [
                result.Markdown.ToDisplayGraph(source),
                result.Script.ToDisplayGraph(source),
                result.Desugared.ToDisplayGraph(source),
                new SemanticProjection().Project(result.Semantics, source),
            ]
            :
            [
                result.Markdown.ToDisplayGraph(source),
                result.Script.ToDisplayGraph(source),
                ScriptDisplayExtensions.DesugaredUnavailable(StageUnavailableReason),
                SemanticProjection.Unavailable(StageUnavailableReason),
            ];
        var symbols = result.IsComplete
            ? new SymbolProjection().Project(result.Semantics)
            : SymbolSet.Empty;
        var configuration = _configuration is null
            ? null
            : ConfigurationProjection.Project(_configuration);
        var diagnostics = new DiagnosticProjection().Project(result.LocatedDiagnostics);
        IReadOnlyList<SemanticToken> semanticTokens =
            [.. new SemanticTokenProjection().Project(result.Markdown, result.Script, source)];
        return new ReportContent(stages, symbols, configuration, diagnostics, semanticTokens);
    }

    // The compiled report data shared by the HTML report and the live document payload.
    private sealed record ReportContent(
        IReadOnlyList<DisplayGraph> Stages,
        SymbolSet Symbols,
        ConfigurationReport? Configuration,
        IReadOnlyList<LspDiagnostic> Diagnostics,
        IReadOnlyList<SemanticToken> SemanticTokens);
}
