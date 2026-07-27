import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { writeReport } from "./report";
import type { Report } from "../src/model";

// A Semantic Model report: a two-scene tree beside the speaker, anchor, and
// jump-resolution tables, wired with the same cross-link keys the .NET projection
// emits. Its own fixture so the source-tab suite's tab/node counts are untouched.
const report: Report = {
    source: "# The Market\n\nGuide @guide: Welcome.\n\n=> [Leave](#the-square)\n\n# The Square\n",
    stages: [
        {
            title: "Semantic Model",
            description: "The semantic model the analyzer resolves.",
            nodes: [
                {
                    id: "n0",
                    label: "Document root",
                    attributes: [],
                    category: "document",
                    typeName: "Document",
                },
                {
                    id: "n1",
                    label: "The Market",
                    attributes: [
                        { name: "anchor", value: "#the-market" },
                        { name: "level", value: "1" },
                    ],
                    category: "structure",
                    typeName: "Scene",
                    entityKey: "scene:the-market",
                },
                {
                    id: "n2",
                    label: "The Square",
                    attributes: [
                        { name: "anchor", value: "#the-square" },
                        { name: "level", value: "1" },
                    ],
                    category: "structure",
                    typeName: "Scene",
                    entityKey: "scene:the-square",
                },
                // A script block under "The Market": a line, its speaker mention, and a jump —
                // the last two cross-reference the speaker and target-scene entities.
                {
                    id: "n3",
                    label: "Line",
                    attributes: [{ name: "span", value: "[13, 35)" }],
                    category: "speech",
                    source: "Guide @guide: Welcome.",
                },
                {
                    id: "n4",
                    label: "Speaker (declaration)",
                    attributes: [{ name: "name", value: "Guide" }],
                    category: "speech",
                    refKey: "speaker:@guide",
                },
                {
                    id: "n5",
                    label: "Jump",
                    attributes: [{ name: "target", value: "#the-square" }],
                    category: "jump",
                    refKey: "scene:the-square",
                },
            ],
            edges: [
                { fromId: "n0", toId: "n1", kind: "Child" },
                { fromId: "n0", toId: "n2", kind: "Child" },
                { fromId: "n1", toId: "n3", kind: "Child" },
                { fromId: "n3", toId: "n4", kind: "Child" },
                { fromId: "n1", toId: "n5", kind: "Child" },
            ],
            tables: [
                {
                    title: "Speakers",
                    columns: ["Name", "@id", "Tags", "Default"],
                    emptyText: "No speakers are declared.",
                    rows: [
                        {
                            entityKey: "speaker:@guide",
                            cells: [
                                { text: "Guide", entityKey: "speaker:@guide", category: "speech" },
                                { text: "@guide" },
                                { text: "" },
                                { text: "" },
                            ],
                        },
                    ],
                },
                {
                    title: "Anchors",
                    columns: ["Anchor", "Scene", "Level"],
                    emptyText: "No anchors appear in this script.",
                    rows: [
                        {
                            entityKey: "scene:the-market",
                            cells: [
                                { text: "#the-market", category: "structure" },
                                { text: "The Market" },
                                { text: "1" },
                            ],
                        },
                        {
                            entityKey: "scene:the-square",
                            cells: [
                                { text: "#the-square", category: "structure" },
                                { text: "The Square" },
                                { text: "1" },
                            ],
                        },
                        {
                            entityKey: "scene:the-alley",
                            cells: [
                                { text: "#the-alley", category: "structure" },
                                { text: "The Alley" },
                                { text: "2" },
                            ],
                        },
                    ],
                },
                {
                    title: "Jump resolutions",
                    columns: ["Type", "Jump", "Target", "Resolves to"],
                    emptyText: "No jumps appear in this script.",
                    facetColumns: ["Type"],
                    rows: [
                        {
                            cells: [
                                { text: "Scene", category: "structure" },
                                { text: "Leave" },
                                { text: "#the-square" },
                                {
                                    text: "=> The Square",
                                    refKey: "scene:the-square",
                                },
                            ],
                        },
                        {
                            cells: [
                                { text: "End", category: "terminal" },
                                { text: "the end" },
                                { text: "#END" },
                                { text: "End sentinel" },
                            ],
                        },
                    ],
                },
            ],
        },
    ],
};

const url = writeReport(report);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await page.locator(".tab", { hasText: "Semantic Model" }).click();
});

test("lays the scene-tree graph, its script blocks, and the three stacked tables", async ({
    page,
}) => {
    await expect(page.locator(".semantic-view")).toBeVisible();
    // The root, two scenes, and the three script-block nodes under "The Market".
    await expect(page.locator(".semantic-graph g.node")).toHaveCount(6);
    await expect(page.locator('.semantic-graph g.node:has-text("Line")')).toBeVisible();
    await expect(page.locator('.semantic-graph g.node:has-text("Jump")')).toBeVisible();
    // The three table panels stack below the sticky node-details panel.
    await expect(
        page.locator(".table-panel:not(.node-detail-panel) .table-panel-title"),
    ).toHaveText(["Speakers", "Anchors", "Jump resolutions"]);
    await expect(page.locator(".node-detail-panel")).toBeVisible();
    // The shared node-detail inspector is hidden — the tab has its own tables.
    await expect(page.locator("#app")).toHaveClass(/no-detail/);
    await expect(page.locator("#detail")).toBeHidden();
});

