using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization.Render;

/// <summary>
/// Renders a display graph as a self-contained, interactive HTML page built on
/// D3. The library and all assets are inlined, so the output works fully offline.
/// Child edges are drawn as a collapsible tree; reference edges (shared nodes or
/// cycles) appear as dashed overlays.
/// </summary>
public sealed class HtmlRenderer : IDisplayRenderer
{
    public string Render(DisplayGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return HtmlTemplate.RenderPage([graph]);
    }
}
