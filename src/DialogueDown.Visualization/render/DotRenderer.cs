using System.Text;
using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Render;

/// <summary>
/// Renders a display graph as <see href="https://graphviz.org/doc/info/lang.html">
/// Graphviz DOT</see> text, for users who want to lay it out with Graphviz. Child
/// edges are solid; reference edges are dashed. Labels and their attributes are
/// escaped for DOT's quoted-string rules.
/// </summary>
public sealed class DotRenderer : IDisplayRenderer
{
    public string Render(DisplayGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var builder = new StringBuilder();
        builder.Append("digraph \"").Append(Escape(graph.Title)).Append("\" {\n");

        foreach (var node in graph.Nodes)
        {
            builder.Append("    ").Append(node.Id)
                .Append(" [label=\"").Append(NodeText(node)).Append("\"];\n");
        }

        foreach (var edge in graph.Edges)
        {
            builder.Append("    ").Append(edge.FromId).Append(" -> ").Append(edge.ToId);
            if (edge.Kind == DisplayEdgeKind.Reference)
            {
                builder.Append(" [style=dashed]");
            }

            builder.Append(";\n");
        }

        builder.Append("}\n");
        return builder.ToString();
    }

    private static string NodeText(DisplayNode node)
    {
        var lines = new List<string> { node.Label };
        lines.AddRange(node.Attributes.Select(a => $"{a.Name}: {a.Value}"));
        return string.Join("\\n", lines.Select(Escape));
    }

    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n")
        .Replace("\r", "\\n");
}
