import { describe, it, expect } from "vitest";
import { bandsOf, REGION_TINTS, type PlacedNode } from "./region-bands";

function at(x: number, y: number, region?: string, width = 100): PlacedNode {
    return { x, y, region, width };
}

describe("bandsOf", () => {
    it("draws one band per region, however many nodes share it", () => {
        const bands = bandsOf([
            at(0, 0, "The Gate"),
            at(260, 0, "The Gate"),
            at(520, 0, "The Hall"),
        ]);

        expect(bands.map((band) => band.region)).toEqual(["The Gate", "The Hall"]);
    });

    it("reaches around every node it holds, text included", () => {
        const [band] = bandsOf([at(0, 0, "The Gate", 100), at(260, 62, "The Gate", 180)]);

        expect(band.x).toBeLessThan(0);
        expect(band.x + band.width).toBeGreaterThan(260 + 180);
        expect(band.y).toBeLessThan(0);
        expect(band.y + band.height).toBeGreaterThan(62);
    });

    it("leaves more room above than below, for the band's own name", () => {
        const [band] = bandsOf([at(0, 0, "The Gate")]);

        expect(-band.y).toBeGreaterThan(band.height + band.y);
    });

    it("ignores a node that belongs to no region", () => {
        expect(bandsOf([at(0, 0), at(260, 0)])).toEqual([]);
    });

    it("tints regions in order of first appearance, so a rebuild keeps its colors", () => {
        const bands = bandsOf([at(0, 0, "A"), at(260, 0, "B"), at(520, 0, "A")]);

        expect(bands.map((band) => band.tint)).toEqual([0, 1]);
    });

    it("cycles the tints rather than inventing a color per region", () => {
        const many = Array.from({ length: REGION_TINTS + 1 }, (_, index) =>
            at(index * 260, 0, `Scene ${index}`),
        );

        expect(bandsOf(many).at(-1)!.tint).toBe(0);
    });

    it("keeps a region whose nodes are scattered in one band that spans them", () => {
        // Nothing guarantees a scene's nodes are adjacent — an unreachable line sits where it
        // falls. The band still has to contain all of them.
        const [band] = bandsOf([at(0, 0, "The Gate"), at(1300, 240, "The Gate")]);

        expect(band.width).toBeGreaterThan(1300);
        expect(band.height).toBeGreaterThan(240);
    });
});
