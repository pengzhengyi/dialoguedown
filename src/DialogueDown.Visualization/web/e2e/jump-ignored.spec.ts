import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { writeReport } from "./report";

// A scene holding a table the compiler leaves out. The stage has a node for the scene and none for
// the table, which is the whole point: ignored Markdown becomes no node anywhere, so a jump from it
// can only settle on what encloses it.
const source = [
    "# The Market", // 0
    "", // 13
    "Trader: Apples.", // 15
    "", // 31
    "| Item | Cost |", // 32
    "| --- | --- |", // 48
    "| Rope | 5 |", // 62
    "", // 75
    "Alice: Two please.", // 76
].join("\n");

const dialogueStart = source.indexOf("Trader: Apples.");

const report: Report = {
    source,
    stages: [
        {
            title: "Dialogue AST",
            description: "The dialogue tree.",
            nodes: [
                {
                    id: "n0",
                    label: "The Market",
                    typeName: "Scene",
                    attributes: [],
                    category: "structure",
                    span: { start: 0, end: source.length },
                },
                {
                    id: "n1",
                    label: "Trader: Apples.",
                    typeName: "Line",
                    attributes: [],
                    category: "speech",
                    span: { start: dialogueStart, end: dialogueStart + "Trader: Apples.".length },
                },
            ],
            edges: [{ fromId: "n0", toId: "n1", kind: "Child" }],
        },
    ],
    semanticTokens: [
        {
            kind: "IgnoredMarkdown",
            range: { start: { line: 4, character: 0 }, end: { line: 6, character: 12 } },
        },
    ],
};

/** Select `text` in the editor, then jump to the named stage through the editor's own menu. */
async function jumpFrom(
    page: import("@playwright/test").Page,
    text: string,
    stage: string,
): Promise<void> {
    const line = page.locator(".source-pane .cm-line", { hasText: text }).first();
    await line.click({ clickCount: 2 });
    await line.click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Jump to" }).hover();
    await page.locator(".context-submenu .context-menu-item", { hasText: stage }).click();
}

test.beforeEach(async ({ page }) => {
    await page.goto(writeReport(report));
    await expect(page.locator(".source-pane .cm-content")).toBeVisible();
});

test("says a jump from ignored Markdown settled for the node around it", async ({ page }) => {
    await jumpFrom(page, "| Rope | 5 |", "Dialogue AST");

    const note = page.locator(".dd-arrival-note");
    await expect(note).toBeVisible();
    // Beside the drawing the reader arrived in, not inside the panel they are not looking at.
    const [noteBox, graph] = await Promise.all([
        note.boundingBox(),
        page.locator(".stage.active svg").first().boundingBox(),
    ]);
    if (!noteBox || !graph) throw new Error("Could not measure the arrival note.");
    expect(noteBox.x).toBeLessThan(graph.x + graph.width);
    // What was looked for, and what is being shown instead.
    await expect(note).toContainText("ignored");
    await expect(note).toContainText("Dialogue AST");
    // The note names the node the panel is showing, so the two agree.
    await expect(note).toContainText("The Market");
    // The jump still lands: settling for the enclosing node is the honest answer, not a refusal.
    await expect(page.locator("#detail-title")).toContainText("The Market");
});

test("says nothing when the selection has a node of its own", async ({ page }) => {
    await jumpFrom(page, "Trader: Apples.", "Dialogue AST");

    await expect(page.locator("#detail-title")).toContainText("Trader: Apples.");
    await expect(page.locator(".dd-arrival-note")).toHaveCount(0);
});

test("puts the note away once the reader gets on with the reading", async ({ page }) => {
    await jumpFrom(page, "| Rope | 5 |", "Dialogue AST");
    await expect(page.locator(".dd-arrival-note")).toBeVisible();

    await page.locator("g.node", { hasText: "Trader" }).first().locator("circle").click();

    await expect(page.locator(".dd-arrival-note")).toBeHidden();
});
