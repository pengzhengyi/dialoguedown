namespace DialogueDown.Visualization.Display;

/// <summary>
/// A node's source location as a half-open character range <c>[Start, End)</c> into the
/// original document — the structured form of the display "span" attribute, so a client can
/// splice an edit back into the exact source it came from, or move the cursor there. A synthetic
/// node — one the compiler inserts with no source text of its own, such as a filled-in default
/// speaker — carries a zero-width span marking where it belongs (a caret position); the
/// document-root node carries the whole document.
/// </summary>
public readonly record struct DisplaySpan(int Start, int End);
