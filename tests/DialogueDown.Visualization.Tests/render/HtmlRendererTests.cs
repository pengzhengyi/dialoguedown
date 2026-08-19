using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Render;
using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class HtmlRendererTests
{
    private readonly HtmlRenderer _renderer = new();

    [Fact]
    public void Render_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _renderer.Render(null!));
    }

    [Fact]
    public void Render_ProducesSelfContainedOfflinePage()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var html = _renderer.Render(graph);

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        // The page is one self-contained file: the client libraries are inlined
        // (no CDN, works offline). Pico and Tippy leave recognizable CSS markers.
        Assert.DoesNotContain("cdn.jsdelivr.net", html);
        Assert.DoesNotContain("<script src=\"http", html);
        Assert.Contains("--pico-", html);
        Assert.Contains(".tippy-box", html);
        // The stage-data slot was filled, not left as the raw placeholder.
        Assert.DoesNotContain("\"__REPORT__\"", html);
    }

    [Fact]
    public void Render_EmbedsNodeSourceSnippet()
    {
        var graph = MakeGraph("Markdown AST", [new DisplayNode("n0", "Heading (H1)", [], "# Hello")], []);

        var html = _renderer.Render(graph);

        Assert.Contains("\"source\":\"# Hello\"", html);
    }

    [Fact]
    public void Render_EmbedsStageTitleAndNodeLabels()
    {
        var graph = MakeGraph(
            "Markdown AST",
            [Node("n0", "Document"), Node("n1", "Paragraph")],
            [Child("n0", "n1")]);

        var html = _renderer.Render(graph);

        Assert.Contains("\"title\":\"Markdown AST\"", html);
        Assert.Contains("\"label\":\"Paragraph\"", html);
    }

    [Fact]
    public void Render_EscapesScriptClosingTagFromData()
    {
        var graph = MakeGraph("G", [Node("n0", "</script>")], []);

        var html = _renderer.Render(graph);

        // The data-derived closing tag is unicode-escaped inside the inlined JSON.
        Assert.Contains("\\u003C/script", html);
    }
}
