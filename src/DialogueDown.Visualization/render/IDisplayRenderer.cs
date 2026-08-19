using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization.Render;

/// <summary>
/// Turns a <see cref="DisplayGraph"/> into one output format. Each format is a
/// separate strategy, so adding a format is one new class and every stage gets it
/// for free.
/// </summary>
public interface IDisplayRenderer
{
    /// <summary>Renders the graph to this renderer's format as a string.</summary>
    string Render(DisplayGraph graph);
}
