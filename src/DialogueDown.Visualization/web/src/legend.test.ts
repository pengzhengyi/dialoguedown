import { describe, it, expect, vi, beforeEach } from "vitest";
import {
    categoryStats,
    createLegend,
    edgeCategoryCounts,
    setRegionFoldState,
    type LegendHandlers,
} from "./legend";
import { CATEGORY_COLORS } from "./palette";
import type { DisplayNode, Stage } from "./model";
import { ARROWHEAD_PATH, CROSS_PATH, edgeStyle } from "./edge-style";
import { edgeSwatch, periodsShown } from "./edge-swatch";

function node(id: string, label: string, category?: string): DisplayNode {
    return { id, label, category, attributes: [] };
}

function titledNode(id: string, label: string, typeName: string, category: string): DisplayNode {
    return { id, label, typeName, category, attributes: [] };
}

describe("categoryStats", () => {
    it("counts nodes per category and collects distinct type names", () => {
        const nodes = [
            node("n1", "Heading (H1)", "structure"),
            node("n2", "Heading (H2)", "structure"),
            node("n3", "Code span", "call"),
        ];
        expect(categoryStats(nodes)).toEqual({
            structure: { names: ["Heading"], count: 2 },
            call: { names: ["Code span"], count: 1 },
        });
    });

    it("groups by a node's explicit type name over its (content) label", () => {
        // Scene-tree nodes label themselves by title, so the legend must read the type name
        // instead — otherwise three distinct titles would render as three legend rows.
        const nodes = [
            titledNode("n1", "The Crossroads", "Scene", "structure"),
            titledNode("n2", "The Market", "Scene", "structure"),
        ];
        expect(categoryStats(nodes).structure).toEqual({ names: ["Scene"], count: 2 });
    });

    it("keeps multiple distinct type names within one category", () => {
        const nodes = [node("n1", "Heading (H1)", "structure"), node("n2", "Section", "structure")];
        expect(categoryStats(nodes).structure).toEqual({ names: ["Heading", "Section"], count: 2 });
    });

    it("ignores nodes without a category", () => {
        expect(categoryStats([node("n1", "Text")])).toEqual({});
    });
});

describe("createLegend", () => {
    let handlers: LegendHandlers;

    const stage: Stage = {
        title: "Markdown AST",
        description: "Markdown AST stage.",
        edges: [],
        nodes: [
            node("n1", "Heading (H1)", "structure"),
            node("n2", "Heading (H2)", "structure"),
            node("n3", "Code span", "call"),
            node("n4", "Orphan"),
        ],
    };

    beforeEach(() => {
        handlers = { onToggle: vi.fn(), onHover: vi.fn(), onLeave: vi.fn() };
    });

    it("renders one row per present category, in palette order", () => {
        const legend = createLegend(stage, handlers);
        const labels = [...legend.querySelectorAll(".legend-label")].map((el) => el.textContent);
        // "structure" precedes "call" in CATEGORY_COLORS, and the uncategorised node is skipped.
        expect(Object.keys(CATEGORY_COLORS).indexOf("structure")).toBeLessThan(
            Object.keys(CATEGORY_COLORS).indexOf("call"),
        );
        expect(labels).toEqual(["Heading", "Code span"]);
    });

    it("shows a per-category node count and starts pressed (visible)", () => {
        const legend = createLegend(stage, handlers);
        const rows = legend.querySelectorAll("button.legend-item");
        expect(rows[0].querySelector(".count")?.textContent).toBe("2");
        expect(rows[0].getAttribute("aria-pressed")).toBe("true");
    });

    it("toggles a category off then on, reporting the dim state", () => {
        const legend = createLegend(stage, handlers);
        const row = legend.querySelector<HTMLButtonElement>("button.legend-item")!;

        row.click();
        expect(row.classList.contains("muted")).toBe(true);
        expect(row.getAttribute("aria-pressed")).toBe("false");
        expect(handlers.onToggle).toHaveBeenLastCalledWith("structure", true);

        row.click();
        expect(row.classList.contains("muted")).toBe(false);
        expect(row.getAttribute("aria-pressed")).toBe("true");
        expect(handlers.onToggle).toHaveBeenLastCalledWith("structure", false);
    });

    it("highlights on hover/focus and clears on leave/blur", () => {
        const legend = createLegend(stage, handlers);
        const row = legend.querySelector<HTMLButtonElement>("button.legend-item")!;

        row.dispatchEvent(new MouseEvent("mouseenter"));
        expect(handlers.onHover).toHaveBeenLastCalledWith("structure");
        row.dispatchEvent(new FocusEvent("focus"));
        expect(handlers.onHover).toHaveBeenCalledTimes(2);

        row.dispatchEvent(new MouseEvent("mouseleave"));
        row.dispatchEvent(new FocusEvent("blur"));
        expect(handlers.onLeave).toHaveBeenCalledTimes(2);
    });
});

