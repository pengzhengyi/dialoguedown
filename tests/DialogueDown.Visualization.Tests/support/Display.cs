using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// One place to build display-model objects for renderer tests, so a constructor
/// change touches this file instead of every test.
/// </summary>
internal static class Display
{
    public static DisplayAttribute Attr(string name, string value) => new(name, value);

    public static DisplayNode Node(string id, string label, params DisplayAttribute[] attributes) =>
        new(id, label, attributes);

    public static DisplayNode NodeWithCategory(
        string id, string label, string category, params DisplayAttribute[] attributes) =>
        new(id, label, attributes, Category: category);

    public static DisplayEdge Child(string fromId, string toId) =>
        new(fromId, toId, DisplayEdgeKind.Child);

    public static DisplayEdge Reference(string fromId, string toId) =>
        new(fromId, toId, DisplayEdgeKind.Reference);

    public static DisplayGraph MakeGraph(
        string title, DisplayNode[] nodes, DisplayEdge[] edges, string description = "") =>
        new(title, description, nodes, edges);
}
