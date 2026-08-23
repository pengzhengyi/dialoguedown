import { test, expect, type Page } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import { LIVE_EDIT_PORT, LIVE_EDIT_DOC, LIVE_EDIT_SOURCE } from "./fixture.mjs";

// Live Edit end-to-end against the real .NET --live server: the Source tab is an
// editable CodeMirror buffer, edits update the preview as you type, the Save button and
// the Ctrl/⌘+S shortcut write the file from any tab, and an external change pauses in a
// conflict without clobbering the buffer.
const base = `http://127.0.0.1:${LIVE_EDIT_PORT}`;

test.beforeEach(async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, LIVE_EDIT_SOURCE);
    // Pin Source to Manual (via its host-scoped cookie) so the explicit-save assertions below
    // are not raced by an idle autosave; the dedicated Auto tests opt back in per test.
    await page.context().addCookies([{ name: "dd-save-mode-source", value: "manual", url: base }]);
});

async function edit(page: Page, text: string) {
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("End");
    await page.keyboard.type(text);
}

/** Select a graph node whose tooltip contains `tip` on the active tab (SVG needs dispatch). */
async function selectNode(page: Page, tip: string) {
    await page.evaluate((tip) => {
        const node = [...document.querySelectorAll("section.stage.active svg .node")].find((n) =>
            (n.getAttribute("data-tip") ?? "").includes(tip),
        );
        (node?.querySelector("circle") ?? node)?.dispatchEvent(
            new MouseEvent("click", { bubbles: true }),
        );
    }, tip);
}

test("edits update the preview and dirty state; the Save button writes the file", async ({
    page,
}) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Nothing to save yet: the Save button is present but disabled.
    const save = page.locator(".save-button");
    await expect(save).toBeVisible();
    await expect(save).toBeDisabled();

    await edit(page, " and a game call `Wave()`");

    // Preview updates as you type; the tab goes dirty and Save enables; the file is untouched.
    await expect(page.locator(".source-preview")).toContainText("Wave()");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    await expect(save).toBeEnabled();
    expect(readFileSync(LIVE_EDIT_DOC, "utf8")).not.toContain("Wave()");

    // Clicking Save writes the buffer, clears dirty, and disables the button again.
    await save.click();
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(save).toBeDisabled();
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("Wave()");
});

test("renders and repairs a Mermaid authoring aid while typing", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    await replaceSource(page, "# Scene\n\n```mermaid\nflowchart LR\nA --> B\n```\n");
    const diagram = page.locator(".source-preview .mermaid-diagram");
    await expect(diagram.locator("svg")).toBeVisible();
    await expect(diagram.locator("svg")).toContainText("A");

    await replaceSource(page, "# Scene\n\n```mermaid\nnot a diagram\n```\n");
    await expect(diagram.locator("svg")).toHaveCount(0);
    await expect(diagram.locator(".mermaid-source")).toContainText("not a diagram");
    await expect(diagram.locator(".mermaid-error")).toBeVisible();

    await replaceSource(page, "# Scene\n\n```mermaid\nflowchart LR\nLatest --> Result\n```\n");
    await expect(diagram.locator("svg")).toBeVisible();
    await expect(diagram.locator("svg")).toContainText("Latest");
});

test("the Discard button confirms, then restores the last saved version", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const save = page.locator(".save-button");
    const discard = page.locator(".discard-button");
    // Present but disabled until there are unsaved edits.
    await expect(discard).toBeVisible();
    await expect(discard).toBeDisabled();

    await edit(page, " and an unwanted trailing note");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    await expect(discard).toBeEnabled();

    // Cancelling the confirmation keeps the edits.
    page.once("dialog", (dialog) => void dialog.dismiss());
    await discard.click();
    await expect(page.locator(".source-pane .cm-content")).toContainText("unwanted trailing note");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Accepting it restores the editor to the last saved version and clears dirty.
    page.once("dialog", (dialog) => void dialog.accept());
    await discard.click();
    await expect(page.locator(".source-pane .cm-content")).not.toContainText(
        "unwanted trailing note",
    );
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(discard).toBeDisabled();
    await expect(save).toBeDisabled();
    // Discard never writes the file.
    expect(readFileSync(LIVE_EDIT_DOC, "utf8")).not.toContain("unwanted trailing note");
});

