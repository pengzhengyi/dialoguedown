import type { DisplayNode } from "./model";

/**
 * The tightest node whose source span encloses a selection — the target of a reverse **Jump to**
 * from the Source editor into a compiler-stage tab.
 *
 * A node encloses the selection when its span covers the whole `[from, to)` range; among those, the
 * one with the smallest span wins, since it is the most specific. When the selection straddles two
 * siblings (no node covers all of it), the search falls back to the node enclosing the selection
 * start. Zero-width spans (a synthetic node's caret position) never enclose, so they are skipped.
 *
 * @returns the matching node, or `null` when no span-bearing node contains the offset.
 */
export function findEnclosingNode(
    nodes: readonly DisplayNode[],
    from: number,
    to: number,
): DisplayNode | null {
    const low = Math.min(from, to);
    const high = Math.max(from, to);
    return (
        tightestEnclosing(nodes, low, high) ??
        (low === high ? null : tightestEnclosing(nodes, low, low))
    );
}

function tightestEnclosing(
    nodes: readonly DisplayNode[],
    low: number,
    high: number,
): DisplayNode | null {
    let best: DisplayNode | null = null;
    let bestWidth = Number.POSITIVE_INFINITY;
    for (const node of nodes) {
        const span = node.span;
        if (span == null || span.end <= span.start) continue;
        if (span.start <= low && high <= span.end) {
            const width = span.end - span.start;
            if (width < bestWidth) {
                best = node;
                bestWidth = width;
            }
        }
    }
    return best;
}
