using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Playbook;

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
    private const string MermaidSlot = "\"__MERMAID__\"";
    private const string ScriptTag = """<script type="module" crossorigin src="/report.js"></script>""";
    private const string StyleTag = """<link rel="stylesheet" crossorigin href="/report.css">""";

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
        ReportProject? project = null,
        PlaybookReport? playbook = null)
    {
        return Fill(
            SelfContained(source), stages, source, mode, path, symbols,
            configuration, diagnostics, semanticTokens, configOverlay, project, playbook);
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
        ReportProject? project = null,
        PlaybookReport? playbook = null)
    {
        return Fill(
            Linked(), stages, source, mode, path, symbols,
            configuration, diagnostics, semanticTokens, configOverlay, project, playbook);
    }

    // Everything inlined, so the file opens from disk with no server and no network. Mermaid is
    // the one asset that comes and goes: it is larger than the rest of the report together, so it
    // rides along only for a script that actually draws a diagram.
    private static string SelfContained(string? source)
    {
        var bundle = ReportBundle.Default;
        // Substitute into the small page first and grow it last, so every anchor is matched
        // against markup rather than against a megabyte of inlined script.
        var page = ReplaceOnce(bundle.Page, MermaidSlot, "\"\"");
        if (MermaidFence.AppearsIn(source))
        {
            page = ReplaceOnce(page, "</head>", $"<script>{bundle.Mermaid}</script></head>");
        }

        page = ReplaceOnce(page, StyleTag, $"<style>{bundle.Style}</style>");
        return ReplaceOnce(page, ScriptTag, $"<script type=\"module\">{bundle.Script}</script>");
    }

    // Assets referred to by content-addressed paths the server hosts, so one download and one
    // compile of the client serve every script a reader opens.
    private static string Linked()
    {
        var bundle = ReportBundle.Default;
        var page = ReplaceOnce(bundle.Page, MermaidSlot, $"\"{bundle.MermaidPath}\"");
        page = ReplaceOnce(page, StyleTag, $"<link rel=\"stylesheet\" crossorigin href=\"{bundle.StylePath}\">");
        return ReplaceOnce(
            page, ScriptTag, $"<script type=\"module\" crossorigin src=\"{bundle.ScriptPath}\"></script>");
    }

    // A built page that no longer holds an anchor exactly once has changed shape underneath us;
    // failing here is far better than emitting a report that is quietly missing its client.
    private static string ReplaceOnce(string page, string anchor, string value)
    {
        var at = page.IndexOf(anchor, StringComparison.Ordinal);
        if (at < 0 || page.IndexOf(anchor, at + anchor.Length, StringComparison.Ordinal) >= 0)
        {
            throw new InvalidOperationException(
                $"The built report should hold '{anchor}' exactly once.");
        }

        return string.Concat(page.AsSpan(0, at), value, page.AsSpan(at + anchor.Length));
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
        ReportProject? project,
        PlaybookReport? playbook)
    {
        return template.Replace(
            ReportSlot,
            DisplayGraphJson.SerializeReport(
                mode, path, source, stages, symbols, configuration, diagnostics,
                semanticTokens, configOverlay, project, playbook),
            StringComparison.Ordinal);
    }
}