test("Discard after a save restores to the saved version, not the original", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Save one edit so it becomes the new baseline.
    await edit(page, " FIRST");
    await page.locator(".save-button").click();
    await expect(page.locator(".tab.dirty")).toHaveCount(0);

    // A second, unsaved edit, then Discard restores to the saved (FIRST) version.
    await edit(page, " SECOND");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    page.once("dialog", (dialog) => void dialog.accept());
    await page.locator(".discard-button").click();

    await expect(page.locator(".source-pane .cm-content")).toContainText("FIRST");
    await expect(page.locator(".source-pane .cm-content")).not.toContainText("SECOND");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
});

test("Discard restores the source editor's diagnostics and semantic-token overlays", async ({
    page,
}) => {
    // Two identical scene anchors emit a DLG2001 diagnostic; the speaker lines emit tokens.
    writeFileSync(LIVE_EDIT_DOC, "# Chapter\n\nAlice: Hello.\n\n# Chapter\n\nBob: Bye.\n");
    await page.goto(`${base}/`);
    const source = page.locator(".source-pane");
    await expect(source.locator(".cm-editor")).toBeVisible();
    await expect(source.locator(".dd-tok-speaker-name").first()).toContainText("Alice");
    await expect(source.locator(".cm-lintRange-error").first()).toBeVisible();

    // Edit then Discard — a full-document restore that would otherwise drop those overlays.
    await edit(page, " tail");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    page.once("dialog", (dialog) => void dialog.accept());
    await page.locator(".discard-button").click();
    await expect(page.locator(".tab.dirty")).toHaveCount(0);

    // Both overlays are reapplied from the accepted baseline report, not lost by the restore.
    await expect(source.locator(".dd-tok-speaker-name").first()).toContainText("Alice");
    await expect(source.locator(".cm-lintRange-error").first()).toBeVisible();
});

test("formatting shortcuts and emphasis auto-surround wrap the selection", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const content = page.locator(".source-pane .cm-content");
    await content.click();

    // Bold via the ⌘/Ctrl+B shortcut wraps the selected word.
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.type("brave");
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.press("ControlOrMeta+b");
    await expect(content).toContainText("**brave**");

    // Emphasis auto-surround: typing an emphasis mark over a selection wraps it.
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.type("hero");
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.type("_");
    await expect(content).toContainText("_hero_");
});

test("an external change pauses in Conflict without discarding local edits", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Make a local edit, then change the file on disk from outside the editor.
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.type("X");
    writeFileSync(LIVE_EDIT_DOC, "# Rewritten from disk\n");

    // The status enters Conflict and offers Reload; the editor keeps the local buffer.
    await expect(page.locator(".save-status[data-status='conflict']")).toBeVisible();
    await expect(page.locator(".reload-button")).toBeVisible();
    await expect(page.locator(".source-pane .cm-content")).toContainText("Alice");

    // Reload adopts the disk content as the new baseline and clears the conflict.
    await page.locator(".reload-button").click();
    await expect(page.locator(".source-pane .cm-content")).toContainText("Rewritten from disk");
    await expect(page.locator(".save-status[data-status='saved']")).toBeVisible();
});

test("Source defaults to Auto and saves after the idle delay", async ({ page }) => {
    await page.context().addCookies([{ name: "dd-save-mode-source", value: "auto", url: base }]);
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();
    await expect(page.locator(".save-mode-option[aria-pressed='true']")).toHaveText("Auto");

    await edit(page, " and an autosaved tail");

    // Without touching Save, the idle debounce writes the file and clears dirty.
    await expect(page.locator(".tab.dirty")).toHaveCount(0, { timeout: 4000 });
    await expect(page.locator(".save-status[data-status='saved']")).toBeVisible();
    expect(readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("autosaved tail");
});

test("the Auto/Manual choice is persisted to a host-scoped cookie", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".save-mode-option[aria-pressed='true']")).toHaveText("Manual");

    // Switching to Auto writes the per-document-type preference to a host-scoped cookie, which
    // survives the ephemeral port a later session binds (where an origin-scoped store would not).
    await page.locator(".save-mode-option", { hasText: "Auto" }).click();
    await expect(page.locator(".save-mode-option[aria-pressed='true']")).toHaveText("Auto");
    const cookie = await page.evaluate(() => document.cookie);
    expect(cookie).toContain("dd-save-mode-source=auto");
});

