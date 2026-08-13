import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import {
    CONFIG_EDIT_PORT,
    CONFIG_EDIT_TREE,
    CONFIG_EDIT_DOC,
    CONFIG_EDIT_TOML,
    CONFIG_EDIT_SOURCE,
    CONFIG_EDIT_CONFIG,
} from "./fixture.mjs";

// Config Live Edit end-to-end against the real .NET --edit server: the Config tab's TOML is
// an editable buffer; editing marks it dirty and its speakers pane stale, and Save writes
// dialogue.toml and recompiles so the configured speakers refresh.
const base = `http://127.0.0.1:${CONFIG_EDIT_PORT}`;

const BOB = '\n[[speakers]]\nname = "Bob"\nid = "B"\n';

test.beforeEach(async () => {
    mkdirSync(CONFIG_EDIT_TREE, { recursive: true });
    writeFileSync(CONFIG_EDIT_DOC, CONFIG_EDIT_SOURCE);
    writeFileSync(CONFIG_EDIT_TOML, CONFIG_EDIT_CONFIG);
    // The live server is shared across this file, so a prior test may have left a different
    // config adopted in memory. Re-adopt the reset file so each test starts from a baseline
    // that matches disk (otherwise the optimistic save reports a conflict).
    await fetch(`${base}/api/reload`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ target: "config" }),
    }).catch(() => {});
});

/** Open the Config tab and append `text` at the end of the TOML editor. */
async function appendToConfig(page: import("@playwright/test").Page, text: string): Promise<void> {
    await page.locator(".tab", { hasText: "Config" }).click();
    await page.locator(".config-source .cm-content").click();
    await page.keyboard.press("ControlOrMeta+End"); // CodeMirror Mod-End = document end
    await page.keyboard.insertText(text); // insertText avoids the auto-close-bracket per-key path
}

test("editing the config marks it dirty and stale; Save recompiles the speakers", async ({
    page,
}) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();
    await expect(page.locator(".config-speakers-table")).toContainText("Alice");
    await expect(page.locator(".config-stale-hint")).toBeHidden();

    await appendToConfig(page, BOB);

    // The Config tab goes dirty, the speakers pane is signposted stale, and Save enables.
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    await expect(page.locator(".config-stale-hint")).toBeVisible();
    await expect(page.locator(".save-button")).toBeEnabled();

    await page.locator(".save-button").click();

    // The recompile refreshes the configured speakers and clears dirty + the stale hint.
    await expect(page.locator(".config-speakers-table")).toContainText("Bob");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(page.locator(".config-stale-hint")).toBeHidden();
    expect(readFileSync(CONFIG_EDIT_TOML, "utf8")).toContain("Bob");
});

test("globally collapses configured ignored block and inline Preview regions", async ({ page }) => {
    await page.goto(base);
    await page.evaluate(() => localStorage.removeItem("dd-ignored-preview-collapsed"));
    await page.reload();

    const preview = page.locator(".source-preview");
    const footer = page.locator(".dd-ignored-preview-footer");
    const inline = preview.locator(".dd-preview-ignored-region-inline");
    const block = preview.locator(
        ".dd-preview-ignored-region:not(.dd-preview-ignored-region-inline)",
    );

    await expect(footer).toContainText("2 ignored");
    await expect(footer).toContainText("shown in Preview");
    await expect(block).toHaveAttribute("data-ignored-summary", "Table · 3 lines");
    await expect(inline).toHaveAttribute("title", "Ignored autolink: <https://example.com/road>");

    await footer.getByRole("button", { name: "Hide all ignored content in Preview" }).click();

    await expect(preview).toHaveClass(/ignored-preview-collapsed/);
    await expect(footer).toContainText("hidden in Preview");
    await expect(block.locator(".dd-preview-ignored")).toBeHidden();
    await expect(inline.locator(".dd-preview-ignored")).toBeHidden();
    await expect(page.locator(".source-pane .dd-tok-ignored-markdown")).not.toHaveCount(0);
    expect(
        (await new AxeBuilder({ page }).include(".source-preview-shell").analyze()).violations,
    ).toEqual([]);

    // The preference is report-wide, not tied to one DOM instance.
    await page.reload();
    await expect(page.locator(".source-preview")).toHaveClass(/ignored-preview-collapsed/);
    await expect(page.locator(".dd-ignored-preview-footer")).toContainText("hidden in Preview");

    await page
        .locator(".dd-ignored-preview-footer")
        .getByRole("button", { name: "Show all ignored content in Preview" })
        .click();
    await expect(page.locator(".source-preview")).not.toHaveClass(/ignored-preview-collapsed/);
    const restored = page.locator(".source-preview .dd-preview-ignored");
    await expect(restored).toHaveCount(2);
    await expect(restored.first()).toBeVisible();
    await expect(restored.last()).toBeVisible();
});

