import { describe, it, expect } from "vitest";
import { hierarchy } from "d3";
import { lineageIds, createTreeView } from "./tree-view";
import type { DisplayNode, Stage } from "./model";

interface Node {
    id: string;
    kids?: Node[];
}

/** A small tree: root → (a → a1, a2), b. */
function sample() {
    return hierarchy<Node>(
        {
            id: "root",
            kids: [{ id: "a", kids: [{ id: "a1" }, { id: "a2" }] }, { id: "b" }],
        },
        (datum) => datum.kids,
    );
}

const child = (node: ReturnType<typeof sample>, id: string) =>
    node.children!.find((candidate) => candidate.data.id === id)!;

describe("lineageIds", () => {
    it("includes the node, its ancestors, and its descendants", () => {
        const a = child(sample(), "a");
        expect(lineageIds(a)).toEqual(new Set(["root", "a", "a1", "a2"]));
    });

    it("for the root, spans the whole tree", () => {
        expect(lineageIds(sample())).toEqual(new Set(["root", "a", "a1", "a2", "b"]));
    });

    it("for a leaf, is just its path to the root", () => {
        const a1 = child(child(sample(), "a"), "a1");
        expect(lineageIds(a1)).toEqual(new Set(["root", "a", "a1"]));
    });

    it("respects collapse — a collapsed node's hidden descendants are excluded", () => {
        const a = child(sample(), "a");
        // The tree view collapses by moving children to _children, leaving children undefined.
        (a as { children?: unknown }).children = undefined;
        expect(lineageIds(a)).toEqual(new Set(["root", "a"]));
    });
});

/** A minimal stage: root → a, b, where `a` carries a source span that a recompile can shift. */
function stageWith(spanA: { start: number; end: number }): Stage {
    return {
        title: "AST",
        description: "",
        nodes: [
            { id: "root", label: "root", attributes: [] },
            { id: "a", label: "a", attributes: [], span: spanA },
            { id: "b", label: "b", attributes: [] },
        ],
        edges: [
            { fromId: "root", toId: "a", kind: "Child" },
            { fromId: "root", toId: "b", kind: "Child" },
        ],
    };
}

describe("createTreeView — selection by stable id", () => {
    it("selectById selects the resolved node and reports failure for an unknown id", () => {
        const selected: DisplayNode[] = [];
        const view = createTreeView(stageWith({ start: 0, end: 3 }), (n) => selected.push(n));

        expect(view.selectById("a")).toBe(true);
        expect(selected.at(-1)!.id).toBe("a");
        expect(view.selectById("ghost")).toBe(false); // cancels safely; no selection change
        expect(selected.at(-1)!.id).toBe("a");
    });

    it("resolves the id against the freshly installed view, not the stale spans", () => {
        // The click captured id "a" from the pre-save view (span 0..3). A save recompiled and
        // rebuilt the view with the node's span shifted (5..8). Resolving by id against the new
        // view selects the node with the CURRENT span, never the stale captured one.
        const staleView = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        void staleView;

        const freshSelected: DisplayNode[] = [];
        const freshView = createTreeView(stageWith({ start: 5, end: 8 }), (n) =>
            freshSelected.push(n),
        );

        expect(freshView.selectById("a")).toBe(true);
        expect(freshSelected.at(-1)!.span).toEqual({ start: 5, end: 8 });
    });

    it("selectById with toggle collapses the node's fold, like a circle click", () => {
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        expect(view.svg.querySelectorAll("g.node").length).toBe(3); // root, a, b

        view.selectById("root", { toggle: true }); // fold the root, hiding its subtree

        expect(view.svg.querySelectorAll("g.node").length).toBe(1); // only the collapsed root remains
    });
});
