namespace DialogueDown.Script.Semantics;

/// <summary>
/// The reserved jump-target anchors DialogueDown owns. A reserved anchor is uppercase and matched
/// case-sensitively, so it can never collide with a heading's anchor, which is always lowercased.
/// It is the single source of truth for these names, shared by the resolver that recognizes them
/// and the editor projections that highlight and complete them.
/// </summary>
internal static class ReservedAnchors
{
    /// <summary>The terminator that ends a run early; resolves to the run's End sentinel.</summary>
    public const string End = "END";
}
