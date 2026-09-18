import { describe, it, expect } from "vitest";
import { isFlowStage, neighborsOf } from "./neighbors";
import type { DisplayNode, Stage } from "./model";

function node(id: string, label: string, category?: string): DisplayNode {
    return { id, label, category, attributes: [] };
}

const stage: Stage = {
    title: "Dialogue Graph",
    description: "The compiled flow.",
    nodes: [
        node("ask", "Guide: Which way?", "speech"),
        node("choice", "Choice", "structure"),
        node("left", "Alice: Left.", "speech"),
        node("right", "Alice: Right.", "speech"),
        node("inside", "Guide: You are inside.", "speech"),
    ],
    edges: [
        { fromId: "ask", toId: "choice", kind: "Child", category: "break" },
        { fromId: "choice", toId: "left", kind: "Child", category: "choice" },
        { fromId: "choice", toId: "right", kind: "Child", category: "choice" },
        { fromId: "left", toId: "inside", kind: "Child", category: "break" },
        { fromId: "right", toId: "inside", kind: "Reference", category: "break" },
    ],
    // The graph's child edges span the flow; a tree's nest. The stage says which it is.
    nests: false,
};

describe("neighborsOf", () => {
    it("names what leads to a node", () => {
        expect(neighborsOf(stage, "inside").incoming.map((n) => n.label)).toEqual([
            "Alice: Left.",
            "Alice: Right.",
        ]);
    });

    it("names what a node leads to, in the order the stage lists them", () => {
        expect(neighborsOf(stage, "choice").outgoing.map((n) => n.label)).toEqual([
            "Alice: Left.",
            "Alice: Right.",
        ]);
    });

    it("carries the route each neighbor is reached by, so a row can name it", () => {
        expect(neighborsOf(stage, "choice").outgoing.map((n) => n.edgeCategory)).toEqual([
            "choice",
            "choice",
        ]);
    });

    it("carries the neighbor's own category, so a row can wear its color", () => {
        expect(neighborsOf(stage, "ask").outgoing[0].nodeCategory).toBe("structure");
    });

    it("carries the id, so a row can take the reader to the node", () => {
        expect(neighborsOf(stage, "ask").outgoing[0].id).toBe("choice");
    });

    it("counts a cross-link like any other route — the flow does not care how it was drawn", () => {
        expect(neighborsOf(stage, "right").outgoing).toHaveLength(1);
    });

    it("reports an entry node as having nothing leading to it", () => {
        expect(neighborsOf(stage, "ask").incoming).toEqual([]);
    });

    it("skips an edge naming a node the stage does not draw", () => {
        const dangling: Stage = {
            ...stage,
            edges: [...stage.edges, { fromId: "ask", toId: "ghost", kind: "Child" }],
        };

        expect(neighborsOf(dangling, "ask").outgoing.map((n) => n.id)).toEqual(["choice"]);
    });

    it("falls back to a type name when a node has no label to show", () => {
        const unnamed: Stage = {
            ...stage,
            nodes: [...stage.nodes, { id: "end", label: "", typeName: "End", attributes: [] }],
            edges: [...stage.edges, { fromId: "inside", toId: "end", kind: "Child" }],
        };

        expect(neighborsOf(unnamed, "inside").outgoing[0].label).toBe("End");
    });

    it("leaves out a placement link — it is a visual clue, not a way control travels", () => {
        const placed: Stage = {
            ...stage,
            edges: [
                ...stage.edges,
                { fromId: "inside", toId: "orphan", category: "deferred", kind: "Reference" },
            ],
            nodes: [...stage.nodes, node("orphan", "Guide: Nobody reads this.")],
        };

        expect(neighborsOf(placed, "inside").outgoing.map((n) => n.id)).not.toContain("orphan");
    });
});

describe("isFlowStage", () => {
    it("recognizes the stage whose child edges are the flow rather than containment", () => {
        expect(isFlowStage(stage)).toBe(true);
    });

    it("does not mistake a nesting tree for the flow graph", () => {
        const tree: Stage = {
            ...stage,
            title: "Markdown AST",
            nests: true,
            edges: stage.edges.map(({ fromId, toId, kind }) => ({ fromId, toId, kind })),
        };

        expect(isFlowStage(tree)).toBe(false);
    });

    it("reads the stage's declaration, not the colors of its edges", () => {
        // A named route does not make a stage the flow graph: a tree may color an edge and stay a
        // tree. `nests` is the stage's own statement of what a `Child` edge means.
        const coloredTree: Stage = { ...stage, title: "Dialogue AST", nests: true };

        expect(isFlowStage(coloredTree)).toBe(false);
    });

    it("stays the flow graph even when no edge names its route", () => {
        const unnamed: Stage = {
            ...stage,
            edges: stage.edges.map(({ fromId, toId, kind }) => ({ fromId, toId, kind })),
        };

        expect(isFlowStage(unnamed)).toBe(true);
    });
});
