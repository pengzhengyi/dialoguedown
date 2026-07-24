import { test, expect } from "@playwright/test";
import { writeFileSync, rmSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { LIVE_DOC, INITIAL_SOURCE } from "./fixture.mjs";

// A 1×1 PNG, written next to the document so a relative image link can resolve.
const PNG_1x1 = Buffer.from(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "base64",
);

// These tests share one server and document (the config runs them serially).
// Each test restores the document to its initial content first and waits for the
// page to reflect it, so tests are order-independent.
test.beforeEach(async ({ page }) => {
    writeFileSync(LIVE_DOC, INITIAL_SOURCE);
    await page.goto("/");
    await expect(page.locator(".source-pane .cm-content")).toContainText("Original Scene");
});

test("serves a view report bound to the document", async ({ page }) => {
    // The payload is a served session, so the tabs render. Config leads (it always has a
    // configuration context here), while the report still opens on Source.
    await expect(page.locator(".tab")).toHaveCount(6); // Config + Source + Markdown/Dialogue/Desugared AST + Semantic Model
    await expect(page.locator(".tab").first()).toHaveText("Config");
    await expect(page.locator(".tab.active")).toHaveText("Source");
    // The View/Edit toggle is shown, starting in View.
    await expect(page.locator('.mode-toggle-option[data-mode="view"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );
    // The document path is shown in the status bar.
    await expect(page.locator("#doc-path")).toBeVisible();
});

test("highlights the dialogue speaker from the compiler's semantic tokens", async ({ page }) => {
    // The served payload carries the compiler's projected tokens, so the editor colors the
    // speaker of "Alice: The original line." with no browser-side lexer.
    await expect(page.locator(".source-pane .dd-tok-speaker-name").first()).toContainText("Alice");

    // A hot reload re-highlights in place: the new speaker is colored after the SSE push.
    writeFileSync(LIVE_DOC, "# New Scene\n\nBob: A different speaker.\n");
    await expect(page.locator(".source-pane .dd-tok-speaker-name").first()).toContainText("Bob");
});

test("serves images alongside the document so relative links resolve", async ({ page }) => {
    const assets = join(dirname(LIVE_DOC), "assets");
    mkdirSync(assets, { recursive: true });
    writeFileSync(join(assets, "pic.png"), PNG_1x1);
    writeFileSync(LIVE_DOC, "# Gallery\n\n![a picture](assets/pic.png)\n");

    const image = page.locator(".source-preview img");
    await expect(image).toBeVisible();
    // The image actually loaded (served by the live server), not a broken link.
    await expect
        .poll(async () => image.evaluate((img: HTMLImageElement) => img.naturalWidth))
        .toBeGreaterThan(0);

    rmSync(assets, { recursive: true, force: true });
});

test("hot-reloads the report when the document changes on disk", async ({ page }) => {
    writeFileSync(LIVE_DOC, "# Rewritten Scene\n\nBob: A brand new line.\n");

    // The server watches the file, recompiles, and pushes over SSE; the client
    // rebuilds in place. No reload/navigation here — the DOM updates itself.
    await expect(page.locator(".source-pane .cm-content")).toContainText("Rewritten Scene");
    await expect(page.locator(".source-preview")).toContainText("A brand new line");
});

test("keeps the active tab across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active")).not.toHaveClass(/source-stage/);

    writeFileSync(LIVE_DOC, "# Another Scene\n\nAlice: Still on the graph tab.\n");

    // After the rebuild the Markdown AST tab is still the active one.
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
    await expect(page.locator("section.stage.active g.node")).not.toHaveCount(0);
});

test("keeps a graph's zoom across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    const viewport = page.locator("section.stage.active svg.tree > g").first();
    const zoomIn = page.locator("section.stage.active .zoom-controls button", { hasText: "+" });

    // Reading the transform first lets the initial default framing settle before we zoom.
    const framed = await viewport.getAttribute("transform");
    await zoomIn.click();
    await zoomIn.click();
    const zoomed = await viewport.getAttribute("transform");
    expect(zoomed).not.toEqual(framed);

    // A disk change rebuilds the graph tabs in place (View mode auto-updates).
    writeFileSync(LIVE_DOC, "# Zoom Scene\n\nAlice: The graph is rebuilt but stays put.\n");
    await expect(page.locator(".source-preview")).toContainText("stays put");

    // The rebuilt Markdown AST graph kept the zoom it had before the reload, rather
    // than snapping back to the default framing.
    await expect(page.locator("section.stage.active svg.tree > g").first()).toHaveAttribute(
        "transform",
        zoomed ?? "",
    );
});