describe("edgeCategoryCounts", () => {
    it("counts edges per category and ignores uncategorized ones", () => {
        const counts = edgeCategoryCounts([
            { fromId: "n0", toId: "n1", kind: "Child", category: "break" },
            { fromId: "n1", toId: "n2", kind: "Child", category: "break" },
            { fromId: "n2", toId: "n0", kind: "Reference", category: "jump" },
            { fromId: "n3", toId: "n4", kind: "Child" },
        ]);

        expect(counts).toEqual({ break: 2, jump: 1 });
    });
});

describe("createLegend edge group", () => {
    const handlers: LegendHandlers = { onToggle: vi.fn(), onHover: vi.fn(), onLeave: vi.fn() };

    const flowStage: Stage = {
        title: "Dialogue Graph",
        description: "The compiled flow.",
        nodes: [node("n0", "Alice: Hi.", "speech"), node("n1", "End", "terminal")],
        edges: [
            { fromId: "n0", toId: "n1", kind: "Child", category: "break" },
            { fromId: "n1", toId: "n0", kind: "Reference", category: "jump" },
        ],
    };

    it("names the two groups when the stage's edges carry meaning", () => {
        const legend = createLegend(flowStage, handlers);

        expect([...legend.querySelectorAll(".legend-heading")].map((el) => el.textContent)).toEqual(
            ["Nodes", "Edges"],
        );
    });

    it("labels an edge row by the route it is, not by the node kind it borrows color from", () => {
        const legend = createLegend(flowStage, handlers);

        const labels = [...legend.querySelectorAll(".legend-edge .legend-label")].map(
            (el) => el.textContent,
        );
        expect(labels).toEqual(["Jump", "Succession"]);
    });

    it("colors an edge row from the shared palette, so a route matches its node", () => {
        const legend = createLegend(flowStage, handlers);

        // The first edge row is the jump, following palette order.
        const line = legend.querySelector(".legend-edge .edge-swatch .swatch-line");
        expect(line?.getAttribute("stroke")).toBe(CATEGORY_COLORS.jump);
    });

    it("draws an edge row with the route's own dash pattern, not an approximation of it", () => {
        const legend = createLegend(flowStage, handlers);

        const line = legend.querySelector(".legend-edge .edge-swatch .swatch-line");
        expect(line?.getAttribute("stroke-dasharray")).toBe(edgeStyle("jump")?.dash);
    });

    it("points an edge row with an arrowhead, because a route has a direction", () => {
        const legend = createLegend(flowStage, handlers);

        const swatch = legend.querySelector(".legend-edge .edge-swatch")!;
        const marker = swatch.querySelector("marker")!.id;
        expect(swatch.querySelector(".swatch-line")!.getAttribute("marker-end")).toBe(
            `url(#${marker})`,
        );
        // Every stage's legend shares the one document, so the id has to name its stage or one
        // stage would point with another's color.
        expect(marker).toContain("dialogue-graph");
    });

    it("omits the groups entirely when no edge carries a meaning", () => {
        const plain: Stage = { ...flowStage, edges: [{ fromId: "n0", toId: "n1", kind: "Child" }] };

        const legend = createLegend(plain, handlers);

        expect(legend.querySelectorAll(".legend-heading")).toHaveLength(0);
        expect(legend.querySelectorAll(".legend-edge")).toHaveLength(0);
    });

    it("answers the pointer as a node row does, so alike rows behave alike", () => {
        const onHover = vi.fn();
        const onLeave = vi.fn();
        const legend = createLegend(flowStage, { onToggle: vi.fn(), onHover, onLeave });

        const row = legend.querySelector<HTMLElement>(".legend-edge")!;
        row.dispatchEvent(new MouseEvent("mouseenter"));
        expect(onHover).toHaveBeenCalledWith("jump");

        row.dispatchEvent(new MouseEvent("mouseleave"));
        expect(onLeave).toHaveBeenCalled();
    });

    it("switches its own route off and on again when clicked", () => {
        const onToggle = vi.fn();
        const legend = createLegend(flowStage, { onToggle, onHover: vi.fn(), onLeave: vi.fn() });

        const row = legend.querySelector<HTMLButtonElement>(".legend-edge")!;
        row.click();
        expect(onToggle).toHaveBeenLastCalledWith("jump", true);
        expect(row.getAttribute("aria-pressed")).toBe("false");

        row.click();
        expect(onToggle).toHaveBeenLastCalledWith("jump", false);
        expect(row.getAttribute("aria-pressed")).toBe("true");
    });

    it("is a button, so a keyboard reaches it too", () => {
        const legend = createLegend(flowStage, handlers);

        expect(legend.querySelector(".legend-edge")?.tagName).toBe("BUTTON");
    });
});

