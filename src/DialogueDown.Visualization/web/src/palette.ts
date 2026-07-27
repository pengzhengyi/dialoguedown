/**
 * Semantic color palette. The projection tags each node with a stable,
 * cross-stage category; a later stage reuses the same name for a corresponding
 * concept, so the two share a color (a Markdown code span and the game call it
 * compiles to are both "call" = red). Only the color is shared across stages —
 * the human label in the legend is derived from each stage's own node types.
 */
export const CATEGORY_COLORS: Readonly<Record<string, string>> = {
    document: "#64748b",
    structure: "#3b82f6",
    speech: "#22c55e",
    text: "#14b8a6",
    choice: "#a855f7",
    jump: "#06b6d4",
    media: "#f97316",
    call: "#ef4444",
    styling: "#f59e0b",
    break: "#9ca3af",
    tag: "#ec4899",
    terminal: "#cf222e",
    deferred: "#d97706",
};

export const DEFAULT_COLOR = "#94a3b8";

/** The color for a category, falling back to a neutral color for unknowns. */
export function colorOf(category: string | undefined): string {
    return (category && CATEGORY_COLORS[category]) || DEFAULT_COLOR;
}
