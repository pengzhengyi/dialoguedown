import AxeBuilder from "@axe-core/playwright";
import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { writeReport } from "./report";

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

function controls(page: import("@playwright/test").Page) {
    return page.locator(".source-pane .dd-source-ignored-toggle");
}

function editorLines(page: import("@playwright/test").Page) {
    return page.locator(".source-pane .cm-line");
}

test("offers a control on every ignored region the compiler reported", async ({ page }) => {
    await expect(controls(page)).toHaveCount(3);
    await expect(controls(page).first()).toHaveAttribute("aria-expanded", "true");
});

test("folds one ignored block to a summary the writer can still see", async ({ page }) => {
    const before = await editorLines(page).count();
    await expect(page.locator(".source-pane .cm-content")).toContainText("| Rope | 5 |");

    await controls(page).first().click();

    // The table's own lines are gone, replaced by one row naming what was put away.
    await expect(page.locator(".source-pane .cm-content")).not.toContainText("| Rope | 5 |");
    expect(await editorLines(page).count()).toBeLessThan(before);
    await expect(page.locator(".dd-source-ignored-summary")).toHaveText("Ignored · 3 lines");
    await expect(controls(page).first()).toHaveAttribute("aria-expanded", "false");
});

test("leaves the Preview alone, because the two panes fold their own state", async ({ page }) => {
    await controls(page).first().click();

    // The Preview still shows the table it renders; only the editor put its lines away.
    await expect(page.locator(".source-preview table")).toBeVisible();
    await expect(page.locator(".dd-ignored-preview-footer")).toContainText("all shown in Preview");
});

test("folds and opens every ignored region from the keyboard", async ({ page }) => {
    const before = await editorLines(page).count();
    await page.locator(".source-pane .cm-content").click();

    await page.keyboard.press("Alt+i");
    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(3);
    expect(await editorLines(page).count()).toBeLessThan(before);

    await page.keyboard.press("Alt+o");
    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(0);
    await expect(editorLines(page)).toHaveCount(before);
});

test("offers the same pair in the editor's own menu", async ({ page }) => {
    await editorLines(page).first().click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Fold all ignored Markdown" }).click();

    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(3);

    await editorLines(page).first().click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Open all ignored Markdown" }).click();

    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(0);
});

test("a command discards the choices made region by region", async ({ page }) => {
    await controls(page).first().click();
    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(1);

    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Alt+o");

    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(0);
});

test("has no accessibility violations with regions folded", async ({ page }) => {
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Alt+i");
    await expect(page.locator(".dd-source-ignored-folded")).toHaveCount(3);

    const analyze = () => new AxeBuilder({ page }).include(".source-pane").analyze();
    expect((await analyze()).violations).toEqual([]);

    await page.locator(".theme-select").selectOption("dark");
    expect((await analyze()).violations).toEqual([]);
});
