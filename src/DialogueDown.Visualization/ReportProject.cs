namespace DialogueDown.Visualization;

/// <summary>
/// The served-mode project context carried in the report payload so the client can render the
/// Explorer sidebar: the project <see cref="Root"/> to display, and the <see cref="ActivePath"/>
/// — the active script's root-relative path — to highlight and reveal in the tree, or
/// <c>null</c> when no document is active (the served shell's empty state). Absent entirely from a
/// static export (which has no server to browse), so its presence gates the sidebar.
/// </summary>
public sealed record ReportProject(string Root, string? ActivePath);
