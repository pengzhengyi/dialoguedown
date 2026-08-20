using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Editor;

namespace DialogueDown.Visualization.Render;

/// <summary>
/// Assembles a self-contained HTML report from one or more display graphs and,
/// optionally, the source they were compiled from. The page itself — D3, marked,
/// Pico.css, Tippy, the stylesheet, and the client script — is built ahead of
/// time by the <c>web/</c> Vite project into a single file
/// (<c>web/dist/report.html</c>) that is embedded in this assembly. All this step
/// does is inject the report data (the source and each stage) into that file's
/// data slot, so the report opens in any modern browser with no network and no
/// files on disk. The source becomes a "Source" tab and each graph becomes a
/// stage tab. Used for a single graph (<see cref="HtmlRenderer"/>) and for the
/// multi-stage report.
/// </summary>
internal static class HtmlTemplate
{
    private const string ReportSlot = "\"__REPORT__\"";

    public static string RenderPage(
        IReadOnlyList<DisplayGraph> stages,
        string? source = null,
        string mode = VisualizationMode.Static,
        string? path = null,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null,
        IReadOnlyList<LspDiagnostic>? diagnostics = null,
        IReadOnlyList<SemanticToken>? semanticTokens = null,
        ConfigStatusOverlay? configOverlay = null,
        ReportProject? project = null)
    {
        return Fill(
            EmbeddedAsset.ReadText("report.html"), stages, source, mode, path, symbols,
            configuration, diagnostics, semanticTokens, configOverlay, project);
    }

    /// <summary>
    /// Renders the same report as <see cref="RenderPage"/>, but linking the client rather than
    /// inlining it, for a server that can also serve <see cref="ReportBundle"/>'s assets. Every
    /// document then shares one download and one compile of the client, and the page carries only
    /// its own payload. Never use this for a file that leaves the server: nothing would resolve.
    /// </summary>
    public static string RenderLinkedPage(
        IReadOnlyList<DisplayGraph> stages,
        string? source = null,
        string mode = VisualizationMode.Static,
        string? path = null,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null,
        IReadOnlyList<LspDiagnostic>? diagnostics = null,
        IReadOnlyList<SemanticToken>? semanticTokens = null,
        ConfigStatusOverlay? configOverlay = null,
        ReportProject? project = null)
    {
        return Fill(
            ReportBundle.Default.LinkedHtml, stages, source, mode, path, symbols,
            configuration, diagnostics, semanticTokens, configOverlay, project);
    }

    private static string Fill(
        string template,
        IReadOnlyList<DisplayGraph> stages,
        string? source,
        string mode,
        string? path,
        SymbolSet? symbols,
        ConfigurationReport? configuration,
        IReadOnlyList<LspDiagnostic>? diagnostics,
        IReadOnlyList<SemanticToken>? semanticTokens,
        ConfigStatusOverlay? configOverlay,
        ReportProject? project)
    {
        return template.Replace(
            ReportSlot,
            DisplayGraphJson.SerializeReport(
                mode, path, source, stages, symbols, configuration, diagnostics,
                semanticTokens, configOverlay, project),
            StringComparison.Ordinal);
    }
}
