/**
 * Folding a scene: the graph seen with one region contracted to a single box.
 *
 * A scene is the one grouping a reader may collapse without the drawing telling a lie about
 * itself. A node's children in this graph are an accident of which route reached them first, so
 * folding a *node* hides lines other routes still lead to; scene membership is decided by the
 * compiler from the document, and no traversal order can change it.
 *
 * The fold is a quotient, not a filter: the scene's nodes contract to one supernode, the edges
 * that crossed its border are re-pointed at that supernode, and the edges wholly inside vanish
 * with the interior they joined. Everything downstream stays exactly where it was — control
 * passes *through* the folded scene rather than disappearing into it.
 */

import type { DisplayEdge, DisplayNode } from "./model";

/** The graph as it is drawn with some regions folded. */
export interface FoldedGraph {
    readonly nodes: readonly DisplayNode[];
    readonly edges: readonly DisplayEdge[];
}

/**
 * The id the box standing for a folded region is drawn under.
 *
 * The compiler names its graph nodes `n<number>`, so the two namespaces are disjoint and the
 * mapping is exactly invertible by {@link regionOfBoxId}. A stage that named a node this anyway
 * simply does not fold that region — see {@link foldRegions}.
 */
export function regionNodeIdFor(region: string): string {
    return `${BOX_PREFIX}${region}`;
}

/** The region a box stands for, or `null` for an ordinary node's id. */
export function regionOfBoxId(id: string): string | null {
    return id.startsWith(BOX_PREFIX) ? id.slice(BOX_PREFIX.length) : null;
}

const BOX_PREFIX = "region:";

/**
 * The graph with every named region contracted to one box.
 *
 * With nothing collapsed the input is returned untouched, so an ordinary graph pays nothing for
 * a fold it is not using — and is drawn from exactly the nodes and edges the compiler emitted.
 */
export function foldRegions(
    nodes: readonly DisplayNode[],
    edges: readonly DisplayEdge[],
    collapsed: ReadonlySet<string>,
): FoldedGraph {
    if (collapsed.size === 0) return { nodes, edges };

    const interior = countByRegion(nodes, collapsed);
    if (interior.size === 0) return { nodes, edges };

    const boxIdOf = allocateBoxIds(nodes, collapsed, interior);
    if (boxIdOf.size === 0) return { nodes, edges };
    const boxOf = (node: DisplayNode): string | undefined =>
        node.region !== undefined ? boxIdOf.get(node.region) : undefined;

    const folded: DisplayNode[] = [];
    const idOf = new Map<string, string>();
    const drawn = new Set<string>();
    for (const node of nodes) {
        const box = boxOf(node);
        idOf.set(node.id, box ?? node.id);
        if (box === undefined) {
            folded.push(node);
            continue;
        }
        // The box stands where the region's first node stood, so folding a scene does not
        // reshuffle the order the rest of the document is read in.
        if (drawn.has(box)) continue;
        drawn.add(box);
        folded.push(boxNode(box, node.region!, interior.get(node.region!)!));
    }

    const rootId = idOf.get(rootOf(nodes, edges)) ?? folded[0]?.id;
    return { nodes: folded, edges: rebuildTree(contract(edges, idOf), rootId) };
}

function countByRegion(
    nodes: readonly DisplayNode[],
    collapsed: ReadonlySet<string>,
): Map<string, number> {
    const counts = new Map<string, number>();
    for (const node of nodes) {
        if (node.region === undefined || !collapsed.has(node.region)) continue;
        counts.set(node.region, (counts.get(node.region) ?? 0) + 1);
    }
    return counts;
}

/**
 * An id per folded region.
 *
 * A region whose box id a node already answers to is left unfolded rather than renamed: the box
 * id has to invert back to the region's name for an inspector to say what a line joins, and a
 * disambiguated id would not. The compiler's own ids cannot collide, so this is the door being
 * locked rather than a case that arises.
 */
function allocateBoxIds(
    nodes: readonly DisplayNode[],
    collapsed: ReadonlySet<string>,
    interior: ReadonlyMap<string, number>,
): Map<string, string> {
    const taken = new Set(
        nodes
            .filter((node) => node.region === undefined || !collapsed.has(node.region))
            .map((node) => node.id),
    );
    const ids = new Map<string, string>();
    for (const region of interior.keys()) {
        const id = regionNodeIdFor(region);
        if (!taken.has(id)) ids.set(region, id);
    }
    return ids;
}

