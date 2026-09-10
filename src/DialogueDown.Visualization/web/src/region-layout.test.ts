import { describe, it, expect } from "vitest";
import { rankByRegion, ROW_PITCH, TIER_GAP, type RankInput } from "./region-layout";
import { bandsOf, PAD_BOTTOM, PAD_TOP, type Band, type PlacedNode } from "./region-bands";

/** A node as the tree layout leaves it: an id, the scene it sits in, and the row it landed on. */
const node = (id: string, row: number, region?: string): RankInput => ({ id, row, region });

/** The rows a set of ids ended up on, in the order the ids are given. */
function rowsOf(placed: Map<string, number>, ids: readonly string[]): number[] {
    return ids.map((id) => placed.get(id)!);
}

/** The span a tier covers, from its lowest placed row to its highest. */
function spanOf(placed: Map<string, number>, ids: readonly string[]): [number, number] {
    const rows = rowsOf(placed, ids);
    return [Math.min(...rows), Math.max(...rows)];
}

describe("rankByRegion", () => {
    it("gives each region a contiguous run of rows, clear of the next", () => {
        // Two scenes the tree layout interleaved: their rows alternate.
        const placed = rankByRegion(
            [node("a1", 0, "A"), node("b1", 20, "B"), node("a2", 40, "A"), node("b2", 60, "B")],
            ["A", "B"],
        );

        const [aTop, aBottom] = spanOf(placed, ["a1", "a2"]);
        const [bTop, bBottom] = spanOf(placed, ["b1", "b2"]);

        expect(aBottom).toBeLessThan(bTop);
        expect(aTop).toBeLessThan(aBottom);
        expect(bTop).toBeLessThan(bBottom);
    });

    it("parts two tiers by more than the padding their bands reach", () => {
        // A gap that only separates the rows would still leave the drawn bands touching.
        const placed = rankByRegion([node("a", 0, "A"), node("b", 10, "B")], ["A", "B"]);

        expect(placed.get("b")! - placed.get("a")!).toBeGreaterThan(PAD_TOP + PAD_BOTTOM);
        expect(TIER_GAP).toBeGreaterThan(PAD_TOP + PAD_BOTTOM);
    });

    it("keeps nodes the layout put on one row together", () => {
        // A straight run of dialogue is a single-child chain, and the tree layout gives every node
        // in it the same row. Splitting them apart would turn one line into a staircase.
        const placed = rankByRegion(
            [node("l1", 5, "A"), node("l2", 5, "A"), node("l3", 5, "A"), node("l4", 67, "A")],
            ["A"],
        );

        expect(placed.get("l1")).toBe(placed.get("l2"));
        expect(placed.get("l2")).toBe(placed.get("l3"));
        expect(placed.get("l4")).toBe(placed.get("l1")! + ROW_PITCH);
    });

    it("keeps the order of a tier's distinct rows", () => {
        const placed = rankByRegion(
            [node("third", 90, "A"), node("first", 10, "A"), node("second", 50, "A")],
            ["A"],
        );

        expect(rowsOf(placed, ["first", "second", "third"])).toEqual([
            placed.get("first")!,
            placed.get("first")! + ROW_PITCH,
            placed.get("first")! + ROW_PITCH * 2,
        ]);
    });

    it("stacks the scenes in the order it is given, not the order the rows ran", () => {
        // The tree layout put B's row above A's; the legend names A first, so A is the upper tier.
        const placed = rankByRegion([node("b", 0, "B"), node("a", 100, "A")], ["A", "B"]);

        expect(placed.get("a")).toBeLessThan(placed.get("b")!);
    });

    it("puts every node that belongs to no scene above every scene", () => {
        // The entry is the hierarchy's root, so the tree layout centres it in the middle of
        // everything. Left there it would be drawn inside whichever band surrounded it.
        const placed = rankByRegion(
            [node("entry", 50), node("a", 0, "A"), node("b", 100, "B")],
            ["A", "B"],
        );

        expect(placed.get("entry")).toBeLessThan(placed.get("a")!);
        expect(placed.get("entry")).toBeLessThan(placed.get("b")!);
    });

    it("keeps the loose nodes in their own order", () => {
        const placed = rankByRegion([node("second", 30), node("first", -10)], []);

        expect(placed.get("first")).toBeLessThan(placed.get("second")!);
    });

    it("keeps the drawing's origin, so the root does not drift to the top of the view", () => {
        // Rows are signed and centred on the root, so the topmost is routinely negative.
        const placed = rankByRegion([node("a", -93, "A"), node("b", 31, "B")], ["A", "B"]);

        expect(placed.get("a")).toBe(-93);
    });

    it("gives no room to a region the stage names but does not draw", () => {
        const withGhost = rankByRegion(
            [node("a", 0, "A"), node("b", 10, "B")],
            ["A", "Ghost", "B"],
        );
        const without = rankByRegion([node("a", 0, "A"), node("b", 10, "B")], ["A", "B"]);

        expect(withGhost.get("b")).toBe(without.get("b"));
    });

    it("still places a region the caller never named", () => {
        const placed = rankByRegion([node("a", 0, "A"), node("z", 10, "Z")], ["A"]);

        expect(placed.get("z")).toBeGreaterThan(placed.get("a")!);
    });

    it("places a lone scene at the origin when nothing is loose", () => {
        const placed = rankByRegion([node("a", 7, "A"), node("b", 70, "A")], ["A"]);

        expect(placed.get("a")).toBe(7);
    });

    it("orders by the tree's rows when there are no scenes at all", () => {
        const placed = rankByRegion([node("second", 40), node("first", 0)], []);

        expect(placed.get("first")).toBe(0);
        expect(placed.get("second")).toBe(ROW_PITCH);
    });

    it("returns nothing for a drawing with no nodes", () => {
        expect(rankByRegion([], ["A"]).size).toBe(0);
    });

    it("places every node it is given", () => {
        const nodes = [node("a", 0, "A"), node("loose", 1), node("b", 2, "B")];

        expect(rankByRegion(nodes, ["A", "B"]).size).toBe(nodes.length);
    });
});