describe("edgeSwatch", () => {
    it("draws the route's own dash pattern, so the legend cannot drift from the drawing", () => {
        const swatch = edgeSwatch("jump", "graph");

        expect(swatch.querySelector(".swatch-line")?.getAttribute("stroke-dasharray")).toBe(
            edgeStyle("jump")?.dash,
        );
    });

    it("draws a solid line for a route with no pattern, rather than an invented one", () => {
        const swatch = edgeSwatch("break", "graph");

        expect(swatch.querySelector(".swatch-line")?.hasAttribute("stroke-dasharray")).toBe(false);
    });

    it("colors the line from the shared palette, so a route matches its node", () => {
        const swatch = edgeSwatch("jump", "graph");

        expect(swatch.querySelector(".swatch-line")?.getAttribute("stroke")).toBe(
            CATEGORY_COLORS.jump,
        );
    });

    it("points a route with an arrowhead, because a route leads somewhere", () => {
        const swatch = edgeSwatch("jump", "graph");

        const marker = swatch.querySelector("marker")!;
        expect(swatch.querySelector(".swatch-line")!.getAttribute("marker-end")).toBe(
            `url(#${marker.id})`,
        );
        expect(marker.querySelector("path")?.getAttribute("fill")).toBe(CATEGORY_COLORS.jump);
    });

    it("namespaces the arrowhead per stage, since every legend shares one document", () => {
        expect(edgeSwatch("jump", "desugared").querySelector("marker")!.id).not.toBe(
            edgeSwatch("jump", "dialogue-graph").querySelector("marker")!.id,
        );
    });

    it("stamps a withheld route with crosses and never points it, because it leads nowhere", () => {
        const swatch = edgeSwatch("deferred", "graph");

        const line = swatch.querySelector(".swatch-line")!;
        expect(line.getAttribute("marker-end")).toBeNull();
        expect(line.getAttribute("marker-mid")).toContain("tick-");
        // A glyph is stamped at a vertex, so a stamped line has to be drawn as several segments.
        expect(line.getAttribute("d")!.split("L").length).toBeGreaterThanOrEqual(3);
    });

    it("carries the canvas's own marker shapes, so the legend cannot draw a different arrow", () => {
        expect(edgeSwatch("jump", "graph").querySelector("marker path")?.getAttribute("d")).toBe(
            ARROWHEAD_PATH,
        );
        expect(
            edgeSwatch("deferred", "graph").querySelector("marker path")?.getAttribute("d"),
        ).toBe(CROSS_PATH);
    });
});

describe("periodsShown", () => {
    // A pattern the reader cannot see repeat is not a pattern; it is one bar of unknown length.
    // This is what made a jump and a conditional indistinguishable at the old swatch width.
    it.each(["jump", "choice", "control"])("repeats %s's pattern at least twice", (category) => {
        expect(periodsShown(edgeStyle(category))).toBeGreaterThanOrEqual(2);
    });

    it("treats a solid line as its own pattern at any length", () => {
        expect(periodsShown(edgeStyle("break"))).toBe(Infinity);
    });
});

describe("region rows", () => {
    const scenes: Stage = {
        title: "Dialogue Graph",
        description: "The compiled flow.",
        edges: [],
        regions: [{ name: "The Market", kind: "Scene" }],
        nodes: [
            { ...node("n1", "Line", "speech"), region: "The Market" },
            { ...node("n2", "Line", "speech"), region: "The Market" },
            { ...node("n3", "Line", "speech"), region: "The Forest" },
        ],
    };
    // A stage that draws regions always offers the two commands, and the state line comes with
    // them, so the fixture supplies them too.
    const handlers: LegendHandlers = {
        onToggle: vi.fn(),
        onHover: vi.fn(),
        onLeave: vi.fn(),
        regionFold: { onExpandAll: vi.fn(), onCollapseAll: vi.fn() },
    };

    it("says what a region's number counts, which its heading cannot", () => {
        const legend = createLegend(scenes, handlers);

        const counts = [...legend.querySelectorAll(".legend-region .count")].map(
            (el) => el.textContent,
        );
        expect(counts).toEqual(["2 nodes", "1 node"]);
    });

    it("names the kind alone, because the heading above already says Regions", () => {
        const legend = createLegend(scenes, handlers);

        expect(legend.querySelector(".legend-kind-toggle")?.textContent).toBe("Scene");
    });

    it("empties a folded scene's mark and states the mixed view", () => {
        const legend = createLegend(scenes, handlers);

        setRegionFoldState(legend, new Set(["The Market"]), 2);

        const rows = [...legend.querySelectorAll<HTMLElement>(".legend-region")];
        expect(rows.map((row) => row.dataset.folded)).toEqual(["true", "false"]);
        expect(rows[0].title).toBe("The Market — folded");
        expect(legend.querySelector(".legend-kind-state")?.textContent).toBe("1 of 2 folded");
    });

    it("fills every mark again once nothing is folded", () => {
        const legend = createLegend(scenes, handlers);

        setRegionFoldState(legend, new Set(["The Market"]), 2);
        setRegionFoldState(legend, new Set(), 2);

        const rows = [...legend.querySelectorAll<HTMLElement>(".legend-region")];
        expect(rows.every((row) => row.dataset.folded === "false")).toBe(true);
        expect(legend.querySelector(".legend-kind-state")?.textContent).toBe("all open");
    });
});