/**
 * The box a folded region is drawn as: a scene-shaped node named for the scene it stands for and
 * carrying how much it is hiding — a fold that does not say what it took away is just a gap. Its
 * band drops its own name while it is folded, so the name is written once, not twice.
 */
function boxNode(id: string, region: string, count: number): DisplayNode {
    return {
        id,
        label: region,
        attributes: [{ name: "nodes", value: String(count) }],
        region,
        // A folded scene reads as a structural anchor, the way a scene node does elsewhere.
        typeName: "Scene",
    };
}

/**
 * Every edge with both ends mapped through the fold: an edge whose ends now coincide was inside
 * the region and goes with it, and edges that land on the same pair become one line.
 *
 * Merging loses a crossing's own color where two disagree — the drawing can carry one line
 * between two nodes, and the region's inspector still lists every crossing at both ends.
 */
function contract(edges: readonly DisplayEdge[], idOf: ReadonlyMap<string, string>): DisplayEdge[] {
    const merged = new Map<string, DisplayEdge>();
    for (const edge of edges) {
        const fromId = idOf.get(edge.fromId) ?? edge.fromId;
        const toId = idOf.get(edge.toId) ?? edge.toId;
        if (fromId === toId) continue;
        const key = `${fromId}->${toId}`;
        const kept = merged.get(key);
        if (kept === undefined) {
            merged.set(key, { ...edge, fromId, toId });
            continue;
        }
        // A pair joined by both a tree edge and a cross-link is a tree edge: the drawing is laid
        // out from the tree, and demoting the one that carried it would orphan what it reached.
        if (edge.kind === "Child" && kept.kind !== "Child")
            merged.set(key, { ...kept, ...edge, fromId, toId });
    }
    return [...merged.values()];
}

/**
 * Re-derives which edges the drawing is laid out from.
 *
 * The client draws a graph by naming one parent per node and treating everything else as a
 * cross-link, and the compiler guarantees that shape on the way in. Contracting a region breaks
 * it in two ways: two routes that entered a scene at different lines now enter the same box, and
 * two scenes that lead into each other become a cycle no node-level cycle existed for. A
 * breadth-first walk from the root claims the edge that first reaches each node, exactly as the
 * compiler's own spanning tree does — the tree edges the fold left legal are all kept, since the
 * walk follows them first.
 */
function rebuildTree(edges: readonly DisplayEdge[], rootId: string | undefined): DisplayEdge[] {
    if (rootId === undefined) return [...edges];
    const outgoing = new Map<string, DisplayEdge[]>();
    for (const edge of edges) {
        const from = outgoing.get(edge.fromId);
        if (from) from.push(edge);
        else outgoing.set(edge.fromId, [edge]);
    }

    const tree = new Set<DisplayEdge>();
    const reached = new Set<string>([rootId]);
    const walk = (follow: (edge: DisplayEdge) => boolean): void => {
        const queue = [...reached];
        while (queue.length > 0) {
            for (const edge of outgoing.get(queue.shift()!) ?? []) {
                if (!follow(edge) || reached.has(edge.toId)) continue;
                reached.add(edge.toId);
                tree.add(edge);
                queue.push(edge.toId);
            }
        }
    };
    // The compiler's own tree edges first, so a fold moves only the lines it had to.
    walk((edge) => edge.kind === "Child");
    // Then anything a malformed or disconnected graph left unreached, so the drawing still builds.
    walk(() => true);

    return edges.map((edge) => ({ ...edge, kind: tree.has(edge) ? "Child" : "Reference" }));
}

/** The node the graph hangs from: the one no tree edge leads to, else the first drawn. */
function rootOf(nodes: readonly DisplayNode[], edges: readonly DisplayEdge[]): string {
    const parented = new Set(
        edges.filter((edge) => edge.kind === "Child").map((edge) => edge.toId),
    );
    return (nodes.find((node) => !parented.has(node.id)) ?? nodes[0])?.id;
}