/** Two boxes touch when they overlap on both axes — the test `bandsOf`'s output must never pass. */
function intersects(left: Band, right: Band): boolean {
    return (
        left.x < right.x + right.width &&
        right.x < left.x + left.width &&
        left.y < right.y + right.height &&
        right.y < left.y + left.height
    );
}

/** Whether a point sits inside a band, as a reader would see it. */
function encloses(band: Band, x: number, y: number): boolean {
    return x >= band.x && x <= band.x + band.width && y >= band.y && y <= band.y + band.height;
}

describe("rankByRegion drawn as bands", () => {
    /** The interleaving the drawing suffers today: B is entered partway through A, and A goes on. */
    const interleaved: RankInput[] = [
        node("entry", 40),
        node("a1", 0, "A"),
        node("b1", 20, "B"),
        node("a2", 60, "A"),
        node("b2", 80, "B"),
        node("a3", 120, "A"),
        node("c1", 100, "C"),
    ];

    /** The nodes as the drawing places them: depth along x, row along y, plus a label's width. */
    function draw(placed: Map<string, number>, nodes: readonly RankInput[]): PlacedNode[] {
        return nodes.map((input, index) => ({
            region: input.region,
            x: index * 120,
            y: placed.get(input.id)!,
            width: 100,
        }));
    }

    it("draws no band across another", () => {
        const placed = rankByRegion(interleaved, ["A", "B", "C"]);
        const bands = bandsOf(draw(placed, interleaved));

        for (const [index, band] of bands.entries()) {
            for (const other of bands.slice(index + 1)) {
                expect(intersects(band, other)).toBe(false);
            }
        }
    });

    it("leaves every band overlapping today, without the pass", () => {
        // The regression this exists to prevent: the same nodes, laid out as the tree left them.
        const asLaidOut = new Map(interleaved.map((input) => [input.id, input.row]));
        const bands = bandsOf(draw(asLaidOut, interleaved));

        const overlapping = bands.some((band, index) =>
            bands.slice(index + 1).some((other) => intersects(band, other)),
        );
        expect(overlapping).toBe(true);
    });

    it("draws no node inside a band it does not belong to", () => {
        const placed = rankByRegion(interleaved, ["A", "B", "C"]);
        const drawn = draw(placed, interleaved);
        const bands = bandsOf(drawn);

        for (const [index, spot] of drawn.entries()) {
            for (const band of bands) {
                if (band.region === spot.region) continue;
                expect(encloses(band, spot.x, spot.y)).toBe(false);
                expect(encloses(band, spot.x + interleaved[index].id.length, spot.y)).toBe(false);
            }
        }
    });
});

