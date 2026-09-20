import { test, expect } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import { QUICK_FIX_PORT, QUICK_FIX_DOC, QUICK_FIX_SOURCE } from "./fixture.mjs";

// The compiler's quick fix end-to-end against the real .NET --live server: the dangling
// arrow's warning offers the compiler's "Escape as literal text" fix, applying it inserts the
// backslash, the save writes it to disk, and the recompile clears the warning.
const base = `http://127.0.0.1:${QUICK_FIX_PORT}`;

test.beforeEach(async ({ page }) => {
    writeFileSync(QUICK_FIX_DOC, QUICK_FIX_SOURCE);
    // Pin Source to Manual so the explicit Save below owns the write, not an idle autosave.
    await page.context().addCookies([{ name: "dd-save-mode-source", value: "manual", url: base }]);
});

test("applying the dangling-arrow fix escapes the arrow and clears the warning", async ({
    page,
}) => {
    await page.goto(`${base}/`);

    const pane = page.locator(".source-pane");
    await expect(pane.locator(".cm-lint-marker-warning")).toHaveCount(1);

    // The warning's tooltip offers the compiler's fix.
    await pane.locator(".cm-lintRange-warning").first().hover();
    const action = page.locator(".cm-tooltip-lint button.cm-diagnosticAction", {
        hasText: "Escape as literal text",
    });
    await expect(action).toBeVisible();
    await action.click();

    // The fix edited the buffer but has not written the file yet.
    await expect(pane.locator(".cm-content")).toContainText("\\=>");
    expect(readFileSync(QUICK_FIX_DOC, "utf8")).not.toContain("\\=>");

    // Saving writes the escape; the recompile clears the warning.
    await page.locator(".save-button").click();
    await expect(page.locator(".save-status[data-status='saved']")).toBeVisible();
    await expect(pane.locator(".cm-lint-marker-warning")).toHaveCount(0);
    await expect.poll(() => readFileSync(QUICK_FIX_DOC, "utf8")).toContain("\\=>");
});