// A long, multi-scene document so both panes actually scroll. Front matter previously parsed as
// a false editor heading, shifting every real scene pair by one.
const SCENES = 8;
const SCROLL_DOC =
    "---\ntitle: Scroll Sync\n---\n# Prologue\n\n" +
    Array.from({ length: SCENES }, (_, i) => {
        const body = Array.from(
            { length: 6 },
            (_, l) =>
                `Speaker${l % 3}: Line ${l} of scene ${i + 1}. Lorem ipsum dolor sit amet, ` +
                "consectetur adipiscing elit, plenty of filler so the line wraps.",
        ).join("\n\n");
        const uneven =
            i === 2
                ? "\n\n> A tall blockquote changes preview height differently from source height.\n" +
                  ">\n> - One nested item.\n> - Another nested item with extra words."
                : "";
        return `## Scene ${i + 1}\n\n${body}${uneven}\n`;
    }).join("\n");

/** The labeled top-level block nearest the top of each pane. */
/**
 * Nudge a pane so the block nearest its top sits exactly at the top.
 *
 * `blocksAtTop` reports the *nearest* block to each pane's top, so a scroll that happens to stop
 * between two blocks lets the panes round to different neighbours -- a one-block disagreement that
 * says nothing about whether they are in sync. Landing squarely on a block removes that luck, and
 * the assertion then means what it says.
 */
async function alignToNearestBlock(page: Page, selector: string) {
    await page.evaluate((sel) => {
        const pane = document.querySelector<HTMLElement>(sel)!;
        const top = pane.getBoundingClientRect().top;
        const rows = sel.includes("cm-scroller")
            ? [...pane.querySelectorAll<HTMLElement>(".cm-line")]
            : [...pane.children].map((c) => c as HTMLElement);
        const nearest = rows
            .filter((row) =>
                /(?:Prologue|Scene \d+|Line \d+ of scene \d+)/.test(row.textContent ?? ""),
            )
            .map((row) => row.getBoundingClientRect().top - top)
            .sort((a, b) => Math.abs(a) - Math.abs(b))[0];
        if (nearest !== undefined) pane.scrollTop += nearest;
    }, selector);
    await page.waitForTimeout(400);
}

async function blocksAtTop(page: Page) {
    return page.evaluate(() => {
        const identity = (label: string | null) =>
            /(?:Prologue|Scene \d+|Line \d+ of scene \d+)/.exec(label ?? "")?.[0] ?? null;
        const scroller = document.querySelector<HTMLElement>(".source-pane .cm-scroller")!;
        const preview = document.querySelector<HTMLElement>(".source-preview")!;
        const editorTop = scroller.getBoundingClientRect().top;
        const previewTop = preview.getBoundingClientRect().top;
        const nearestEditor = [...scroller.querySelectorAll<HTMLElement>(".cm-line")]
            .map((line) => ({
                d: line.getBoundingClientRect().top - editorTop,
                t: line.textContent!.trim(),
            }))
            .filter(({ t }) => identity(t) !== null)
            .sort((a, b) => Math.abs(a.d) - Math.abs(b.d))[0];
        const nearestPreview = [...preview.children]
            .map((block) => ({
                d: block.getBoundingClientRect().top - previewTop,
                t: block.textContent!.trim(),
            }))
            .filter(({ t }) => identity(t) !== null)
            .sort((a, b) => Math.abs(a.d) - Math.abs(b.d))[0];
        return {
            editorBlock: identity(nearestEditor?.t ?? null),
            previewBlock: identity(nearestPreview?.t ?? null),
        };
    });
}

const scrollBy = (page: Page, selector: string, total: number, step: number) =>
    page.evaluate(
        async ([selector, total, step]) => {
            const el = document.querySelector<HTMLElement>(selector as string)!;
            for (let done = 0; done < (total as number); done += step as number) {
                el.scrollTop += step as number;
                // Give each step a couple of frames so the follower (and CodeMirror's
                // re-measure of newly rendered lines) keeps up before the next nudge.
                await new Promise((r) => setTimeout(r, 60));
            }
        },
        [selector, total, step] as const,
    );