test("navigation locks while the config is unsaved", async ({ page }) => {
    await page.goto(base);
    await appendToConfig(page, BOB);
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Declining the discard prompt keeps you on the Config tab with the edits intact.
    page.once("dialog", (dialog) => dialog.dismiss());
    await page.locator(".tab", { hasText: "Source" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Config");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
});

test("autocompletes the TOML schema in the config editor", async ({ page }) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();
    await page.locator(".config-source .cm-content").click();
    await page.keyboard.press("ControlOrMeta+End");
    const tip = page.locator(".config-source .cm-tooltip-autocomplete");

    // A table-header position (a line-leading `[`) suggests `[[speakers]]`. Insert the text and
    // open the menu explicitly, so the check tests the completion source, not the auto-trigger
    // or accept timing (which race on slower CI runners).
    await page.keyboard.insertText("\n[");
    await page.keyboard.press("Control+Space");
    await expect(tip).toBeVisible();
    await expect(tip.locator("li")).toContainText(["[[speakers]]"]);
    await page.keyboard.press("Escape");

    // A key position inside a `[[speakers]]` table suggests the keys and the reserved tag
    // `default`. Finish the header text in full and drop to a key line, so the context is
    // deterministic rather than depending on accepting the menu above.
    await page.keyboard.insertText("[speakers]]\nd");
    await page.keyboard.press("Control+Space");
    await expect(tip).toBeVisible();
    await expect(tip.locator("li")).toContainText(["default"]);
});

test("a saved config id feeds the Source editor's @-autocomplete", async ({ page }) => {
    await page.goto(base);
    // Add a speaker whose id appears nowhere in the dialogue, so completion can only offer it
    // from the recompiled analyzer symbols.
    await appendToConfig(page, '\n[[speakers]]\nname = "Zed"\nid = "ZED"\n');
    await page.locator(".save-button").click();
    await expect(page.locator(".config-speakers-table")).toContainText("Zed");

    // In the Source editor, start an @-mention: the completion draws on the analyzer's
    // symbols, which the save must have refreshed (the reported bug left them stale).
    await page.locator(".tab", { hasText: "Source" }).click();
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("ControlOrMeta+End");
    await page.keyboard.type("\nAlice: hi @Z");

    await expect(page.locator(".cm-tooltip-autocomplete")).toBeVisible();
    await expect(page.locator(".cm-tooltip-autocomplete li")).toContainText(["ZED"]);
});

test("an external dialogue.toml change pauses the config in Conflict, and Reload adopts it", async ({
    page,
}) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();
    await expect(page.locator(".config-speakers-table")).toContainText("Alice");

    // Change dialogue.toml from outside the editor: the config watcher hot-reloads it, and in
    // Edit the config controller pauses in Conflict rather than clobbering the buffer.
    writeFileSync(CONFIG_EDIT_TOML, CONFIG_EDIT_CONFIG + BOB);
    await expect(page.locator(".save-status[data-status='conflict']")).toBeVisible();
    await expect(page.locator(".reload-button")).toBeVisible();

    // Reload adopts the external config as the new baseline and refreshes the speakers.
    await page.locator(".reload-button").click();
    await expect(page.locator(".save-status[data-status='saved']")).toBeVisible();
    await expect(page.locator(".config-speakers-table")).toContainText("Bob");
});

test("a persisted invalid config survives a page reload as saved-invalid", async ({ page }) => {
    await page.goto(base);

    // Persist invalid TOML with an explicit save (Config defaults to Manual, so Save is
    // allow-invalid): the config becomes saved-invalid with a stale speakers pane.
    await appendToConfig(page, "\n[[speakers]]\nbogus = true\n");
    await page.locator(".save-button").click();
    await expect(page.locator(".save-status[data-status='saved-invalid']")).toBeVisible();
    // The parse detail is rendered inside the aria-live status readout, not just a title tooltip.
    await expect(page.locator(".save-status[data-status='saved-invalid']")).toContainText(
        "Saved — invalid TOML:",
    );
    await expect(page.locator(".config-stale-hint")).toBeVisible();

    // Reloading the page must restore that saved-invalid state from the served payload rather
    // than reverting the Config tab to the last valid text with a clean report.
    await page.reload();
    await page.locator(".tab", { hasText: "Config" }).click();
    await expect(page.locator(".save-status[data-status='saved-invalid']")).toBeVisible();
    // The reloaded saved-invalid status carries its parse detail (seeded from report.configMessage).
    await expect(page.locator(".save-status[data-status='saved-invalid']")).toContainText(":");
    await expect(page.locator(".config-stale-hint")).toBeVisible();
    await expect(page.locator(".config-source .cm-content")).toContainText("bogus");
});
