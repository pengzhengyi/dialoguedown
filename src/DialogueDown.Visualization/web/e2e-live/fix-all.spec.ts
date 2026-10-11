import { test, expect } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import { FIX_ALL_PORT, FIX_ALL_DOC, FIX_ALL_SOURCE } from "./fixture.mjs";

// Fix all end-to-end against the real .NET --edit server: a line with two dangling arrows offers
// two of the compiler's fixes, one "Fix all" applies both, and one undo reverts the batch.
const base = `http://127.0.0.1:${FIX_ALL_PORT}`;

test.beforeEach(async ({ page }) => {
    writeFileSync(FIX_ALL_DOC, FIX_ALL_SOURCE);
    // Pin Source to Manual so the explicit Save below owns the write, not an idle autosave.
    await page.context().addCookies([{ name: "dd-save-mode-source", value: "manual", url: base }]);
});

test("Fix all escapes every dangling arrow in one edit, which one undo reverts", async ({
    page,
}) => {
    await page.goto(`${base}/`);

    // Both dangling arrows are listed as problems, and the batch action counts them.
    await page.locator(".status-bar .diagnostic-summary").click();
    const panel = page.locator('[data-body="problems"]');
    const fixAll = panel.locator(".problem-fix-all");
    await expect(panel.locator(".problem-row")).toHaveCount(2);
    await expect(fixAll).toHaveText("Fix all (2)");

    await fixAll.click();

    // Both arrows are escaped in one edit; the file is untouched until Save.
    const content = page.locator(".source-pane .cm-content");
    await expect(content).toContainText("x \\=> y and z \\=> w");
    await expect(page.locator(".toast")).toContainText("Fixed 2 problems.");
    expect(readFileSync(FIX_ALL_DOC, "utf8")).not.toContain("\\=>");

    // One undo reverts both edits, proving the batch is a single transaction.
    await content.click();
    await page.keyboard.press("ControlOrMeta+z");
    await expect(content).toContainText("x => y and z => w");

    // Apply again and save; the recompile clears both problems and writes the escapes.
    await fixAll.click();
    await page.locator(".save-button").click();
    await expect(page.locator(".save-status[data-status='saved']")).toBeVisible();
    await expect(panel.locator(".problem-row")).toHaveCount(0);
    await expect.poll(() => readFileSync(FIX_ALL_DOC, "utf8")).toContain("\\=>");
});