test("the editor and preview scroll in sync, anchored on Markdown blocks", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, SCROLL_DOC);
    // The editor virtualizes its lines, so assert on the preview (full HTML) to know the
    // long document loaded. Reused between the two directions for a clean, unowned slate.
    const load = async () => {
        await expect(async () => {
            await page.goto(`${base}/`);
            await expect(page.locator(".source-preview")).toContainText(`Scene ${SCENES}`, {
                timeout: 2000,
            });
        }).toPass({ timeout: 20_000 });
        await expect(page.locator(".source-pane .cm-editor")).toBeVisible();
    };
    const scrollTopOf = (selector: string) =>
        page.evaluate((s) => document.querySelector<HTMLElement>(s)!.scrollTop, selector);

    // Scrolling the editor down carries the preview to the same source block. The panes have
    // different heights, so this is a real mapping (not a shared scrollbar), and the exact
    // within-block pixel offset depends on the platform's line metrics.
    await load();
    await scrollBy(page, ".source-pane .cm-scroller", 1600, 150);
    await page.waitForTimeout(300);
    await alignToNearestBlock(page, ".source-pane .cm-scroller");
    const byEditor = await blocksAtTop(page);
    expect(await scrollTopOf(".source-preview")).toBeGreaterThan(100); // the preview followed
    expect(byEditor.editorBlock).toBe(byEditor.previewBlock);

    // Reload for a clean slate (neither pane owns the sync), then drive from the preview:
    // scrolling it down carries the editor to the same scene (bidirectional).
    await load();
    await scrollBy(page, ".source-preview", 1600, 150);
    await page.waitForTimeout(300);
    await alignToNearestBlock(page, ".source-preview");
    const byPreview = await blocksAtTop(page);
    expect(await scrollTopOf(".source-pane .cm-scroller")).toBeGreaterThan(100); // the editor followed
    expect(byPreview.editorBlock).toBe(byPreview.previewBlock);
});

// A multi-node document with a speaker-less line, so the Dialogue AST has a synthetic
// (filled default speaker) node to prove is read-only.
const NODE_DOC = "# Market\n\nGuide: Welcome.\n\nA line with no speaker.\n";

// A graph tab still hosts one text field — the zoom percentage — and it must swallow the very
// keys the tree view navigates by, so arrows edit the number instead of moving the selection and
// Space does not collapse a node. The global keydown handler otherwise routes those keys to the
// active tree view.
test("text-field keys stay in the field and never move the graph selection", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Select a node so the graph has a selection that arrow keys could move.
    await selectNode(page, "Text");
    const selected = page.locator("section.stage.active g.node.selected");
    await expect(selected).toHaveCount(1);
    const before = await selected.getAttribute("data-tip");
    const collapsedBefore = await page.locator("section.stage.active g.node.collapsed").count();

    // Press the very keys the tree view navigates by, from inside the field.
    await page.locator("section.stage.active .zoom-input").click();
    for (const key of ["ArrowRight", "ArrowLeft", "ArrowDown", "ArrowUp", "Space"]) {
        await page.keyboard.press(key);
    }

    // The selection and every node's collapse state are exactly as before.
    await expect(selected).toHaveCount(1);
    expect(await selected.getAttribute("data-tip")).toBe(before);
    expect(await page.locator("section.stage.active g.node.collapsed").count()).toBe(
        collapsedBefore,
    );
});

test("a synthetic node shows an inserted note instead of a source block", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    // The default speaker is inserted by desugar, so it appears on the Desugared AST tab.
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // The speaker-less line's filled default speaker is synthetic: it maps to no text, so the
    // inspector says so rather than showing an empty Source block.
    await selectNode(page, "default");
    await expect(page.locator("#detail-body .inserted-note")).toContainText(
        "Inserted by the compiler",
    );
    await expect(page.locator("#detail-body pre code")).toHaveCount(0);
});