test("keeps a graph's collapsed nodes across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    // Overlays (legend, zoom, detail) sit above the SVG; let the collapse click through.
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });

    const nodes = page.locator("section.stage.active g.node");
    const collapsed = page.locator("section.stage.active g.node.collapsed");
    await expect(nodes.first()).toBeVisible();
    await expect(collapsed).toHaveCount(0);

    // Collapse the root Document node, hiding its whole subtree.
    await nodes.filter({ hasText: "Document" }).first().locator("circle").first().click();
    await expect(collapsed).toHaveCount(1);
    await expect(nodes).toHaveCount(1); // only the collapsed root remains

    // A disk change rebuilds the graph tabs in place (View mode auto-updates).
    writeFileSync(LIVE_DOC, INITIAL_SOURCE + "\n## Added Section\n\nBob: A brand new line.\n");
    // Scope to the Source tab's preview — the node inspector now has its own preview too.
    await expect(page.locator(".source-stage .source-preview")).toContainText("brand new line");

    // The rebuilt graph kept the Document node collapsed rather than expanding every
    // node on reload — the new section stays hidden under the still-collapsed root.
    await expect(collapsed).toHaveCount(1);
    await expect(nodes).toHaveCount(1);
});

test("an untouched graph inherits the current zoom; an adjusted one keeps its own", async ({
    page,
}) => {
    const zoom = () => page.locator("section.stage.active .zoom-input");

    // Set the Markdown AST graph to a distinct zoom (pins its own camera).
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await zoom().fill("150");
    await zoom().press("Enter");
    await expect(zoom()).toHaveValue("150");

    // The untouched Dialogue AST tab inherits the current 150%; then give it its own 80%.
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(zoom()).toHaveValue("150");
    await zoom().fill("80");
    await zoom().press("Enter");
    await expect(zoom()).toHaveValue("80");

    // Coming straight from Dialogue, the untouched Desugared AST inherits the current 80%.
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await expect(zoom()).toHaveValue("80");

    // Each adjusted graph kept its own: Markdown 150%, Dialogue 80%.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(zoom()).toHaveValue("150");
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(zoom()).toHaveValue("80");
});

test("shows a banner when the document is deleted", async ({ page }) => {
    await expect(page.locator("#live-banner")).toBeHidden();

    rmSync(LIVE_DOC);

    await expect(page.locator("#live-banner")).toBeVisible();
    await expect(page.locator("#live-banner")).toContainText("not found");
});

test("toggles to Edit and back to View, reconfiguring the one editor in place", async ({
    page,
}) => {
    const view = page.locator('.mode-toggle-option[data-mode="view"]');
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');

    // Starts in View: read-only (edits are rejected) and no Save button.
    await expect(view).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeHidden();
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.type("XX");
    await expect(page.locator(".source-pane .cm-content")).not.toContainText("XX");

    // Switch to Edit: editable and the Save button appears; the buffer is unchanged.
    await edit.click();
    await expect(edit).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeVisible();
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.type("YY");
    await expect(page.locator(".source-pane .cm-content")).toContainText("YY"); // edits land now

    // Switch back to View (accepting the discard prompt) — read-only again.
    page.once("dialog", (dialog) => void dialog.accept());
    await view.click();
    await expect(view).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeHidden();
});

test("accents blue in View and green in Edit, and keeps the footer on one aligned line", async ({
    page,
}) => {
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');
    const activeTab = page.locator("button.tab.active");
    const GREEN = "rgb(21, 128, 61)"; // #15803d, the Save-button green

    // View: the document reports view mode and the accent is not the Edit green.
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "view");
    await expect(activeTab).not.toHaveCSS("border-bottom-color", GREEN);

    // Edit: the accent switches to green on the active tab and the pressed toggle.
    await edit.click();
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "edit");
    await expect(activeTab).toHaveCSS("border-bottom-color", GREEN);
    await expect(edit).toHaveCSS("background-color", GREEN);

    // The status line keeps the toggle, path, and help toggle vertically centered together.
    const centers = await page.evaluate(() => {
        const center = (selector: string): number => {
            const rect = document.querySelector(selector)!.getBoundingClientRect();
            return rect.top + rect.height / 2;
        };
        return {
            toggle: center(".mode-toggle"),
            path: center("#doc-path"),
            help: center("#help-toggle"),
        };
    });
    expect(Math.abs(centers.toggle - centers.path)).toBeLessThan(2);
    expect(Math.abs(centers.toggle - centers.help)).toBeLessThan(2);
});

test("keeps the View/Edit toggle enabled on graph tabs so editing can begin there", async ({
    page,
}) => {
    const toggle = page.locator(".mode-toggle");
    const view = page.locator('.mode-toggle-option[data-mode="view"]');

    // Source tab is active first: the toggle is interactive.
    await expect(view).toBeEnabled();

    // A graph tab keeps the toggle interactive too — a node can be edited there, so mode is
    // no longer confined to the Source tab.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await expect(view).toBeEnabled();
    await expect(toggle).not.toHaveAttribute("aria-disabled", "true");

    // Back on Source it is still interactive.
    await page.locator(".tab", { hasText: "Source" }).click();
    await expect(view).toBeEnabled();
});
