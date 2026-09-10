import { test, expect, type Page } from "@playwright/test";
import { writeReport } from "./report";
import type { DisplayEdge, DisplayNode, Stage } from "../src/model";

/**
 * A node of the graph, named by the scene it belongs to so the fixture reads as a script would.
 */
function node(id: string, region?: string): DisplayNode {
    return { id, label: id, attributes: [], category: "speech", region };
}

function child(fromId: string, toId: string): DisplayEdge {
    return { fromId, toId, kind: "Child" };
}

/**
 * Three scenes the flow weaves through, shaped as the Dialogue Graph of a real script is.
 *
 * The Alarm offers a choice. One arm is a line of the Alarm's own that talks the resident down and
 * sends them to the Stairwell; the other diverts to the Door, which opens a choice of three.
 *
 * The tree layout centres a parent between its children, so the Alarm's choice is pushed down
 * towards the Door's wide fan while the Door's own band reaches up to hold its first arm. Left as
 * the layout leaves them, the two bands cross — the same blend `examples/highrise-fire.dialogue.md`
 * shows between those two scenes, and the defect these tests hold shut.
 */
const WOVEN: Stage = {
    title: "Dialogue Graph",
    description: "A graph whose scenes the flow weaves through.",
    nodes: [
        node("entry"),
        node("alarm-choice", "The Alarm"),
        node("alarm-calm", "The Alarm"),
        node("door-feel", "The Door"),
        node("door-hot", "The Door"),
        node("door-cool", "The Door"),
        node("door-shut", "The Door"),
        node("stairwell-crawl", "The Stairwell"),
    ],
    edges: [
        child("entry", "alarm-choice"),
        child("alarm-choice", "alarm-calm"),
        child("alarm-choice", "door-feel"),
        child("alarm-calm", "stairwell-crawl"),
        child("door-feel", "door-hot"),
        child("door-feel", "door-cool"),
        child("door-feel", "door-shut"),
    ],
    regions: [
        { name: "The Alarm", kind: "Scene" },
        { name: "The Door", kind: "Scene" },
        { name: "The Stairwell", kind: "Scene" },
    ],
    nests: false,
};

const url = writeReport({ stages: [WOVEN] });

interface Box {
    readonly region: string;
    readonly x: number;
    readonly y: number;
    readonly width: number;
    readonly height: number;
}

/** Every band currently drawn, in the graph's own coordinates. */
async function bands(page: Page): Promise<Box[]> {
    return page.$$eval("section.stage.active g.region", (groups) =>
        groups.map((group) => {
            const rect = group.querySelector("rect.region-band")!;
            const read = (name: string): number => Number(rect.getAttribute(name) ?? "0");
            return {
                region: group.querySelector("text.region-name")?.textContent?.trim() ?? "",
                x: read("x"),
                y: read("y"),
                width: read("width"),
                height: read("height"),
            };
        }),
    );
}

/** Two boxes touch when they overlap on both axes. */
function intersects(left: Box, right: Box): boolean {
    return (
        left.x < right.x + right.width &&
        right.x < left.x + left.width &&
        left.y < right.y + right.height &&
        right.y < left.y + left.height
    );
}

/** Name every pair of bands that cross, so a failure says which scenes blended. */
function crossings(boxes: readonly Box[]): string[] {
    const found: string[] = [];
    for (const [index, box] of boxes.entries()) {
        for (const other of boxes.slice(index + 1)) {
            if (intersects(box, other)) found.push(`${box.region} x ${other.region}`);
        }
    }
    return found;
}

async function showGraph(page: Page): Promise<void> {
    await page.goto(url);
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await expect(page.locator("section.stage.active g.region")).toHaveCount(3);
}

test("no scene's band is drawn across another's", async ({ page }) => {
    await showGraph(page);

    expect(crossings(await bands(page))).toEqual([]);
});

test("the scenes stack in the order the legend names them", async ({ page }) => {
    await showGraph(page);

    const order = (await bands(page)).sort((left, right) => left.y - right.y).map((b) => b.region);
    expect(order).toEqual(["The Alarm", "The Door", "The Stairwell"]);
});

test("no node is drawn inside a band that is not its own", async ({ page }) => {
    await showGraph(page);
    const boxes = await bands(page);

    // Where the drawing actually put each node, read from its own transform rather than recomputed.
    const spots = await page.$$eval("section.stage.active g.node", (groups) =>
        groups.map((group) => {
            const moved = /translate\(([-\d.]+),\s*([-\d.]+)\)/.exec(
                group.getAttribute("transform") ?? "",
            );
            return {
                id:
                    (group as SVGGElement & { __data__?: { data?: { id?: string } } }).__data__
                        ?.data?.id ?? "",
                x: Number(moved?.[1] ?? 0),
                y: Number(moved?.[2] ?? 0),
            };
        }),
    );
    expect(spots.length).toBe(WOVEN.nodes.length);

    const regionOf = new Map(WOVEN.nodes.map((n) => [n.id, n.region]));
    for (const spot of spots) {
        for (const box of boxes) {
            if (box.region === regionOf.get(spot.id)) continue;
            const inside =
                spot.x >= box.x &&
                spot.x <= box.x + box.width &&
                spot.y >= box.y &&
                spot.y <= box.y + box.height;
            expect(inside, `${spot.id} sits inside ${box.region}`).toBe(false);
        }
    }
});

test("the bands stay apart when a scene is folded away", async ({ page }) => {
    await showGraph(page);

    const alarm = page
        .locator("section.stage.active g.region", { hasText: "The Alarm" })
        .locator(".region-fold");
    await alarm.click();
    await expect(page.locator("section.stage.active g.region.folded").first()).toBeAttached();

    expect(crossings(await bands(page))).toEqual([]);
});
