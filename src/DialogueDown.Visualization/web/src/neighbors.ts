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
 * Whether a stage is the flow graph rather than a tree.
 *
 * The stage declares this itself: `nests` is false when its `Child` edges are the spanning tree
 * the flow is *drawn* with rather than what contains what — the Dialogue Graph, whose nodes may be
 * led to from several places. So it is the one whose ways in the keyboard addresses with Shift,
 * and the one whose node inspector numbers them. The syntax and semantic trees nest, where a
 * node's ways out are its children and every node but the root arrives by exactly one edge: "the
 * nth way in" is not a question there.
 */
export function isFlowStage(stage: Stage): boolean {
    return stage.nests === false;
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
    return neighborsByNode(stage).get(nodeId) ?? { incoming: [], outgoing: [] };
}

/**
 * The same lists for every node, in one pass over the edges.
 *
 * The graph's own keyboard navigation resolves ways in and out on each keypress; reading the
 * whole stage once per drawing keeps that a lookup rather than a scan.
 */
export function neighborsByNode(stage: Stage): Map<string, Neighbors> {
    const byId = new Map(stage.nodes.map((node) => [node.id, node]));
    const byNode = new Map<string, Neighbors>(
        stage.nodes.map((node) => [node.id, { incoming: [], outgoing: [] }]),
    );

    for (const edge of stage.edges) {
        if (!isFlow(edge)) continue;
        const from = byId.get(edge.fromId);
        const to = byId.get(edge.toId);
        if (!from || !to) continue;
        byNode.get(from.id)!.outgoing.push(neighbor(edge, from.id, to));
        byNode.get(to.id)!.incoming.push(neighbor(edge, to.id, from));
    }
    return byNode;
}

function neighbor(edge: DisplayEdge, ownerId: string, other: DisplayNode): Neighbor {
    return {
        id: other.id,
        ownerId,
        label: labelOf(other),
        nodeCategory: other.category,
        edgeCategory: edge.category,
    };
}

function labelOf(node: DisplayNode): string {
    return node.label.trim() === "" ? (node.typeName ?? node.id) : node.label;
}
