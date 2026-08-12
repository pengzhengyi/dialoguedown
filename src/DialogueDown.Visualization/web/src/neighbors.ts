import type { DisplayEdge, DisplayNode, Stage } from "./model";
import { edgeStyle } from "./edge-style";

/**
 * Whether an edge is one control actually travels.
 *
 * Not every line on the drawing is flow: a placement link only says where an unreachable node
 * sits in the document. It earns a line so the node is not adrift, but it is not something a
 * reader can follow, so it has no place in a list of where control comes from or goes.
 */
export function isFlow(edge: DisplayEdge): boolean {
    return edgeStyle(edge.category)?.isRoute !== false;
}

/**
 * One end of an edge, seen from the node on the other end of it.
 *
 * Named for what a reader is looking for — *which* node, reached *how* — rather than for the
 * edge record it came from.
 */
export interface Neighbor {
    /** The node at the other end, so a row can take the reader there. */
    id: string;
    /** The node this list belongs to — the other end of the same edge. */
    ownerId: string;
    label: string;
    /** The other node's own category, for its color dot. */
    nodeCategory?: string;
    /** The route's category, for the line it is named and drawn by. */
    edgeCategory?: string;
}

export interface Neighbors {
    /** The nodes that lead here, in the order the stage lists their edges. */
    incoming: Neighbor[];
    /** The nodes this one leads to, in that same order — for a choice, its arms in order. */
    outgoing: Neighbor[];
}

/**
 * Which nodes lead to a node and which it leads to.
 *
 * A graph tab draws the flow but cannot label every line at once; this is the same information
 * read as text, for the one node the reader has asked about. Edges naming a node the stage does
 * not draw are skipped rather than shown as a row that goes nowhere.
 */
export function neighborsOf(stage: Stage, nodeId: string): Neighbors {
    const byId = new Map(stage.nodes.map((node) => [node.id, node]));
    const incoming: Neighbor[] = [];
    const outgoing: Neighbor[] = [];

    for (const edge of stage.edges) {
        if (!isFlow(edge)) continue;
        if (edge.toId === nodeId) push(incoming, edge, edge.fromId);
        if (edge.fromId === nodeId) push(outgoing, edge, edge.toId);
    }
    return { incoming, outgoing };

    function push(into: Neighbor[], edge: DisplayEdge, otherId: string): void {
        const other = byId.get(otherId);
        if (!other) return;
        into.push({
            id: other.id,
            ownerId: nodeId,
            label: labelOf(other),
            nodeCategory: other.category,
            edgeCategory: edge.category,
        });
    }
}

function labelOf(node: DisplayNode): string {
    return node.label.trim() === "" ? (node.typeName ?? node.id) : node.label;
}
