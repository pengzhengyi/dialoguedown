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
        return EmbeddedAsset.ReadText("report.html")
            .Replace(
                ReportSlot,
                DisplayGraphJson.SerializeReport(
                    mode, path, source, stages, symbols, configuration, diagnostics,
                    semanticTokens, configOverlay, project));
    }
}
