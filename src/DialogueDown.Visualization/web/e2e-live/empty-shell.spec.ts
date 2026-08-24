import { test, expect } from "@playwright/test";
import { rmSync } from "node:fs";
import { join } from "node:path";
import { SHELL_PORT, SHELL_TREE } from "./fixture.mjs";

// Targets the empty shell (visualize --root <tree>, no source): the served report
// lands with no document open, showing the Explorer over the project and a "No
// script open" call to action. The spec browses the tree, opens scripts into the
// report under /r/, and creates a script — all through the Explorer, against the
// real .NET server started by serve-shell.mjs.
const base = `http://127.0.0.1:${SHELL_PORT}`;

test.beforeEach(async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".empty-shell-title")).toHaveText("No script open");
});

test("lists the root's scripts and folders in the Explorer", async ({ page }) => {
    // Open on arrival here, unlike a session with a document: nothing is showing, so the tree is
    // not a detour but the only thing to do — the card beside it points straight at it.
    await expect(page.locator("#explorer")).toBeVisible();
    await expect(page.locator(".tabbar-explorer")).toHaveAttribute("aria-expanded", "true");
    await expect(
        page.locator(".explorer-script-row", { hasText: "top.dialogue.md" }),
    ).toBeVisible();
    await expect(page.locator(".explorer-folder-row", { hasText: "sub" })).toBeVisible();
});

test("opens a root script into the report in View", async ({ page }) => {
    await page.locator(".explorer-script-row", { hasText: "top.dialogue.md" }).click();

    // Opening navigates into the report under /r/ in the shell server's default View mode.
    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".tab")).toHaveCount(8); // Config + Source + Markdown/Dialogue/Desugared AST + Semantic Model + Dialogue Graph + Playbook
    await expect(page.locator(".tab").first()).toHaveText("Config");
    await expect(page.locator(".tab.active")).toHaveText("Source");
});

test("browses into a sub-folder and opens a nested script", async ({ page }) => {
    await page.locator(".explorer-folder-row", { hasText: "sub" }).click();
    const nested = page.locator(".explorer-script-row", { hasText: "nested.dialogue.md" });
    await expect(nested).toBeVisible();

    await nested.click();
    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".tab")).toHaveCount(8);
});

// The create tests write into the shell tree; remove the file afterward so a rerun and the
// other tests see the base fixture. (The "exists" test opens an existing script, so nothing
// is created and force:true makes the removal a no-op.)
const createdInTest = join(SHELL_TREE, "created-in-test.dialogue.md");
test.afterEach(() => rmSync(createdInTest, { force: true }));

test("creates a new script from the call to action and opens it in Edit", async ({ page }) => {
    await page.locator(".empty-shell-create").click();
    const name = page.locator(".explorer-create-name");
    await name.fill("created-in-test");
    await name.press("Enter");

    // A freshly created (empty) script always opens in Edit so the writer can start typing.
    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator('.mode-toggle-option[data-mode="edit"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();
});

test("an existing name offers to open it instead of overwriting", async ({ page }) => {
    page.once("dialog", (dialog) => void dialog.accept()); // "open it instead?"
    await page.locator(".empty-shell-create").click();
    const name = page.locator(".explorer-create-name");
    await name.fill("top"); // top.dialogue.md already exists
    await name.press("Enter");

    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".source-pane .cm-editor")).toContainText("Top Scene");
});
