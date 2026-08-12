import { describe, it, expect } from "vitest";
import {
    edgePath,
    labelBlockWidth,
    labelClearance,
    laneRoute,
    routeCurve,
    LABEL_BLOCK_ORIGIN,
    type Point,
} from "./edge-path";

const from: Point = { x: 0, y: 0 };

describe("labelBlockWidth", () => {
    it("grows with the text it must cover", () => {
        expect(labelBlockWidth(20)).toBeLessThan(labelBlockWidth(200));
    });

    it("still reserves room for a node with no text at all", () => {
        expect(labelBlockWidth(0)).toBeGreaterThan(0);
    });
});

describe("labelClearance", () => {
    it("reaches past the node's own text, so an outgoing line cannot strike through it", () => {
        expect(labelClearance(200)).toBeGreaterThan(LABEL_BLOCK_ORIGIN + 200);
    });
});

describe("routeCurve", () => {
    it("starts past the source label rather than at the dot", () => {
        expect(routeCurve(from, { x: 260, y: 0 }, 60).start).toEqual({ x: 60, y: 0 });
    });

    it("ends exactly on the target dot, so the arrowhead lands on the node", () => {
        const target = { x: 260, y: 62 };
        expect(routeCurve(from, target, 60).end).toEqual(target);
    });

    it("stops short of the target, however wide the source label", () => {
        const target = { x: 40, y: 0 };
        expect(routeCurve(from, target, 500).start.x).toBeLessThan(target.x);
    });

    it("keeps a stretch of curve even when the label nearly fills the run", () => {
        expect(routeCurve(from, { x: 260, y: 62 }, 500).start.x).toBe(248);
    });

    it("never leads out backwards, however cramped the run", () => {
        expect(routeCurve(from, { x: 5, y: 0 }, 500).start.x).toBe(0);
    });

    it("runs level between two nodes on the same row", () => {
        const curve = routeCurve(from, { x: 260, y: 0 }, 60);
        expect(curve.control1.y).toBe(0);
        expect(curve.control2.y).toBe(0);
    });

    it("turns through the midpoint, so the step reads as one smooth move", () => {
        const curve = routeCurve(from, { x: 260, y: 62 }, 60);
        expect(curve.control1.x).toBe(160);
        expect(curve.control2.x).toBe(160);
    });
});

describe("laneRoute", () => {
    it("drops and rises at the lane it was given", () => {
        const route = laneRoute(from, { x: 900, y: 62 }, 400);
        expect(route.drop.y).toBe(400);
        expect(route.rise.y).toBe(400);
    });

    it("still begins and ends on its own two nodes", () => {
        const target = { x: 900, y: 62 };
        const route = laneRoute(from, target, 400);
        expect(route.start.y).toBe(0);
        expect(route.end).toEqual(target);
    });

    it("rises in the target's own dot column, which is clear of every label", () => {
        const target = { x: 900, y: 62 };
        expect(laneRoute(from, target, 400).end.x).toBe(target.x);
    });

    it("steps aside from the source's dot before dropping, forward", () => {
        expect(laneRoute(from, { x: 900, y: 0 }, 400).start.x).toBeGreaterThan(0);
    });

    it("steps aside to the left when it must double back", () => {
        expect(laneRoute({ x: 900, y: 0 }, from, 400).start.x).toBeLessThan(900);
    });

    it("turns toward its target, so a backward run heads left along the lane", () => {
        const route = laneRoute({ x: 900, y: 0 }, from, 400);
        expect(route.drop.x).toBeLessThan(route.start.x);
        expect(route.rise.x).toBeGreaterThan(0);
    });
});

describe("edgePath", () => {
    it("writes an ordinary step as a single cubic from the lead-out point to the target", () => {
        expect(edgePath(from, { x: 260, y: 62 }, { clearance: 60 })).toBe(
            "M60,0C160,0,160,62,260,62",
        );
    });

    it("writes a cross-link as a drop, a run along the lane, and a climb into its target", () => {
        expect(edgePath(from, { x: 900, y: 62 }, { lane: 400 })).toBe(
            "M14,0C14,400,14,400,38,400L876,400C876,400,876,62,900,62",
        );
    });

    it("rounds coordinates, so the markup stays readable", () => {
        expect(edgePath(from, { x: 70, y: 0 }, { clearance: 1 / 3 })).not.toMatch(/\d\.\d{3}/);
    });

    it("climbs further back for each route already claiming its target's approach", () => {
        const target = { x: 900, y: 0 };
        const first = laneRoute(from, target, 400, 0);
        const second = laneRoute(from, target, 400, 1);

        // Two routes to one node would otherwise climb in the same column, one hidden under the
        // other — one line to the eye, and a coin toss to the pointer.
        expect(second.rise.x).toBeLessThan(first.rise.x);
    });

    it("still ends on the target dot however far back it climbed", () => {
        const target = { x: 900, y: 62 };
        expect(laneRoute(from, target, 400, 3).end).toEqual(target);
    });

    it("climbs further back on a backward run too, not further forward", () => {
        const first = laneRoute({ x: 900, y: 0 }, from, 400, 0);
        const second = laneRoute({ x: 900, y: 0 }, from, 400, 1);

        expect(second.rise.x).toBeGreaterThan(first.rise.x);
    });
});

describe("edgePath ports", () => {
    it("leans in from its own row, so two arrivals differ in their last leg", () => {
        const target = { x: 900, y: 0 };
        const level = edgePath(from, target, { lane: 400 });
        const leaning = edgePath(from, target, { lane: 400, port: -9 });

        expect(leaning).not.toBe(level);
    });
});
