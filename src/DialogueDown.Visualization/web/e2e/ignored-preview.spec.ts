import AxeBuilder from "@axe-core/playwright";
import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { writeReport } from "./report";

const source = [
    "# Ignored regions",
    "",
    "| A | B |",
    "| - | - |",
    "| x | y |",
    "",
    "Alice: Visit <https://example.com/road> before sundown.",
    "",
    "---",
    "",
].join("\n");

const autolinkLine = 6;
const autolinkStart = source.split("\n")[autolinkLine].indexOf("<https");

const report: Report = {
    source,
    stages: [],
    semanticTokens: [
        {
            kind: "IgnoredMarkdown",
            range: { start: { line: 2, character: 0 }, end: { line: 4, character: 9 } },
        },
        {
            kind: "IgnoredMarkdown",
            range: {
                start: { line: autolinkLine, character: autolinkStart },
                end: { line: autolinkLine, character: autolinkStart + 26 },
            },
        },
        {
            kind: "IgnoredMarkdown",
            range: { start: { line: 8, character: 0 }, end: { line: 8, character: 3 } },
        },
    ],
};

test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => localStorage.removeItem("dd-ignored-preview-collapsed"));
    await page.goto(writeReport(report));
    await expect(page.locator(".source-preview .dd-preview-ignored-region")).toHaveCount(3);
});

function footer(page: import("@playwright/test").Page) {
    return page.locator(".dd-ignored-preview-footer");
}

function regions(page: import("@playwright/test").Page) {
    return page.locator(".source-preview .dd-preview-ignored-region");
}

function hidden(page: import("@playwright/test").Page) {
    return page.locator(".source-preview .dd-preview-ignored-region.dd-ignored-region-hidden");
}

test("hides one region from its own marker, leaving the others shown", async ({ page }) => {
    await expect(footer(page)).toContainText("3 ignored");
    await expect(footer(page)).toContainText("all shown in Preview");

    const table = regions(page).first();
    await table.locator(".dd-ignored-region-toggle").click();

    await expect(hidden(page)).toHaveCount(1);
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");
    await expect(table.locator(".dd-preview-ignored")).toBeHidden();
    await expect(table.locator(".dd-ignored-region-toggle")).toHaveAttribute(
        "aria-expanded",
        "false",
    );
});

test("reaches a region's control from the keyboard", async ({ page }) => {
    const control = regions(page).nth(2).locator(".dd-ignored-region-toggle");

    await control.focus();
    await expect(control).toBeFocused();
    await page.keyboard.press("Enter");

    await expect(regions(page).nth(2)).toHaveClass(/dd-ignored-region-hidden/);
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");
});

test("a global command overrides every individual choice", async ({ page }) => {
    await regions(page).first().locator(".dd-ignored-region-toggle").click();
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");

    await footer(page).getByRole("button", { name: "Hide all ignored content in Preview" }).click();
    await expect(hidden(page)).toHaveCount(3);
    await expect(footer(page)).toContainText("all hidden in Preview");

    // The region that was individually hidden must not survive as an exception to "show all".
    await regions(page).nth(1).locator(".dd-ignored-region-toggle").click();
    await footer(page).getByRole("button", { name: "Show all ignored content in Preview" }).click();

    await expect(hidden(page)).toHaveCount(0);
    await expect(footer(page)).toContainText("all shown in Preview");
});

test("keeps a hidden inline region as a chip inside its sentence", async ({ page }) => {
    const inline = page.locator(".source-preview .dd-preview-ignored-region-inline");

    await inline.locator(".dd-ignored-region-toggle").click();

    await expect(inline).toHaveClass(/dd-ignored-region-hidden/);
    await expect(inline).toHaveAttribute("title", "Ignored autolink: <https://example.com/road>");
    const sentence = page.locator(".source-preview p", { hasText: "before sundown" });
    await expect(sentence).toContainText("Alice: Visit");
    await expect(sentence).toContainText("before sundown.");
});

test("leaves Source untouched whatever the Preview shows", async ({ page }) => {
    await footer(page).getByRole("button", { name: "Hide all ignored content in Preview" }).click();

    await expect(page.locator(".source-pane .dd-tok-ignored-markdown").first()).toBeVisible();
    await expect(page.locator(".source-pane .cm-content")).toContainText("| x | y |");
});

test("has no accessibility violations in a mixed view", async ({ page }) => {
    await regions(page).first().locator(".dd-ignored-region-toggle").click();
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");

    const analyze = () => new AxeBuilder({ page }).include(".source-preview-shell").analyze();
    expect((await analyze()).violations).toEqual([]);

    await page.locator(".theme-select").selectOption("dark");
    expect((await analyze()).violations).toEqual([]);
});