describe("rankByRegion over generated graphs", () => {
    /**
     * A pseudo-random source with a fixed seed, so a failure is reproducible and a green run is
     * not luck. Mulberry32 — small, and good enough to shuffle rows and scenes.
     */
    function randomFrom(seed: number): () => number {
        let state = seed;
        return () => {
            state = (state + 0x6d2b79f5) | 0;
            let drawn = Math.imul(state ^ (state >>> 15), 1 | state);
            drawn = (drawn + Math.imul(drawn ^ (drawn >>> 7), 61 | drawn)) ^ drawn;
            return ((drawn ^ (drawn >>> 14)) >>> 0) / 4294967296;
        };
    }

    /**
     * A drawing shaped as the compiler can actually emit one: scenes own runs of consecutive
     * nodes, and the only nodes without a scene are the ones before the first heading.
     */
    function contiguousRuns(random: () => number): { nodes: RankInput[]; order: string[] } {
        const count = 4 + Math.floor(random() * 20);
        const loose = Math.floor(random() * 3);
        const nodes: RankInput[] = [];
        const order: string[] = [];
        let scene = 0;
        for (let index = 0; index < count; index++) {
            if (index >= loose && (index === loose || random() < 0.25)) {
                scene += 1;
                order.push(`scene-${scene}`);
            }
            nodes.push({
                id: `n${index}`,
                region: scene === 0 ? undefined : `scene-${scene}`,
                // Rows repeat and run in no order, as a tree layout's do.
                row: Math.floor(random() * 8) * 62 - 200,
            });
        }
        return { nodes, order };
    }

    it("never lets two bands touch, over a hundred generated drawings", () => {
        const random = randomFrom(20260910);
        for (let run = 0; run < 100; run++) {
            const { nodes, order } = contiguousRuns(random);
            const placed = rankByRegion(nodes, order);
            const bands = bandsOf(
                nodes.map((input, index) => ({
                    region: input.region,
                    x: index * 90,
                    y: placed.get(input.id)!,
                    width: 80,
                })),
            );

            for (const [index, band] of bands.entries()) {
                for (const other of bands.slice(index + 1)) {
                    expect(intersects(band, other)).toBe(false);
                }
            }
        }
    });

    it("places every node whatever it is handed, even a shape the compiler cannot emit", () => {
        // Scattered membership is not something `ScenesByNode` can produce, so no ordering claim is
        // made about it — only that the pass stays total and its tiers stay apart.
        const random = randomFrom(4242);
        for (let run = 0; run < 100; run++) {
            const count = 1 + Math.floor(random() * 15);
            const nodes: RankInput[] = Array.from({ length: count }, (_, index) => ({
                id: `n${index}`,
                region: random() < 0.3 ? undefined : `scene-${Math.floor(random() * 4)}`,
                row: Math.floor(random() * 300) - 150,
            }));

            const placed = rankByRegion(nodes, ["scene-0", "scene-1", "scene-2", "scene-3"]);

            expect(placed.size).toBe(nodes.length);
            const spans = new Map<string, [number, number]>();
            for (const input of nodes) {
                const key = input.region ?? "";
                const row = placed.get(input.id)!;
                const span = spans.get(key);
                spans.set(
                    key,
                    span ? [Math.min(span[0], row), Math.max(span[1], row)] : [row, row],
                );
            }
            const ordered = [...spans.values()].sort((left, right) => left[0] - right[0]);
            for (let index = 1; index < ordered.length; index++) {
                expect(ordered[index][0] - ordered[index - 1][1]).toBeGreaterThan(
                    PAD_TOP + PAD_BOTTOM,
                );
            }
        }
    });
});
