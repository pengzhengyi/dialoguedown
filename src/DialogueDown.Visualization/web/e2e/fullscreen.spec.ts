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

test("Zen mode leaves the Source editor alone, and restores the preview on exit", async ({
    page,
}) => {
    const preview = page.locator("section.stage.active .source-preview");
    const editor = page.locator("section.stage.active .source-pane .cm-editor");
    await expect(preview).toBeVisible();

    await page.keyboard.press("z");

    // Chrome is hidden like full screen, and the preview steps aside so the editor is alone.
    await expect(page.locator("body")).toHaveClass(/zen/);
    await expect(page.locator(".app-header")).toBeHidden();
    await expect(preview).toBeHidden();
    await expect(editor).toBeVisible();

    // There is always a visible way out, since the header is gone.
    await expect(page.locator(".maximize-exit")).toBeVisible();

    await page.keyboard.press("z");
    await expect(page.locator("body")).not.toHaveClass(/zen/);
    await expect(preview).toBeVisible();
});

test("Zen mode leaves the graph alone by hiding the details panel", async ({ page }) => {
    await showAst(page);
    await expect(page.locator("#detail")).toBeVisible();

    await page.keyboard.press("z");

    await expect(page.locator("#detail")).toBeHidden();
    // The graph's own instruments stay — Zen removes panels, not the content's tools.
    await expect(page.locator("section.stage.active .legend")).toBeVisible();
    await expect(page.locator("section.stage.active .zoom-controls")).toBeVisible();

    await page.keyboard.press("Escape");
    await expect(page.locator("#detail")).toBeVisible();
});

test("Zen mode does not overwrite a reader's own collapse choice", async ({ page }) => {
    const view = page.locator("section.stage.active.source-stage .source-view");
    const preview = view.locator(".source-preview");

    // The reader deliberately hides the preview, then uses Zen and leaves it.
    await view.locator(".collapse-toggle").click();
    await expect(view).toHaveClass(/preview-collapsed/);

    await page.keyboard.press("z");
    await expect(preview).toBeHidden();
    await page.keyboard.press("z");

    // Their choice survived Zen: still collapsed, not silently restored.
    await expect(view).toHaveClass(/preview-collapsed/);
    await expect(preview).toBeHidden();
});

test("`z` is ignored while typing, and Zen deepens an existing full screen", async ({ page }) => {
    // Typing in the editor must insert a `z`, never toggle the mode.
    await page.locator("section.stage.active .cm-content").click();
    await page.keyboard.press("z");
    await expect(page.locator("body")).not.toHaveClass(/zen/);

    // From full screen, `z` deepens into Zen; `f` then leaves focus mode entirely.
    await page.locator("body").click({ position: { x: 5, y: 5 } });
    await page.keyboard.press("f");
    await expect(page.locator("body")).toHaveClass(/maximized/);
    await page.keyboard.press("z");
    await expect(page.locator("body")).toHaveClass(/zen/);
    await page.keyboard.press("f");
    await expect(page.locator("body")).not.toHaveClass(/maximized/);
    await expect(page.locator("body")).not.toHaveClass(/zen/);
});

test("the tab-bar Zen button enters and leaves Zen", async ({ page }) => {
    const zen = page.locator(".tabbar-zen");
    const preview = page.locator("section.stage.active .source-preview");
    await expect(zen).toBeVisible();
    await expect(zen).toHaveAttribute("aria-pressed", "false");

    await zen.click();

    await expect(page.locator("body")).toHaveClass(/zen/);
    await expect(preview).toBeHidden();
    // The tab row is hidden in Zen, so the corner chip is the visible way back.
    await expect(page.locator(".maximize-exit")).toBeVisible();

    await page.locator(".maximize-exit").click();
    await expect(page.locator("body")).not.toHaveClass(/zen/);
    await expect(zen).toHaveAttribute("aria-pressed", "false");
    await expect(preview).toBeVisible();
});