test("jumps from a graph node to its source, selecting the node's span", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Select a Text node; the inspector offers a "Jump to source" icon beside the title.
    await selectNode(page, "Text");
    const nodeSource = ((await page.locator("#detail-body pre code").textContent()) ?? "").trim();
    const jump = page.locator("#detail-title .node-jump");
    await expect(jump).toBeVisible();
    await expect(jump).toHaveAttribute("aria-label", "Jump to source");
    await jump.click();

    // The Source tab is now active, its editor focused, with the node's exact text selected.
    await expect(page.locator(".tab.active")).toHaveText("Source");
    await expect(page.locator(".source-stage .cm-content")).toBeFocused();
    const selected = (await page.evaluate(() => window.getSelection()?.toString() ?? "")).trim();
    expect(selected).toBe(nodeSource);
});

test("jumps from a synthetic node to its position, placing the caret without a selection", async ({
    page,
}) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    // The filled default speaker is synthetic — inserted by desugar — so it appears here.
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Its jump lands the caret at the node's position rather than selecting a range.
    await selectNode(page, "default");
    const jump = page.locator("#detail-title .node-jump");
    await expect(jump).toBeVisible();
    await jump.click();

    await expect(page.locator(".tab.active")).toHaveText("Source");
    await expect(page.locator(".source-stage .cm-content")).toBeFocused();
    // A zero-width caret places the cursor with nothing selected.
    const selected = await page.evaluate(() => window.getSelection()?.toString() ?? "");
    expect(selected).toBe("");
});

test("jumps from a Semantic-tab node to its source", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Semantic Model" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // The Semantic tab has its own node-details panel; it carries the same jump affordance.
    await selectNode(page, "Market");
    const jump = page.locator(".node-detail-heading .node-jump");
    await expect(jump).toBeVisible();
    await jump.click();

    await expect(page.locator(".tab.active")).toHaveText("Source");
    await expect(page.locator(".source-stage .cm-content")).toBeFocused();
    const selected = (await page.evaluate(() => window.getSelection()?.toString() ?? "")).trim();
    expect(selected.length).toBeGreaterThan(0);
    expect(NODE_DOC).toContain(selected);
});

// The diagnostics overlay is produced by the .NET compiler and pushed into the editor, so a
// Save that introduces a compile error must surface it, and fixing the error must clear it —
// proving the whole payload → overlay path end to end against the real server.
const DIAG_CLEAN = "# Chapter One\n\nAlice: Hello.\n";
const DIAG_BROKEN = "# Chapter\n\nAlice: Hello.\n\n# Chapter\n\nBob: Goodbye.\n";

/** Replace the whole Source buffer deterministically (select all, then insert literally). */
async function replaceSource(page: Page, text: string) {
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.insertText(text);
}

test("a save that introduces an error shows the diagnostics overlay; fixing it clears it", async ({
    page,
}) => {
    writeFileSync(LIVE_EDIT_DOC, DIAG_CLEAN);
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const marker = page.locator("section.stage.active .cm-lint-marker-error");
    await expect(marker).toHaveCount(0); // a clean compile has no overlay

    // Introduce a duplicate-anchor error (DLG2001) and save: the overlay appears.
    await replaceSource(page, DIAG_BROKEN);
    await page.keyboard.press("ControlOrMeta+s");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(marker.first()).toBeVisible();

    // Hovering the squiggle shows the message and a docs link for the code.
    await page.locator("section.stage.active .cm-lintRange-error").first().hover();
    const tooltip = page.locator(".cm-tooltip-lint");
    await expect(tooltip).toContainText("anchor");
    await expect(tooltip.locator("a.diagnostic-tooltip-link")).toHaveAttribute(
        "href",
        /error-codes\.html#dlg2001$/,
    );
    await page.mouse.move(0, 0); // dismiss the hover tooltip before editing again

    // Fix the error and save: the overlay clears.
    await replaceSource(page, DIAG_CLEAN);
    await page.keyboard.press("ControlOrMeta+s");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(marker).toHaveCount(0);
});

test("a preview heading reveals a link icon that copies the full jump target", async ({ page }) => {
    await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const heading = page.locator(".source-preview h1", { hasText: "Scene" });
    const link = heading.locator(".heading-anchor-link");
    // The link is hidden until the heading is hovered (GitHub-style).
    await expect(link).toHaveCSS("opacity", "0");
    await heading.hover();
    await expect(link).toHaveCSS("opacity", "1");

    await link.click();
    await expect(page.locator(".toast")).toHaveText("Copied [Scene](#scene)");
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe("[Scene](#scene)");
});

