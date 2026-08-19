namespace DialogueDown.Visualization.Render;

/// <summary>
/// The mode a report is shown in, injected into its payload so the client can
/// label it and decide whether to open a live connection. These are the valid
/// values for the <c>mode</c> argument of <see cref="CompilationVisualizer"/>'s
/// live-rendering methods.
/// </summary>
public static class VisualizationMode
{
    /// <summary>A one-shot, self-contained report; no server (the <c>-o</c> export).</summary>
    public const string Static = "static";

    /// <summary>Server-backed, read-only; the report hot-reloads on on-disk changes.</summary>
    public const string View = "view";

    /// <summary>Server-backed with in-browser editing; saves edits back to the file.</summary>
    public const string Edit = "edit";
}
