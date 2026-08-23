import type { DisplayEdge, DisplayNode, Span } from "./model";

/** The node a reverse **Jump to** should reveal, and the source range it covers. */
export interface EnclosingMatch {
    readonly node: DisplayNode;
    /** The node's subtree extent — its own span unioned with its descendants'. */
    readonly extent: Span;
}

/** The stage a reverse **Jump to** searches: its drawing, and what its `Child` edges mean. */
export interface EnclosingScope {
    readonly nodes: readonly DisplayNode[];
    readonly edges: readonly DisplayEdge[];
    /**
     * Whether a `Child` edge nests the child's source inside the parent's. Absent means it does,
     * which is the syntax-tree case.
     */
    readonly nests?: boolean;
}

/**
 * The node whose source range best encloses a selection — the target of a reverse **Jump to** from
 * the Source editor into a compiler-stage tab.
 *
 * A node's range is its **subtree extent** (its own span unioned with its descendants'), not its own
 * span alone, because a container's span is often just a header: a `Scene` node covers only its
 * heading line, while its lines and choices are children. Using the extent, the tightest node
 * enclosing the whole selection is the right target: a precise selection lands on a leaf, a
 * scene-wide selection on the scene.
 *
 * That holds only where a `Child` edge nests the child's source inside the parent's. The Dialogue
 * Graph's do not — they mark a node's parent in the spanning tree the drawing is laid out with, so
 * unioning what they reach would stretch a node's extent along the rest of the flow and, because a
 * jump is such an edge, out of its scene entirely. A stage that says it does not nest is ranked by
 * its nodes' own spans, which there already cover everything a node contains.
 *
 * The result is never coarser than a single scene: a selection that crosses scene boundaries (whose
 * only common ancestor is the document) resolves instead to the scene containing its start. Stages
 * without scenes fall back to the common ancestor.
 *
 * @returns the matching node and its extent, or `null` when no span-bearing node contains the offset.
 */
export function findEnclosingNode(
    scope: EnclosingScope,
    from: number,
    to: number,
): EnclosingMatch | null {
    const nodes = scope.nodes;
    const low = Math.min(from, to);
    const high = Math.max(from, to);
    const extents = scope.nests === false ? ownSpans(nodes) : subtreeExtents(nodes, scope.edges);
    const width = (node: DisplayNode): number => {
        const extent = extents.get(node.id)!;
        return extent.end - extent.start;
    };

    // Cap at scene granularity. When a scene encloses the whole selection, use the tightest node
    // only if it is strictly tighter than that scene (a descendant); otherwise use the scene — this
    // beats a document root that ties the scene's extent. When no scene encloses the selection (it
    // crosses scene boundaries), fall back to the scene containing the start; failing that (a stage
    // with no scenes), the tightest common ancestor.
    const scene = tightest(nodes, extents, low, high, isScene);
    let chosen: DisplayNode | null;
    if (scene != null) {
        const enclosing = tightest(nodes, extents, low, high);
        chosen = enclosing != null && width(enclosing) < width(scene) ? enclosing : scene;
    } else {
        chosen =
            tightest(nodes, extents, low, low, isScene) ??
            tightest(nodes, extents, low, high) ??
            tightest(nodes, extents, low, low);
    }
    if (chosen == null) return null;
    return { node: chosen, extent: extents.get(chosen.id)! };
}

function isScene(node: DisplayNode): boolean {
    return node.typeName === "Scene";
}

/** The tightest node whose subtree extent encloses `[low, high]`, optionally filtered by `accept`. */
function tightest(
    nodes: readonly DisplayNode[],
    extents: ReadonlyMap<string, Span>,
    low: number,
    high: number,
    accept?: (node: DisplayNode) => boolean,
): DisplayNode | null {
    let best: DisplayNode | null = null;
    let bestWidth = Number.POSITIVE_INFINITY;
    for (const node of nodes) {
        if (accept && !accept(node)) continue;
        const extent = extents.get(node.id);
        if (extent == null || !(extent.start <= low && high <= extent.end)) continue;
        const width = extent.end - extent.start;
        if (width < bestWidth) {
            best = node;
            bestWidth = width;
        }
    }
    return best;
}

/** Each node's own span, for a stage whose `Child` edges lay the drawing out rather than nest. */
function ownSpans(nodes: readonly DisplayNode[]): ReadonlyMap<string, Span> {
    const extents = new Map<string, Span>();
    for (const node of nodes) {
        const span = node.span;
        if (span && span.end > span.start) extents.set(node.id, span);
    }
    return extents;
}

/** Each node's subtree extent — its own span unioned with all of its `Child` descendants'. */
function subtreeExtents(
    nodes: readonly DisplayNode[],
    edges: readonly DisplayEdge[],
): ReadonlyMap<string, Span> {
    const children = new Map<string, string[]>();
    for (const edge of edges) {
        if (edge.kind !== "Child") continue;
        (children.get(edge.fromId) ?? setDefault(children, edge.fromId)).push(edge.toId);
    }
    const byId = new Map(nodes.map((node) => [node.id, node]));
    const extents = new Map<string, Span>();
    const visiting = new Set<string>();

    function extentOf(id: string): Span | null {
        const cached = extents.get(id);
        if (cached) return cached;
        if (visiting.has(id)) return null; // guard against a malformed cycle
        visiting.add(id);

        const own = byId.get(id)?.span;
        let start = own && own.end > own.start ? own.start : Number.POSITIVE_INFINITY;
        let end = own && own.end > own.start ? own.end : Number.NEGATIVE_INFINITY;
        for (const childId of children.get(id) ?? []) {
            const childExtent = extentOf(childId);
            if (childExtent) {
                start = Math.min(start, childExtent.start);
                end = Math.max(end, childExtent.end);
            }
        }
        visiting.delete(id);
        if (start > end) return null;
        const extent = { start, end };
        extents.set(id, extent);
        return extent;
    }

    for (const node of nodes) extentOf(node.id);
    return extents;
}

function setDefault(map: Map<string, string[]>, key: string): string[] {
    const list: string[] = [];
    map.set(key, list);
    return list;
}
