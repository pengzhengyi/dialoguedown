import { describe, it, expect } from "vitest";
import { regionDetailOf } from "./region-detail";
import type { DisplayNode, Stage } from "./model";

function node(id: string, label: string, region?: string, span?: [number, number]): DisplayNode {
    return {
        id,
        label,
        region,
        attributes: [],
        category: "speech",
        ...(span ? { span: { start: span[0], end: span[1] } } : {}),
    };
}

const stage: Stage = {
    title: "Dialogue Graph",
    description: "The compiled flow.",
    nodes: [
        node("open", "Guide: Which way?", "The Gate", [10, 30]),
        node("choose", "Choice", "The Gate", [31, 60]),
        node("inside", "Guide: Inside.", "The Hall", [70, 90]),
        node("loose", "Guide: Nowhere.", undefined, [95, 110]),
    ],
    edges: [
        { fromId: "open", toId: "choose", kind: "Child", category: "break" },
        { fromId: "choose", toId: "inside", kind: "Child", category: "break" },
        { fromId: "inside", toId: "open", kind: "Reference", category: "jump" },
        { fromId: "loose", toId: "inside", kind: "Child", category: "break" },
    ],
};

describe("regionDetailOf", () => {
    it("counts what the region holds", () => {
        expect(regionDetailOf(stage, "The Gate").nodeCount).toBe(2);
    });

    it("names the outside node each arriving route comes from", () => {
        expect(regionDetailOf(stage, "The Gate").entering.map((edge) => edge.from.label)).toEqual([
            "Guide: Inside.",
        ]);
    });

    it("names the outside node each departing route goes to", () => {
        expect(regionDetailOf(stage, "The Gate").leaving.map((edge) => edge.to.label)).toEqual([
            "Guide: Inside.",
        ]);
    });

    it("keeps the inside end too, so a row can name the edge it stands for", () => {
        const [arriving] = regionDetailOf(stage, "The Gate").entering;
        expect(arriving.from.id).toBe("inside");
        expect(arriving.to.id).toBe("open");
    });

    it("carries the route's own kind, so the border reads as more than a list of names", () => {
        expect(regionDetailOf(stage, "The Gate").entering[0].category).toBe("jump");
    });

    it("ignores a route that stays inside the region — a border is what crosses it", () => {
        // open -> choose runs from one Gate node to another, so it is not part of the border.
        const detail = regionDetailOf(stage, "The Gate");
        // A crossing always has exactly one end outside The Gate.
        const insideIds = new Set(["open", "choose"]);
        expect(
            [...detail.entering, ...detail.leaving].every(
                (edge) => insideIds.has(edge.from.id) !== insideIds.has(edge.to.id),
            ),
        ).toBe(true);
    });

    it("counts a route from unregioned ground as arriving from outside", () => {
        expect(regionDetailOf(stage, "The Hall").entering.map((edge) => edge.from.id)).toEqual([
            "choose",
            "loose",
        ]);
    });

    it("reaches from the first of its nodes to the last, so its text can be shown", () => {
        expect(regionDetailOf(stage, "The Gate").span).toEqual({ start: 10, end: 60 });
    });

    it("has no reach when none of its nodes knows where it came from", () => {
        const spanless: Stage = { ...stage, nodes: [node("a", "Alice: Hi.", "The Gate")] };

        expect(regionDetailOf(spanless, "The Gate").span).toBeUndefined();
    });

    it("says what kind of grouping it is, and the slug a divert names it by", () => {
        const declared: Stage = {
            ...stage,
            regions: [
                { name: "The Gate", kind: "Scene", anchor: "the-gate", span: { start: 0, end: 8 } },
            ],
        };

        const detail = regionDetailOf(declared, "The Gate");

        expect(detail.kind).toBe("Scene");
        expect(detail.anchor).toBe("the-gate");
    });

    it("points at where it is declared, which is not where its nodes live", () => {
        const declared: Stage = {
            ...stage,
            regions: [{ name: "The Gate", kind: "Scene", span: { start: 0, end: 8 } }],
        };

        const detail = regionDetailOf(declared, "The Gate");

        // The heading names the scene; the nodes are the body beneath it.
        expect(detail.declaredAt).toEqual({ start: 0, end: 8 });
        expect(detail.span).toEqual({ start: 10, end: 60 });
    });
});