test("the active heading line reveals a #slug hint that copies the anchor", async ({ page }) => {
    await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // The caret starts on the "# Scene" heading line, so its #slug hint shows after the text.
    const hint = page.locator(".source-pane .dd-slug-hint");
    await expect(hint).toHaveText("#scene");
    await hint.click();
    await expect(page.locator(".toast")).toHaveText("Copied #scene");
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe("#scene");

    // Moving the caret off the heading line hides the hint (revealed on the active line only).
    await page.locator(".source-pane .cm-line", { hasText: "Alice" }).click();
    await expect(hint).toHaveCount(0);
});

test("quotes and unquotes the selection by keyboard and from the surround menu", async ({
    page,
}) => {
    await page.goto(`${base}/`);
    const editor = page.locator(".source-pane .cm-content");
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Select the whole document and quote it with Cmd/Ctrl+. (adds a marker to every line).
    await editor.click();
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.press("ControlOrMeta+Period");
    await expect(editor).toContainText("> # Scene");
    await expect(editor).toContainText("> Alice: The first line.");

    // Unquote it back with Cmd/Ctrl+Shift+.
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.press("ControlOrMeta+Shift+Period");
    await expect(editor).not.toContainText("> # Scene");

    // Right-click offers Jump-to plus the same surround actions; choosing Quote re-quotes.
    await page.keyboard.press("ControlOrMeta+a");
    await editor.click({ button: "right" });
    const menu = page.locator(".context-menu");
    await expect(menu).toBeVisible();
    await expect(menu.locator(".context-menu-item")).toHaveText([
        "Jump to",
        "Bold",
        "Italic",
        "Strikethrough",
        "Quote",
        "Unquote",
    ]);
    await menu.getByRole("menuitem", { name: "Quote", exact: true }).click();
    await expect(editor).toContainText("> # Scene");
});

test("Tab indents at the line front and Esc leaves the editor, instead of Tab moving focus out", async ({
    page,
}) => {
    await page.goto(`${base}/`);
    const editor = page.locator(".source-pane .cm-content");
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Put the caret at the start of the "Alice" line, then press Tab.
    await page.locator(".source-pane .cm-line", { hasText: "Alice" }).click();
    await page.keyboard.press("Home");
    await page.keyboard.press("Tab");

    // Tab indented the line (rather than tabbing out of the editor): the line now starts with
    // whitespace, and the editor kept focus.
    const indented = await page.evaluate(() =>
        [...document.querySelectorAll(".source-pane .cm-line")].some((line) =>
            /^\s+Alice: The first line\./.test(line.textContent ?? ""),
        ),
    );
    expect(indented).toBe(true);
    await expect(editor).toBeFocused();

    // Escape is the keyboard escape hatch — it blurs the editor so Tab-to-indent is not a trap.
    await page.keyboard.press("Escape");
    await expect(editor).not.toBeFocused();
});

test("Tab inserts spaces mid-line instead of indenting the whole line", async ({ page }) => {
    await page.goto(`${base}/`);
    const editor = page.locator(".source-pane .cm-content");
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Place the caret after the first character of the "Alice" line (past the leading edge), so
    // Tab should insert spaces at the caret rather than re-indenting the line.
    await page.locator(".source-pane .cm-line", { hasText: "Alice" }).click();
    await page.keyboard.press("Home");
    await page.keyboard.press("ArrowRight");
    await page.keyboard.press("Tab");

    // Two spaces were inserted at the caret (splitting "Alice"), and the line front stayed flush.
    const insertedMidLine = await page.evaluate(() =>
        [...document.querySelectorAll(".source-pane .cm-line")].some((line) =>
            /^A {2}lice: The first line\./.test(line.textContent ?? ""),
        ),
    );
    expect(insertedMidLine).toBe(true);
    await expect(editor).toBeFocused();
});

test("keeps the line debugger UI dormant in ordinary served reports", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".dd-debug-toolbar")).toHaveCount(0);
    await expect(page.locator(".dd-debug-breakpoint-gutter")).toHaveCount(0);
    await expect(page.locator(".dd-debug-execution-gutter")).toHaveCount(0);
});