test("filters and sorts a table while keeping its cross-links", async ({ page }) => {
    const anchors = page.locator('.table-panel:has(.table-panel-title:text-is("Anchors"))');

    // Sort by Scene ascending: the alphabetically-first scene leads, and the header announces it.
    await anchors.locator(".th-sort", { hasText: "Scene" }).click();
    await expect(anchors.locator("tbody tr td:nth-child(2)").first()).toHaveText("The Alley");
    await expect(anchors.locator('th:has(.th-sort:text-is("Scene"))')).toHaveAttribute(
        "aria-sort",
        "ascending",
    );

    // Reveal the search box on demand, then filter to just the market; the panel count tracks the
    // visible rows, and the row still carries its cross-link key.
    await anchors.locator(".table-panel-search").click();
    await anchors.locator("input.table-search").fill("market");
    await expect(anchors.locator("tbody tr")).toHaveCount(1);
    await expect(anchors.locator('tbody tr[data-entity-key="scene:the-market"]')).toBeVisible();
    await expect(anchors.locator(".table-panel-count")).toHaveText("1");

    // A no-match filter shows the note and a zero count.
    await anchors.locator("input.table-search").fill("zzzz");
    await expect(anchors.locator(".table-nomatch")).toHaveText("No matches.");
    await expect(anchors.locator(".table-panel-count")).toHaveText("0");
});

test("facets the Jump resolutions table by its Type column", async ({ page }) => {
    const jumps = page.locator('.table-panel:has(.table-panel-title:text-is("Jump resolutions"))');
    await expect(jumps.locator("tbody tr")).toHaveCount(2);

    // Open the Type facet popover and choose End; the choice shows as a chip in the header.
    // Click (not check) the radio: choosing a value closes the popover, so re-verifying the
    // checked state would race that teardown.
    await jumps.locator(".th-facet").click();
    const endOption = page.locator('.facet-popover input[value="End"]');
    await expect(endOption).toBeVisible();
    await endOption.click();
    await expect(jumps.locator("tbody tr")).toHaveCount(1);
    await expect(jumps.locator("tbody tr td").first()).toHaveText("End");
    await expect(jumps.locator(".th-facet .th-facet-value")).toHaveText("End");
    await expect(jumps.locator(".table-panel-count")).toHaveText("1");

    // Reopen and clear with All.
    await jumps.locator(".th-facet").click();
    const allOption = page.locator('.facet-popover input[value=""]');
    await expect(allOption).toBeVisible();
    await allOption.click();
    await expect(jumps.locator("tbody tr")).toHaveCount(2);
});

test("cross-links a scene across the graph, the anchor table, and a jump block", async ({
    page,
}) => {
    const stage = page.locator(".semantic-stage.active");
    // Hover the anchor row for The Square; the scene node, the anchor row, and the jump block
    // that resolves to it all light up.
    await stage.locator('tr[data-entity-key="scene:the-square"]').hover();

    await expect(stage.locator('g.node[data-entity-key="scene:the-square"]')).toHaveClass(
        /entity-highlight/,
    );
    await expect(stage.locator('g.node[data-ref-key="scene:the-square"]')).toHaveClass(
        /entity-highlight/,
    );
    await expect(stage.locator('td[data-ref-key="scene:the-square"]')).toHaveClass(
        /entity-highlight/,
    );
    // A different scene stays unhighlighted.
    await expect(stage.locator('g.node[data-entity-key="scene:the-market"]')).not.toHaveClass(
        /entity-highlight/,
    );
});

test("cross-links a speaker mention in the tree to its Speakers row", async ({ page }) => {
    const stage = page.locator(".semantic-stage.active");
    // Hover the Guide row in the Speakers table (always in view); the speaker mention in the
    // tree lights up — the cross-link is symmetric.
    await stage.locator('tr[data-entity-key="speaker:@guide"]').hover();

    await expect(stage.locator('g.node[data-ref-key="speaker:@guide"]')).toHaveClass(
        /entity-highlight/,
    );
});

