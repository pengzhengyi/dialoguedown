namespace DialogueDown.Visualization.Display;

/// <summary>A language-owned jump target the editor presents outside the source document.</summary>
internal sealed record ReservedTargetSymbol(
    string Anchor,
    string Label,
    ReservedTargetRole Role);
