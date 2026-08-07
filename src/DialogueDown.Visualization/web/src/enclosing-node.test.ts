import { describe, expect, it } from "vitest";
import { findEnclosingNode } from "./enclosing-node";
import type { DisplayEdge, DisplayNode } from "./model";

function node(id: string, start: number | null, end?: number, typeName?: string): DisplayNode {
    return {
        id,
        label: id,
        attributes: [],
        ...(start != null && end != null ? { span: { start, end } } : {}),
        ...(typeName != null ? { typeName } : {}),
    };
}

function child(fromId: string, toId: string): DisplayEdge {
    return { fromId, toId, kind: "Child" };
}

// A two-scene tree: each Scene's own span is just its heading; its lines are children.
//   doc
//     sceneA  [10,16]   lineA1 [18,30]   lineA2 [32,45]
//     sceneB  [50,56]   lineB1 [58,70]
function tree(): { nodes: DisplayNode[]; edges: DisplayEdge[] } {
    const nodes = [
        node("doc", null, undefined, "Document"),
        node("sceneA", 10, 16, "Scene"),
        node("lineA1", 18, 30, "Line"),
        node("lineA2", 32, 45, "Line"),
        node("sceneB", 50, 56, "Scene"),
        node("lineB1", 58, 70, "Line"),
    ];
    const edges = [
        child("doc", "sceneA"),
        child("doc", "sceneB"),
        child("sceneA", "lineA1"),
        child("sceneA", "lineA2"),
        child("sceneB", "lineB1"),
    ];
    return { nodes, edges };
}

describe("findEnclosingNode", () => {
    it("resolves a precise caret to the tightest leaf, with its span as the extent", () => {
        const { nodes, edges } = tree();
        const match = findEnclosingNode(nodes, edges, 20, 20);
        expect(match?.node.id).toBe("lineA1");
        expect(match?.extent).toEqual({ start: 18, end: 30 });
    });

    it("resolves a within-scene multi-line selection to the scene via subtree extent", () => {
        const { nodes, edges } = tree();
        // Spans lineA1 and lineA2: no single line encloses it, but the scene's subtree does.
        const match = findEnclosingNode(nodes, edges, 20, 40);
        expect(match?.node.id).toBe("sceneA");
        expect(match?.extent).toEqual({ start: 10, end: 45 });
    });

    it("caps a cross-scene selection at the scene containing its start", () => {
        const { nodes, edges } = tree();
        const match = findEnclosingNode(nodes, edges, 20, 60);
        expect(match?.node.id).toBe("sceneA");
        expect(match?.extent).toEqual({ start: 10, end: 45 });
    });

    it("resolves a caret in a scene heading to that scene", () => {
        const { nodes, edges } = tree();
        const match = findEnclosingNode(nodes, edges, 12, 12);
        expect(match?.node.id).toBe("sceneA");
    });

    it("normalizes a backwards selection", () => {
        const { nodes, edges } = tree();
        expect(findEnclosingNode(nodes, edges, 40, 20)?.node.id).toBe("sceneA");
    });

    it("falls back to the common ancestor when there are no scenes to cap at", () => {
        const nodes = [
            node("root", null, undefined, "Document"),
            node("a", 0, 20, "Block"),
            node("b", 22, 40, "Block"),
        ];
        const edges = [child("root", "a"), child("root", "b")];
        const match = findEnclosingNode(nodes, edges, 10, 30);
        expect(match?.node.id).toBe("root");
        expect(match?.extent).toEqual({ start: 0, end: 40 });
    });

    it("prefers the scene over an equal-extent document root (single-scene doc)", () => {
        const nodes = [
            node("doc", null, undefined, "Document"),
            node("scene", 0, 6, "Scene"),
            node("line", 8, 20, "Line"),
        ];
        const edges = [child("doc", "scene"), child("scene", "line")];
        // The scene's subtree extent [0,20] ties the document root's; a selection spanning the
        // heading into the body must land on the scene, never the root.
        expect(findEnclosingNode(nodes, edges, 2, 18)?.node.id).toBe("scene");
    });

    it("returns null when no span-bearing node covers the offset", () => {
        const nodes = [node("only", 20, 40, "Line")];
        expect(findEnclosingNode(nodes, [], 50, 55)).toBeNull();
        expect(findEnclosingNode([], [], 0, 0)).toBeNull();
    });
});