test("lineage focus preserves the scene backbone while spotlighting a scene", async ({ page }) => {
    const graph = page.locator(".semantic-graph");
    // Overlays can cover a node; let the hover reach the node beneath.
    await page.addStyleTag({
        content: ".legend, .zoom-controls { pointer-events: none !important; }",
    });

    // Hover the scene "The Market"; its lineage is the document root and its own subtree,
    // so the sibling scene "The Square" sits outside it.
    await graph.locator('g.node[data-entity-key="scene:the-market"] circle').hover();
    await expect(graph.locator("svg.tree")).toHaveClass(/has-focus/);

    // Scene backbone edges keep their `.scene` class (bold stroke) and are partitioned by the
    // lineage — the hovered scene's edge is related, the other scene's stays `.scene` and dims.
    await expect(graph.locator("path.link.scene.related")).not.toHaveCount(0);
    await expect(graph.locator("path.link.scene:not(.related)")).not.toHaveCount(0);
    // Likewise the scene rings: some inside the lineage, some outside, all still `.scene`.
    await expect(graph.locator("g.node.scene.related")).not.toHaveCount(0);
    await expect(graph.locator("g.node.scene:not(.related)")).not.toHaveCount(0);
});

test("collapses and reopens a table from its caret toggle", async ({ page }) => {
    const speakers = page
        .locator(".table-panel")
        .filter({ has: page.locator(".table-panel-title", { hasText: "Speakers" }) });
    const toggle = speakers.locator(".table-panel-toggle");

    await expect(speakers.locator("tbody")).toBeVisible();
    await toggle.click();
    await expect(speakers).toHaveClass(/collapsed/);
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
    await expect(speakers.locator("tbody")).toBeHidden();

    await toggle.click();
    await expect(speakers).not.toHaveClass(/collapsed/);
    await expect(speakers.locator("tbody")).toBeVisible();
});

test("hides and reopens the whole tables column from the divider", async ({ page }) => {
    const view = page.locator(".semantic-view");
    const toggle = page.locator(".semantic-divider .collapse-toggle");

    await expect(page.locator(".semantic-tables")).toBeVisible();
    await toggle.click();
    await expect(view).toHaveClass(/tables-collapsed/);
    await expect(page.locator(".semantic-tables")).toBeHidden();

    await toggle.click();
    await expect(view).not.toHaveClass(/tables-collapsed/);
    await expect(page.locator(".semantic-tables")).toBeVisible();
});

test("shows a clicked node's details in the node-details panel", async ({ page }) => {
    const panel = page.locator(".node-detail-panel");
    // The panel is a normal in-flow card (not sticky, so it never floats over the panels
    // below it) and starts on the placeholder.
    await expect(panel).toBeVisible();
    await expect(panel).toHaveCSS("position", "static");
    await expect(page.locator(".node-detail-body")).toContainText("Click any node");

    // Click the Line block's circle (dispatched, so it fires regardless of the pan/zoom viewport).
    await page.evaluate(() => {
        const nodes = [...document.querySelectorAll(".semantic-stage.active g.node")];
        const line = nodes.find((g) => g.querySelector("text.label")?.textContent === "Line");
        line?.querySelector("circle")?.dispatchEvent(new MouseEvent("click", { bubbles: true }));
    });

    const body = page.locator(".node-detail-body");
    await expect(body.locator(".node-detail-heading")).toContainText("Line");
    await expect(body.locator("pre code")).toContainText("Guide @guide: Welcome."); // its source
    await expect(body.locator(".preview")).toBeVisible(); // a rendered preview
});

test("panel titles have legible contrast (regression: not white-on-white)", async ({ page }) => {
    // The panel header is a <button>; Pico's white button text made the titles invisible on the
    // light panel background. axe reports "incomplete" (not a violation) for a transparent button
    // over the panel, so it did not catch this — assert the contrast directly.
    const ratio = await page.evaluate(() => {
        const title = document.querySelector(".table-panel-title")!;
        const parse = (c: string): number[] => (c.match(/\d+/g) ?? []).slice(0, 3).map(Number);
        const luminance = ([r, g, b]: number[]): number => {
            const channel = (v: number): number => {
                const s = v / 255;
                return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
            };
            return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
        };
        const color = parse(getComputedStyle(title).color);
        let node: Element | null = title;
        let background = [255, 255, 255];
        while (node) {
            const bg = getComputedStyle(node).backgroundColor;
            if (bg && bg !== "rgba(0, 0, 0, 0)") {
                background = parse(bg);
                break;
            }
            node = node.parentElement;
        }
        const [l1, l2] = [luminance(color), luminance(background)];
        return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
    });
    expect(ratio).toBeGreaterThan(4.5);
});

test("emphasizes the scene backbone over content nodes", async ({ page }) => {
    const stage = page.locator(".semantic-stage.active");
    // The root and the two scenes are the backbone; the script blocks are not.
    await expect(stage.locator("g.node.scene")).toHaveCount(3);
    // Scene circles are larger than content-block circles.
    const sceneR = await stage.locator("g.node.scene circle").first().getAttribute("r");
    const blockR = await stage.locator("g.node:not(.scene) circle").first().getAttribute("r");
    expect(Number(sceneR)).toBeGreaterThan(Number(blockR));
    // The scene-to-scene edges are marked so CSS can bolden them.
    await expect(stage.locator("path.link.scene")).toHaveCount(2); // root → each scene
});

test("has no accessibility violations on the Semantic tab", async ({ page }) => {
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});
