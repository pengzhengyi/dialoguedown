import { test, expect } from "@playwright/test";
import { rmSync } from "node:fs";
import { join } from "node:path";
import { LIVE_PORT, SHELL_PORT, SHELL_TREE } from "./fixture.mjs";

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
    // Creating is Edit's half of the shell, so the reader says so first.
    await page.locator('.mode-toggle-option[data-mode="edit"]').click();
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
    await page.locator('.mode-toggle-option[data-mode="edit"]').click(); // creating is Edit's
    await page.locator(".empty-shell-create").click();
    const name = page.locator(".explorer-create-name");
    await name.fill("top"); // top.dialogue.md already exists
    await name.press("Enter");

    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".source-pane .cm-editor")).toContainText("Top Scene");
});

// The Files control sits in two rows — the shell's, with no tabs, and a session's, with them —
// and it has to stand at the same height in both: a control that jumps when a script opens reads
// as a different control. The shell's row is the session's height even while it holds no tabs.
test("seats the Files control at the same height as a session does", async ({ page }) => {
    const shellBox = await page.locator(".tabbar-explorer .tab-icon").boundingBox();

    await page.goto(`http://127.0.0.1:${LIVE_PORT}/`);
    await expect(page.locator(".tab").first()).toBeVisible();
    const sessionBox = await page.locator(".tabbar-explorer .tab-icon").boundingBox();

    expect(shellBox?.y ?? 0).toBeCloseTo(sessionBox?.y ?? -1, 0);
});

// A session's status line carries the way back to the file selector, beside the path it leaves.
// This server pins a document and redirects `/` to it, so the link is also the proof that the
// shell has a door of its own.
test("carries a way back to the file selector from a session", async ({ page }) => {
    await page.goto(`http://127.0.0.1:${LIVE_PORT}/`);
    await expect(page.locator(".tab").first()).toBeVisible();

    const back = page.locator(".shell-back");
    await expect(back).toHaveAttribute("title", "Back to the file selector");
    await back.click();

    await expect(page).toHaveURL(/\/browse$/);
    await expect(page.locator(".empty-shell-title")).toHaveText("No script open");
});

// A file selector has nothing to diagnose, so the Problems panel and its counts stay out of the
// reader's way until a script is open. Help stays: it describes the Explorer, which is the one
// thing to do here.
test("shows no Problems panel until a script is open", async ({ page }) => {
    const tabs = page.locator("#footer-drawer .drawer-tab");
    await expect(tabs).toHaveCount(1);
    await expect(tabs.first()).toHaveText("Help");
    await expect(page.locator(".status-bar .diagnostic-summary")).toHaveCount(0);
});

// The Files tab is the Explorer's own control, and its highlight is what says the panel is
// showing. A glyph riding half outside the bed the state paints around it reads as a control
// that is only half pressed, so the two have to be one box.
test("draws the Files tab's glyph inside its own highlight", async ({ page }) => {
    const box = await page.locator(".tabbar-explorer").evaluate((el) => {
        const glyph = el.querySelector(".tab-icon")!.getBoundingClientRect();
        const bed = getComputedStyle(el, "::before");
        const frame = el.getBoundingClientRect();
        // The bed is measured from the bottom it is anchored to, not from an inset at the top:
        // the button's height follows the row, and the bed follows the glyph.
        const bedBottom = frame.bottom - Number.parseFloat(bed.bottom);
        return {
            glyphTop: glyph.top,
            glyphBottom: glyph.bottom,
            bedTop: bedBottom - Number.parseFloat(bed.height),
            bedBottom,
        };
    });

    expect(box.glyphTop).toBeGreaterThanOrEqual(box.bedTop - 1);
    expect(box.glyphBottom).toBeLessThanOrEqual(box.bedBottom + 1);
});

// Writing is Edit's half of the selector: a View reader is browsing, so the actions that make a
// file or a folder stay in place but inert, with a tip saying what to do instead.
test("lets only Edit write in the file selector", async ({ page }) => {
    const newFile = page.locator('.explorer-action[data-action="new-file"]');
    const newFolder = page.locator('.explorer-action[data-action="new-folder"]');
    const callToAction = page.locator(".empty-shell-create");

    await expect(newFile).toBeDisabled();
    await expect(newFolder).toBeDisabled();
    await expect(newFile).toHaveAttribute("title", /switch to Edit/);
    await expect(callToAction).toBeDisabled();

    await page.locator('.mode-toggle-option[data-mode="edit"]').click();

    await expect(newFile).toBeEnabled();
    await expect(newFolder).toBeEnabled();
    await expect(newFile).toHaveAttribute("title", "New file");
    await expect(callToAction).toBeEnabled();
});

// The View/Edit choice is the reader's, not a document's: the shell offers it before anything is
// open, remembers it, and opens the script they pick in it.
test("offers the View/Edit toggle and opens a script in the chosen mode", async ({ page }) => {
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');
    await expect(page.locator('.mode-toggle-option[data-mode="view"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );

    await edit.click();
    await expect(edit).toHaveAttribute("aria-pressed", "true");

    await page.locator(".explorer-script-row", { hasText: "top.dialogue.md" }).click();
    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator('.mode-toggle-option[data-mode="edit"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );
});
