import { test, expect, type Page } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import {
    SWITCH_PORT,
    SWITCH_FIRST_DOC,
    SWITCH_SECOND_DOC,
    SWITCH_FIRST_SOURCE,
    SWITCH_SECOND_SOURCE,
} from "./fixture.mjs";

// Opening a script from the Explorer replaces the report's contents, not the page. This spec
// proves that end to end against the real .NET server: the reader keeps the window they were
// working in, the report follows the script they opened, and their unsaved work is still theirs.
const base = `http://127.0.0.1:${SWITCH_PORT}`;

/** A value on `window` survives a switch and dies on a page load — the difference under test. */
const MARK = "__dd_same_page__";

test.beforeEach(async ({ page }) => {
    // Each test starts from the first script, whichever one the previous test left open.
    await page.request.post(`${base}/api/open`, {
        data: { source: "first.dialogue.md", mode: "edit" },
    });
    await page.goto(`${base}/r/`);
    await expect(page.locator(".cm-content").first()).toContainText("First Scene");
    await page.evaluate(
        (mark) => ((window as never as Record<string, string>)[mark] = "yes"),
        MARK,
    );
    await openExplorer(page);
});

test.afterEach(() => {
    writeFileSync(SWITCH_FIRST_DOC, SWITCH_FIRST_SOURCE);
    writeFileSync(SWITCH_SECOND_DOC, SWITCH_SECOND_SOURCE);
});

/** Reveal the tree and expand the sub-folder, so both scripts are one click away. */
async function openExplorer(page: Page): Promise<void> {
    const explorer = page.locator("#explorer");
    if (!(await explorer.isVisible())) await page.locator(".tabbar-explorer").click();
    await expect(explorer).toBeVisible();
    const folder = page.locator(".explorer-folder", { hasText: "act" }).first();
    if (!(await folder.evaluate((el) => el.classList.contains("expanded")))) {
        await page.locator(".explorer-folder-row", { hasText: "act" }).click();
    }
    await expect(page.locator(".explorer-script-row", { hasText: "second" })).toBeVisible();
}

/** Whether the page the test marked is still the page on screen. */
const samePage = (page: Page): Promise<boolean> =>
    page.evaluate((mark) => (window as never as Record<string, string>)[mark] === "yes", MARK);

const openSecond = async (page: Page): Promise<void> => {
    await page.locator(".explorer-script-row", { hasText: "second" }).click();
    await expect(page.locator(".cm-content").first()).toContainText("Second Scene");
};

test("opens a script from the Explorer without reloading the page", async ({ page }) => {
    await openSecond(page);

    expect(await samePage(page)).toBe(true);
    await expect(page.locator(".cm-content").first()).toContainText("The script in a sub-folder");
});

test("re-points the report's identity at the opened script", async ({ page }) => {
    await openSecond(page);

    await expect(page).toHaveURL(/\/r\/act\//);
    await expect(page.locator(".explorer-script.active .explorer-script-row")).toHaveText(/second/);
    await expect(page.locator("#doc-path")).toContainText("second.dialogue.md");
});

test("keeps the reader's open tab across a switch", async ({ page }) => {
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Dialogue Graph");

    await page.locator(".explorer-script-row", { hasText: "second" }).click();
    await expect(page.locator(".explorer-script.active .explorer-script-row")).toHaveText(/second/);

    // Staying put is the point of the change: a reader comparing two graphs is not sent back
    // to Source on every click.
    await expect(page.locator(".tab.active")).toHaveText("Dialogue Graph");
    expect(await samePage(page)).toBe(true);
});

test("adopts the opened script in Edit rather than reporting a conflict", async ({ page }) => {
    await openSecond(page);

    // A file changing underneath the reader is a conflict; a script they opened is not.
    await expect(page.locator(".save-status")).toHaveText(/Saved/);
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
});

test("a save after a switch writes the script on screen", async ({ page }) => {
    await openSecond(page);

    await page.locator(".cm-content").first().click();
    await page.keyboard.press("ControlOrMeta+End");
    await page.keyboard.type("\nBob: Added after the switch.\n");
    await page.keyboard.press("ControlOrMeta+s");
    await expect(page.locator(".save-status")).toHaveText(/Saved/);

    expect(readFileSync(SWITCH_SECOND_DOC, "utf8")).toContain("Added after the switch");
    expect(readFileSync(SWITCH_FIRST_DOC, "utf8")).not.toContain("Added after the switch");
});

test("undo after a switch cannot reach the script left behind", async ({ page }) => {
    await openSecond(page);

    await page.locator(".cm-content").first().click();
    await page.keyboard.press("ControlOrMeta+z");

    // Undoing into the previous script would leave its text in this buffer, and the next save
    // would write it to the wrong file.
    await expect(page.locator(".cm-content").first()).toContainText("Second Scene");
    await expect(page.locator(".cm-content").first()).not.toContainText("First Scene");
});

test("Back returns to the previous script, and Forward returns again", async ({ page }) => {
    await openSecond(page);

    await page.goBack();
    await expect(page.locator(".cm-content").first()).toContainText("First Scene");
    await expect(page.locator(".explorer-script.active .explorer-script-row")).toHaveText(/first/);
    expect(await samePage(page)).toBe(true);

    await page.goForward();
    await expect(page.locator(".cm-content").first()).toContainText("Second Scene");
    expect(await samePage(page)).toBe(true);
});

test("Back lands in View, because it is a navigation and not an intent to edit", async ({
    page,
}) => {
    await openSecond(page);
    await page.goBack();

    await expect(page.locator(".cm-content").first()).toContainText("First Scene");
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "view");
});

test("reloading after a switch shows the script the address bar names", async ({ page }) => {
    await openSecond(page);

    await page.reload();

    await expect(page.locator(".cm-content").first()).toContainText("Second Scene");
    // A reload really is a reload — the marked page is gone, so this is not a false positive.
    expect(await samePage(page)).toBe(false);
});

test("keeps the reader in place when they decline to discard unsaved work", async ({ page }) => {
    await page.locator(".save-mode-option", { hasText: "Manual" }).first().click();
    await page.locator(".cm-content").first().click();
    await page.keyboard.type("\nAlice: An unsaved line.\n");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    page.once("dialog", (dialog) => dialog.dismiss());
    await page.locator(".explorer-script-row", { hasText: "second" }).click();

    await expect(page.locator(".cm-content").first()).toContainText("First Scene");
    await expect(page).not.toHaveURL(/\/r\/act\//);
});

test("hot reload keeps working on the newly opened script", async ({ page }) => {
    await openSecond(page);
    // Leaving Edit means the session re-syncs from disk rather than guarding a buffer.
    await page.locator('.mode-toggle-option[data-mode="view"]').click();
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "view");

    writeFileSync(SWITCH_SECOND_DOC, `${SWITCH_SECOND_SOURCE}\nBob: Changed on disk.\n`);

    await expect(page.locator(".cm-content").first()).toContainText("Changed on disk");
    expect(await samePage(page)).toBe(true);
});
