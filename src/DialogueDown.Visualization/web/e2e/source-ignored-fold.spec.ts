import AxeBuilder from "@axe-core/playwright";
import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { writeReport } from "./report";
import { selectTheme } from "./theme";

const source = [
    "# Market",
    "",
    "Trader: Apples.",
    "",
    "| Item | Cost |",
    "| --- | --- |",
    "| Rope | 5 |",
    "",
    "Alice: see <https://example.com> now.",
    "",
    "---",
    "",
    "Bob: onwards.",
    "",
].join("\n");

const lines = source.split("\n");
const autolinkStart = lines[8].indexOf("<https");
const autolinkLength = "<https://example.com>".length;

const report: Report = {
    source,
    stages: [],
    semanticTokens: [
        {
            kind: "IgnoredMarkdown",
            range: {
                start: { line: 4, character: 0 },
                end: { line: 6, character: lines[6].length },
            },
        },
        {
            kind: "IgnoredMarkdown",
            range: {
                start: { line: 8, character: autolinkStart },
                end: { line: 8, character: autolinkStart + autolinkLength },
            },
        },
        {
            kind: "IgnoredMarkdown",
            range: { start: { line: 10, character: 0 }, end: { line: 10, character: 3 } },
        },
    ],
};

test.beforeEach(async ({ page }) => {
    await page.goto(writeReport(report));
    await expect(page.locator(".source-pane .cm-content")).toBeVisible();
});

function editorLines(page: import("@playwright/test").Page) {
    return page.locator(".source-pane .cm-line");
}

/** Press the gutter chevron beside the line an ignored run starts on. */
async function foldFromGutter(page: import("@playwright/test").Page, text: string): Promise<void> {
    const line = await page
        .locator(".source-pane .cm-line", { hasText: text })
        .first()
        .boundingBox();
    if (!line) throw new Error(`Could not find the line starting "${text}".`);

    const markers = page.locator('.source-pane .cm-fold-marker[title="Fold line"]');
    for (const marker of await markers.all()) {
        const box = await marker.boundingBox();
        if (box && Math.abs(box.y - line.y) < line.height) {
            await marker.click();
            return;
        }
    }
    throw new Error(`No fold chevron sits beside "${text}".`);
}

test("marks ignored content without giving it a second way to close", async ({ page }) => {
    // The cue says "ignored", so it marks the table and the lone divider alike -- but not the
    // autolink inside a sentence, where it would read as a stray character.
    await expect(page.locator(".dd-source-ignored-cue")).toHaveCount(2);
    await expect(
        editorLines(page).filter({ hasText: "see" }).locator(".dd-source-ignored-cue"),
    ).toHaveCount(0);

    // Folding stays the editor's own gesture, so no region grows a control of its own.
    await expect(page.locator(".dd-source-ignored-toggle")).toHaveCount(0);
});

test("marks the divider it cannot fold, because dim ink alone barely shows on three dashes", async ({
    page,
}) => {
    const divider = editorLines(page).filter({ hasText: "---" }).last();

    await expect(divider.locator(".dd-source-ignored-cue")).toHaveCount(1);
    await expect(async () => {
        await foldFromGutter(page, "---");
    }).rejects.toThrow();
});

test("folds an ignored run from the editor's own gutter", async ({ page }) => {
    const before = await editorLines(page).count();
    await expect(page.locator(".source-pane .cm-content")).toContainText("| Rope | 5 |");

    await foldFromGutter(page, "| Item");

    await expect(page.locator(".source-pane .cm-content")).not.toContainText("| Rope | 5 |");
    expect(await editorLines(page).count()).toBeLessThan(before);
    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(1);
});

test("leaves the Preview alone, because the two panes fold their own state", async ({ page }) => {
    await foldFromGutter(page, "| Item");

    await expect(page.locator(".source-preview table")).toBeVisible();
    await expect(page.locator(".dd-ignored-preview-footer")).toContainText("all shown in Preview");
});

test("folds and opens every ignored run from the keyboard", async ({ page }) => {
    const before = await editorLines(page).count();
    await page.locator(".source-pane .cm-content").click();

    await page.keyboard.press("Alt+i");
    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(1);
    expect(await editorLines(page).count()).toBeLessThan(before);

    await page.keyboard.press("Alt+o");
    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(0);
    await expect(editorLines(page)).toHaveCount(before);
});

test("offers the same pair in the editor's own menu", async ({ page }) => {
    await editorLines(page).first().click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Fold all ignored Markdown" }).click();

    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(1);

    await editorLines(page).first().click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Open all ignored Markdown" }).click();

    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(0);
});

test("opens a run the command folded, however it was folded", async ({ page }) => {
    await foldFromGutter(page, "| Item");
    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(1);

    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Alt+o");

    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(0);
});

test("has no accessibility violations with ignored runs folded", async ({ page }) => {
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Alt+i");
    await expect(page.locator(".source-pane .cm-foldPlaceholder")).toHaveCount(1);

    const analyze = () => new AxeBuilder({ page }).include(".source-pane").analyze();
    expect((await analyze()).violations).toEqual([]);

    await selectTheme(page, "dark");
    expect((await analyze()).violations).toEqual([]);
});
