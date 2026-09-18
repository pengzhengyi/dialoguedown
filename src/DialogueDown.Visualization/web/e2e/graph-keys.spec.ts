import { test, expect, type Page } from "@playwright/test";
import { writeReport } from "./report";
import type { Report, Stage } from "../src/model";

/** A syntax tree: its edges only say what contains what, so the ways in are not a list to number. */
const TREE_STAGE: Stage = {
    title: "Markdown AST",
    description: "The Markdown tree the front end parses.",
    nodes: [
        { id: "n0", label: "Document", attributes: [] },
        { id: "n1", label: "Heading", attributes: [] },
        { id: "n2", label: "Paragraph", attributes: [] },
        { id: "n3", label: "Text", attributes: [] },
    ],
    edges: [
        { fromId: "n0", toId: "n1", kind: "Child" },
        { fromId: "n0", toId: "n2", kind: "Child" },
        { fromId: "n2", toId: "n3", kind: "Child" },
    ],
    nests: true,
};

/** The flow graph: child edges span the flow, and a join gives two edges into one node. */
const GRAPH_STAGE: Stage = {
    title: "Dialogue Graph",
    description: "The compiled flow a runtime walks.",
    nodes: [
        { id: "n0", label: "Document", attributes: [] },
        { id: "n1", label: "Left", attributes: [] },
        { id: "n2", label: "Right", attributes: [] },
        { id: "n3", label: "End", attributes: [] },
    ],
    edges: [
        { fromId: "n0", toId: "n1", kind: "Child", category: "choice" },
        { fromId: "n0", toId: "n2", kind: "Child", category: "choice" },
        { fromId: "n1", toId: "n3", kind: "Child", category: "break" },
        { fromId: "n2", toId: "n3", kind: "Child", category: "break" },
    ],
    nests: false,
};

const REPORT: Report = {
    source: "# Document\n\nLeft\n\nRight\n",
    stages: [TREE_STAGE, GRAPH_STAGE],
};
const url = writeReport(REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(3); // Source + the tree + the graph
});

/** Open a stage tab and wait for its drawing. */
async function openStage(page: Page, title: string): Promise<void> {
    await page.locator(".tab", { hasText: title }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
}

/**
 * Press a navigation key. The tab button keeps focus after the click that opened it, and the
 * report leaves buttons their keys (Space would toggle one); blur it so the graph receives them.
 */
async function press(page: Page, key: string): Promise<void> {
    await page.evaluate(() => (document.activeElement as HTMLElement | null)?.blur());
    await page.keyboard.press(key);
}

const detailTitle = (page: Page) => page.locator("#detail-title");

test.describe("the Dialogue Graph's keymap", () => {
    test("takes the nth way in with Shift, and the nth way out with a digit", async ({ page }) => {
        await openStage(page, "Dialogue Graph");

        await press(page, "ArrowDown"); // the root, Document
        await press(page, "Digit1"); // its first way out
        await expect(detailTitle(page)).toContainText("Left");
        await press(page, "ArrowRight"); // Left's first way out, the join
        await expect(detailTitle(page)).toContainText("End");

        // Two edges lead to the End; the second is Right. Shift makes the key report `@`.
        await press(page, "Shift+Digit2");

        await expect(detailTitle(page)).toContainText("Right");
    });
});

test.describe("a tree stage's keymap", () => {
    test("takes the nth child with a digit", async ({ page }) => {
        await openStage(page, "Markdown AST");

        await press(page, "ArrowDown"); // the root, Document
        await press(page, "Digit2"); // its second child

        await expect(detailTitle(page)).toContainText("Paragraph");
    });

    test("leaves Shift+digit alone, even where two edges lead in", async ({ page }) => {
        await openStage(page, "Markdown AST");
        await press(page, "ArrowDown");
        await press(page, "Digit2");
        await expect(detailTitle(page)).toContainText("Paragraph");

        await press(page, "Shift+Digit1");

        await expect(detailTitle(page)).toContainText("Paragraph");
    });
});

test.describe("the node inspector's index column", () => {
    test("reads `#` in full, and offers no ellipsis on either table", async ({ page }) => {
        await openStage(page, "Dialogue Graph");
        await press(page, "ArrowDown"); // the root
        await press(page, "Digit1"); // Left
        await press(page, "ArrowRight"); // End: two ways in, none out
        await expect(detailTitle(page)).toContainText("End");

        const headers = page.locator("#detail-body table.neighbors thead th.neighbor-index");
        await expect(headers).toHaveCount(2);
        for (const header of await headers.all()) await expect(header).toHaveText("#");

        // The glyph fits its box: a header whose content overflows is the `#…` this guards against.
        const overflowing = await headers.evaluateAll((cells) =>
            cells.map((cell) => cell.scrollWidth > cell.clientWidth),
        );
        expect(overflowing).toEqual([false, false]);
    });
});

test.describe("the footer help", () => {
    test("describes a tree stage's keys without the graph's ways in", async ({ page }) => {
        await openStage(page, "Markdown AST");
        await page.locator("#help-toggle").click();

        const help = page.locator("#help-content");
        await expect(help).toContainText("first child");
        await expect(help).not.toContainText("Shift");
    });

    test("describes the Dialogue Graph's full keymap, ways in included", async ({ page }) => {
        await openStage(page, "Dialogue Graph");
        await page.locator("#help-toggle").click();

        const help = page.locator("#help-content");
        await expect(help).toContainText("first way out");
        await expect(help).toContainText("Shift");
    });
});
