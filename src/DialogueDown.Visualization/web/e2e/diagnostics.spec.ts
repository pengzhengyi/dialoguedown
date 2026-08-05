import { test, expect } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";
import type { Report } from "../src/model";

// A duplicate-anchor error (DLG2001) on the second "# Chapter" heading (zero-based line 3).
const REPORT: Report = {
    source: "# Chapter\nAlice: Hello.\n\n# Chapter\nBob: Goodbye.\n",
    stages: SAMPLE_STAGES,
    diagnostics: [
        {
            range: { start: { line: 3, character: 0 }, end: { line: 3, character: 9 } },
            severity: 1,
            code: "DLG2001",
            message: "Two scenes resolve to the same anchor '#chapter'.",
            source: "dialoguedown",
        },
    ],
};

const url = writeReport(REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

test("renders a squiggle and a gutter marker for a diagnostic", async ({ page }) => {
    const active = page.locator("section.stage.active");
    await expect(active.locator(".cm-lintRange-error").first()).toBeVisible();
    await expect(active.locator(".cm-gutter-lint .cm-lint-marker-error")).toHaveCount(1);
});

test("hovering a diagnostic shows its message and a docs link", async ({ page }) => {
    const active = page.locator("section.stage.active");
    await active.locator(".cm-lintRange-error").first().hover();

    const tooltip = page.locator(".cm-tooltip-lint");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("Two scenes resolve to the same anchor '#chapter'.");

    const link = tooltip.locator("a.diagnostic-tooltip-link");
    await expect(link).toHaveAttribute("href", /guide\/error-codes\.html#dlg2001$/);
    await expect(link).toContainText("DLG2001");
});

test("the diagnostic tooltip escapes the Source pane without leaving the viewport", async ({
    page,
}) => {
    await page.setViewportSize({ width: 1506, height: 450 });
    await page
        .locator("section.stage.active .source-view")
        .evaluate((element) => element.style.setProperty("--source-split", "20%"));

    const sourcePane = page.locator("section.stage.active .source-pane");
    const preview = page.locator("section.stage.active .source-preview");
    await sourcePane.locator(".cm-lintRange-error").first().hover();

    const tooltip = page.locator(".cm-tooltip:has(.cm-tooltip-lint)");
    await expect(tooltip).toBeVisible();
    expect(
        await tooltip.evaluate((element) => element.parentElement?.parentElement === document.body),
    ).toBe(true);

    const tooltipBox = await tooltip.boundingBox();
    const sourcePaneBox = await sourcePane.boundingBox();
    const previewBox = await preview.boundingBox();
    expect(tooltipBox).not.toBeNull();
    expect(sourcePaneBox).not.toBeNull();
    expect(previewBox).not.toBeNull();
    expect(tooltipBox!.y).toBeGreaterThanOrEqual(0);
    expect(tooltipBox!.y + tooltipBox!.height).toBeLessThanOrEqual(450);
    expect(tooltipBox!.width).toBeLessThanOrEqual(360);
    expect(tooltipBox!.x + tooltipBox!.width).toBeGreaterThan(
        sourcePaneBox!.x + sourcePaneBox!.width,
    );
    expect(
        await tooltip.evaluate((element) => Number(getComputedStyle(element).zIndex)),
    ).toBeGreaterThanOrEqual(1000);
    expect(
        await tooltip.evaluate((element, previewBounds) => {
            const tooltipBounds = element.getBoundingClientRect();
            const overlapLeft = Math.max(tooltipBounds.left, previewBounds.x);
            const overlapRight = Math.min(
                tooltipBounds.right,
                previewBounds.x + previewBounds.width,
            );
            const overlapTop = Math.max(tooltipBounds.top, previewBounds.y);
            const overlapBottom = Math.min(
                tooltipBounds.bottom,
                previewBounds.y + previewBounds.height,
            );
            if (overlapLeft >= overlapRight || overlapTop >= overlapBottom) return false;

            const hit = document.elementFromPoint(
                (overlapLeft + overlapRight) / 2,
                (overlapTop + overlapBottom) / 2,
            );
            return hit === element || element.contains(hit);
        }, previewBox!),
    ).toBe(true);

    const message = tooltip.locator(".diagnostic-tooltip-message");
    expect(
        await message.evaluate((element) => {
            const range = document.createRange();
            range.selectNodeContents(element);
            return range.getClientRects().length;
        }),
    ).toBeGreaterThan(1);

    const link = tooltip.locator("a.diagnostic-tooltip-link");
    await expect(link).toBeVisible();
    expect(
        await link.evaluate((element) => {
            const bounds = element.getBoundingClientRect();
            const hit = document.elementFromPoint(
                bounds.left + bounds.width / 2,
                bounds.top + bounds.height / 2,
            );
            return hit === element || element.contains(hit);
        }),
    ).toBe(true);
});

test("a report with no diagnostics shows no overlay", async ({ page }) => {
    const cleanUrl = writeReport({ source: "# Scene\nAlice: Hi.\n", stages: SAMPLE_STAGES });
    await page.goto(cleanUrl);
    await expect(page.locator(".tab")).toHaveCount(2);

    const active = page.locator("section.stage.active");
    await expect(active.locator(".cm-lintRange-error")).toHaveCount(0);
    await expect(active.locator(".cm-lint-marker-error")).toHaveCount(0);
});
