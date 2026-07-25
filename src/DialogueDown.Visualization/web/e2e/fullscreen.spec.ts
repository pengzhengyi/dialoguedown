import { test, expect, type Page } from "@playwright/test";
import { SAMPLE_REPORT, writeReport } from "./report";

const url = writeReport(SAMPLE_REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

/** Switch to the Markdown AST graph tab and wait for it to render. */
async function showAst(page: Page): Promise<void> {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
}

test("the tab-bar control fills the window and hides the chrome", async ({ page }) => {
    await showAst(page);
    await expect(page.locator(".app-header")).toBeVisible();
    // The maximize control is app-level (one button at the end of the tab row), not per-pane.
    await expect(page.locator(".zoom-controls .maximize-button")).toHaveCount(0);

    await page.locator(".tabbar-maximize").click();
    await expect(page.locator("body")).toHaveClass(/maximized/);
    await expect(page.locator(".app-header")).toBeHidden();
    await expect(page.locator(".app-footer")).toBeHidden();

    // The tab-bar button hides with the chrome; the floating exit chip takes over.
    const exit = page.locator(".maximize-exit");
    await expect(exit).toBeVisible();
    await exit.click();
    await expect(page.locator("body")).not.toHaveClass(/maximized/);
    await expect(page.locator(".app-header")).toBeVisible();
    await expect(exit).toBeHidden();
});

test("the f key toggles full screen and Escape leaves it", async ({ page }) => {
    await showAst(page);
    // Focus a non-editable element (the tab button) so `f` is a shortcut, not text entry.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();

    await page.keyboard.press("f");
    await expect(page.locator("body")).toHaveClass(/maximized/);

    await page.keyboard.press("Escape");
    await expect(page.locator("body")).not.toHaveClass(/maximized/);

    await page.keyboard.press("f");
    await expect(page.locator("body")).toHaveClass(/maximized/);
    await page.keyboard.press("f");
    await expect(page.locator("body")).not.toHaveClass(/maximized/);
});

test("the single tab-bar control also maximizes from the Source tab", async ({ page }) => {
    // No per-pane control remains on the Source tab.
    await expect(page.locator(".source-controls")).toHaveCount(0);

    await page.locator(".tabbar-maximize").click();
    await expect(page.locator("body")).toHaveClass(/maximized/);
    await expect(page.locator(".app-header")).toBeHidden();

    await page.locator(".maximize-exit").click();
    await expect(page.locator("body")).not.toHaveClass(/maximized/);
});

test("typing f inside the editor does not toggle full screen", async ({ page }) => {
    await page.locator("section.stage.active.source-stage .cm-content").click();
    await page.keyboard.press("f");
    await expect(page.locator("body")).not.toHaveClass(/maximized/);
});
