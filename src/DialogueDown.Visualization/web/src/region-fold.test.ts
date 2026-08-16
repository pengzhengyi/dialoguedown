import { describe, it, expect } from "vitest";
import { foldRegions, regionNodeIdFor, regionOfBoxId } from "./region-fold";
import type { DisplayEdge, DisplayNode } from "./model";

function node(id: string, region?: string): DisplayNode {
    return { id, label: id, attributes: [], region };
}

function child(fromId: string, toId: string, category?: string): DisplayEdge {
    return { fromId, toId, kind: "Child", category };
}

function reference(fromId: string, toId: string, category?: string): DisplayEdge {
    return { fromId, toId, kind: "Reference", category };
}

/** The ids a folded graph draws, in order. */
function idsOf(nodes: readonly DisplayNode[]): string[] {
    return nodes.map((each) => each.id);
}

/** Every edge as `from->to`, so a test can name the shape it expects. */
function pairsOf(edges: readonly DisplayEdge[]): string[] {
    return edges.map((edge) => `${edge.fromId}->${edge.toId}`);
}

describe("foldRegions", () => {
    it("returns the graph untouched when nothing is collapsed", () => {
        const nodes = [node("root"), node("a", "S")];
        const edges = [child("root", "a")];

        const folded = foldRegions(nodes, edges, new Set());

        expect(folded.nodes).toBe(nodes);
        expect(folded.edges).toBe(edges);
    });

    it("replaces a region's nodes with one box, where its first node stood", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "S"), node("tail")];
        const edges = [child("root", "a"), child("a", "b"), child("b", "tail")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(idsOf(folded.nodes)).toEqual(["root", regionNodeIdFor("S"), "tail"]);
    });

    it("drops the edges wholly inside a folded region", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "S")];
        const edges = [child("root", "a"), child("a", "b")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(pairsOf(folded.edges)).toEqual([`root->${regionNodeIdFor("S")}`]);
    });

    it("re-points a crossing edge at the box, so the flow passes through it", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "S"), node("tail")];
        const edges = [child("root", "a"), child("a", "b"), child("b", "tail")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(pairsOf(folded.edges)).toEqual([
            `root->${regionNodeIdFor("S")}`,
            `${regionNodeIdFor("S")}->tail`,
        ]);
    });

    it("merges two routes that now join the same pair, since one line can carry one", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "S")];
        const edges = [child("root", "a"), reference("root", "b", "jump")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(pairsOf(folded.edges)).toEqual([`root->${regionNodeIdFor("S")}`]);
    });

    it("keeps the interior's size on the box, so a fold says how much it hides", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "S"), node("c", "S")];
        const edges = [child("root", "a"), child("a", "b"), child("b", "c")];

        const box = foldRegions(nodes, edges, new Set(["S"])).nodes[1];

        expect(box.label).toBe("S");
        expect(box.attributes).toEqual([{ name: "nodes", value: "3" }]);
        expect(box.region).toBe("S");
    });

    it("inverts a box's id back to the region it stands for", () => {
        expect(regionOfBoxId(regionNodeIdFor("The Crossroads"))).toBe("The Crossroads");
        expect(regionOfBoxId("n12")).toBeNull();
    });

    it("folds several regions at once, each into its own box", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "T")];
        const edges = [child("root", "a"), child("a", "b")];

        const folded = foldRegions(nodes, edges, new Set(["S", "T"]));

        expect(idsOf(folded.nodes)).toEqual(["root", regionNodeIdFor("S"), regionNodeIdFor("T")]);
        expect(pairsOf(folded.edges)).toEqual([
            `root->${regionNodeIdFor("S")}`,
            `${regionNodeIdFor("S")}->${regionNodeIdFor("T")}`,
        ]);
    });

    it("ignores a collapsed name no node claims", () => {
        const nodes = [node("root"), node("a", "S")];
        const edges = [child("root", "a")];

        const folded = foldRegions(nodes, edges, new Set(["Nowhere"]));

        expect(idsOf(folded.nodes)).toEqual(["root", "a"]);
        expect(pairsOf(folded.edges)).toEqual(["root->a"]);
    });

    it("makes the box the root when the folded region holds the root", () => {
        const nodes = [node("root", "S"), node("a", "S"), node("tail")];
        const edges = [child("root", "a"), child("a", "tail")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(idsOf(folded.nodes)).toEqual([regionNodeIdFor("S"), "tail"]);
        expect(folded.edges.filter((edge) => edge.toId === regionNodeIdFor("S"))).toEqual([]);
    });

    it("gathers a region whose nodes are scattered through the document into one box", () => {
        // Nothing guarantees a scene's nodes are adjacent — an unreachable line sits where it
        // falls. Both still fold into the one box, drawn where the first of them stood.
        const nodes = [node("root"), node("a", "S"), node("mid"), node("b", "S")];
        const edges = [child("root", "a"), child("a", "mid"), child("mid", "b")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(idsOf(folded.nodes)).toEqual(["root", regionNodeIdFor("S"), "mid"]);
        expect(folded.nodes[1].attributes).toEqual([{ name: "nodes", value: "2" }]);
    });

    it("leaves a region unfolded when a node already answers to its box id", () => {
        // The box id has to invert back to the region's name, so a colliding id is left alone
        // rather than renamed into something no inspector could read back.
        const nodes = [node("root"), node(regionNodeIdFor("S")), node("a", "S")];
        const edges = [child("root", regionNodeIdFor("S")), child(regionNodeIdFor("S"), "a")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(folded.nodes).toBe(nodes);
    });
});

describe("foldRegions keeps the drawing legal", () => {
    it("leaves each node at most one parent when two routes now enter one box", () => {
        // Two lines entered the scene at different nodes; folded, both arrive at the box. Only
        // one may be its parent, or the tree the drawing is laid out with cannot be built.
        const nodes = [node("root"), node("other"), node("a", "S"), node("b", "S")];
        const edges = [child("root", "a"), child("root", "other"), child("other", "b")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(parentsOf(folded.edges, regionNodeIdFor("S"))).toHaveLength(1);
        expect(folded.edges).toHaveLength(3);
    });

    it("breaks a cycle two folded scenes make between them", () => {
        // Neither document is cyclic over its nodes: S's first node leads into T, and T's last
        // leads back into S's second. Contracted, that is S → T → S.
        const nodes = [node("root"), node("s1", "S"), node("t1", "T"), node("s2", "S")];
        const edges = [child("root", "s1"), child("s1", "t1"), child("t1", "s2")];

        const folded = foldRegions(nodes, edges, new Set(["S", "T"]));

        expect(everyNodeHasAtMostOneParent(folded.edges)).toBe(true);
        expect(isAcyclicOverParents(folded.edges)).toBe(true);
    });

    it("demotes the edge it could not keep to a cross-link rather than dropping it", () => {
        const nodes = [node("root"), node("other"), node("a", "S"), node("b", "S")];
        const edges = [child("root", "a"), child("root", "other"), child("other", "b")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(folded.edges.filter((edge) => edge.kind === "Reference")).toHaveLength(1);
    });

    it("keeps a cross-link a cross-link when a tree edge already claimed the node", () => {
        const nodes = [node("root"), node("a", "S"), node("tail")];
        const edges = [child("root", "a"), child("a", "tail"), reference("tail", "a", "jump")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        const back = folded.edges.find(
            (edge) => edge.fromId === "tail" && edge.toId === regionNodeIdFor("S"),
        );
        expect(back?.kind).toBe("Reference");
    });

    it("carries a merged edge's meaning from the first route that made it", () => {
        const nodes = [node("root"), node("a", "S"), node("b", "S")];
        const edges = [child("root", "a", "succession"), reference("root", "b", "jump")];

        const folded = foldRegions(nodes, edges, new Set(["S"]));

        expect(folded.edges[0].category).toBe("succession");
    });
});

/** Every tree edge naming this node as its child. */
function parentsOf(edges: readonly DisplayEdge[], id: string): DisplayEdge[] {
    return edges.filter((edge) => edge.kind === "Child" && edge.toId === id);
}

function everyNodeHasAtMostOneParent(edges: readonly DisplayEdge[]): boolean {
    const seen = new Set<string>();
    for (const edge of edges) {
        if (edge.kind !== "Child") continue;
        if (seen.has(edge.toId)) return false;
        seen.add(edge.toId);
    }
    return true;
}

function isAcyclicOverParents(edges: readonly DisplayEdge[]): boolean {
    const parent = new Map(
        edges.filter((edge) => edge.kind === "Child").map((edge) => [edge.toId, edge.fromId]),
    );
    for (const start of parent.keys()) {
        const walked = new Set<string>([start]);
        let at = parent.get(start);
        while (at !== undefined) {
            if (walked.has(at)) return false;
            walked.add(at);
            at = parent.get(at);
        }
    }
    return true;
}
