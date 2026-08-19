namespace DialogueDown.Visualization.Render;

/// <summary>
/// The category → color map for emitted diagrams, mirroring the report's on-screen
/// palette so an exported Mermaid graph carries the same category signal (a code span
/// and the game call it becomes are both red). The interactive report keeps its own
/// copy in the web client; this is the .NET-side source for text renderers.
/// </summary>
internal static class CategoryPalette
{
    private const string DefaultColor = "#94a3b8";

    private static readonly IReadOnlyDictionary<string, string> _colors = new Dictionary<string, string>
    {
        ["document"] = "#64748b",
        ["structure"] = "#3b82f6",
        ["speech"] = "#22c55e",
        ["text"] = "#14b8a6",
        ["choice"] = "#a855f7",
        ["jump"] = "#06b6d4",
        ["media"] = "#f97316",
        ["call"] = "#ef4444",
        ["control"] = "#6366f1",
        ["styling"] = "#f59e0b",
        ["break"] = "#9ca3af",
        ["tag"] = "#ec4899",
    };

    /// <summary>The color for a category, or a neutral default for an unknown one.</summary>
    public static string ColorOf(string category) =>
        _colors.TryGetValue(category, out var color) ? color : DefaultColor;
}