test("jumps from a Source selection to the enclosing node in a chosen stage", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);

    // Source is the default tab; put the caret in the scene heading, then reverse-jump.
    const content = page.locator(".source-pane .cm-content");
    await content.getByText("Market", { exact: false }).first().click();
    await content.getByText("Market", { exact: false }).first().click({ button: "right" });

    await page.locator(".context-menu-item", { hasText: "Jump to" }).hover();
    await page.locator(".context-submenu .context-menu-item", { hasText: "Dialogue AST" }).click();

    // The Dialogue AST tab is now active with the enclosing node selected (and centered).
    await expect(page.locator(".tab.active")).toHaveText("Dialogue AST");
    await expect(page.locator("section.stage.active g.node.selected")).toHaveCount(1);
});

test("Alt-J opens the Jump-to picker from the source caret", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);

    const content = page.locator(".source-pane .cm-content");
    await content.getByText("Welcome", { exact: false }).first().click();
    await page.keyboard.press("Alt+j");

    // The picker lists the stages directly; choosing one reveals the enclosing node there.
    await page.locator(".context-menu .context-menu-item", { hasText: "Semantic Model" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Semantic Model");
    await expect(page.locator("section.stage.active g.node.selected")).toHaveCount(1);
});

test("Jump to Semantic Model resolves a multi-line selection to its scene", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);

    // Select across two body lines inside the "Market" scene (no single line encloses it).
    const content = page.locator(".source-pane .cm-content");
    await content.getByText("Welcome", { exact: false }).first().click();
    await page.keyboard.press("Home");
    await page.keyboard.press("Shift+ArrowDown");
    await page.keyboard.press("Shift+ArrowDown");
    await page.keyboard.press("Shift+End");

    await content.click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Jump to" }).hover();
    await page
        .locator(".context-submenu .context-menu-item", { hasText: "Semantic Model" })
        .click();

    // The jump lands on the scene, not the whole document or a stray fragment.
    await expect(page.locator(".tab.active")).toHaveText("Semantic Model");
    await expect(page.locator("section.stage.active g.node.selected")).toHaveCount(1);
    await expect(page.locator(".node-detail-heading")).toContainText("Market");
});

// A jump from the Source into the Dialogue Graph must be ranked by what each node *contains*, not
// by what its flow leads to. The graph's child edges are the spanning tree it is drawn with, so
// following them would stretch the first option's reach down the rest of the script — through the
// jump and into the next scene — and the jump would land on that option instead of the choice.
const BRANCHING_DOC = [
    "# Forest",
    "",
    "Alice: Ready?",
    "",
    "- `60%` A fox darts across.",
    "- `40%` An owl watches.",
    "",
    "=> [onward](#lake)",
    "",
    "# Lake",
    "",
    "Alice: Done.",
    "",
].join("\n");

test("Jump to Dialogue Graph resolves a selection to what contains it, not what it leads to", async ({
    page,
}) => {
    writeFileSync(LIVE_EDIT_DOC, BRANCHING_DOC);
    await page.goto(`${base}/`);

    // Select from inside the first option into the second. Neither option's own span covers that,
    // but the choice holding both does — while the first option's *flow* reaches the whole rest of
    // the script, so ranking by flow would land there instead.
    const content = page.locator(".source-pane .cm-content");
    await content.getByText("A fox darts", { exact: false }).first().click();
    await page.keyboard.press("Home");
    for (let step = 0; step < 12; step += 1) await page.keyboard.press("ArrowRight");
    await page.keyboard.press("Shift+ArrowDown");

    await content.click({ button: "right" });
    await page.locator(".context-menu-item", { hasText: "Jump to" }).hover();
    await page
        .locator(".context-submenu .context-menu-item", { hasText: "Dialogue Graph" })
        .click();

    await expect(page.locator(".tab.active")).toHaveText("Dialogue Graph");
    const selected = page.locator("section.stage.active g.node.selected");
    await expect(selected).toHaveCount(1);
    // The choice that holds both options — not the first option, whose flow runs on into "Lake".
    await expect(selected).toContainText("Random choice");
});
