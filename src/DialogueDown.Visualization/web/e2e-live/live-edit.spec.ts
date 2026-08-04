import { test, expect, type Page } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import { LIVE_EDIT_PORT, LIVE_EDIT_DOC, LIVE_EDIT_SOURCE } from "./fixture.mjs";
import { LINE_DEBUGGER_FIXTURE_ID, LINE_DEBUGGER_SOURCE } from "../src/debug-fixture";

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

/** Type into the node inspector's editor (distinct from the Source tab's editor). */
async function editNode(page: Page, text: string) {
    const content = page.locator(".node-source .cm-content");
    await content.click();
    await page.keyboard.press("End");
    // insertText (not type): each node keystroke re-splices and re-renders the preview, so
    // synthetic char-by-char typing can outrun it and drop spaces; a real user never does.
    await page.keyboard.insertText(text);
}

/** Click the prototype breakpoint gutter beside one visible one-based source line. */
async function breakpointCoordinates(page: Page, line: number) {
    const number = page
        .locator(".source-stage .cm-lineNumbers .cm-gutterElement")
        .filter({ hasText: new RegExp(`^${line}$`) });
    const [numberBox, gutterBox] = await Promise.all([
        number.boundingBox(),
        page.locator(".source-stage .dd-debug-breakpoint-gutter").boundingBox(),
    ]);
    if (!numberBox || !gutterBox) throw new Error(`Could not locate breakpoint line ${line}.`);
    return {
        x: gutterBox.x + gutterBox.width / 2,
        y: numberBox.y + numberBox.height / 2,
        gutterBox,
        numberBox,
    };
}

async function clickBreakpoint(page: Page, line: number) {
    const { x, y } = await breakpointCoordinates(page, line);
    await page.mouse.click(x, y);
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

test("the Ctrl/Cmd+S shortcut saves a node edit from a graph tab", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Edit a node directly on a graph tab, then save with the shortcut from there.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await selectNode(page, "Text");
    await editNode(page, " via a shortcut");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    await page.keyboard.press("ControlOrMeta+s");

    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("via a shortcut");
});

test("the Save button saves a node edit from a graph tab", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await selectNode(page, "Text");
    await editNode(page, " via the button");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // The Save button lives in the status bar, so it is reachable from every tab.
    const save = page.locator(".save-button");
    await expect(save).toBeEnabled();
    await save.click();

    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("via the button");
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

// A long, multi-scene document so both panes actually scroll. Headings anchor the sync.
const SCENES = 8;
const SCROLL_DOC =
    "# Prologue\n\n" +
    Array.from({ length: SCENES }, (_, i) => {
        const body = Array.from(
            { length: 6 },
            (_, l) =>
                `Speaker${l % 3}: Line ${l} of scene ${i + 1}. Lorem ipsum dolor sit amet, ` +
                "consectetur adipiscing elit, plenty of filler so the line wraps.",
        ).join("\n\n");
        return `## Scene ${i + 1}\n\n${body}\n`;
    }).join("\n");

/** The heading nearest the top of each pane, and how far below the top it sits. */
async function headingsAtTop(page: Page) {
    return page.evaluate(() => {
        const scene = (label: string | null) => label?.replace(/^#+\s*/, "").trim() ?? null;
        const scroller = document.querySelector<HTMLElement>(".source-pane .cm-scroller")!;
        const preview = document.querySelector<HTMLElement>(".source-preview")!;
        const editorTop = scroller.getBoundingClientRect().top;
        const previewTop = preview.getBoundingClientRect().top;
        const nearestEditor = [...scroller.querySelectorAll(".cm-line")]
            .map((l) => ({
                d: l.getBoundingClientRect().top - editorTop,
                t: l.textContent!.trim(),
            }))
            .filter((o) => /^#{1,6}\s/.test(o.t))
            .sort((a, b) => Math.abs(a.d) - Math.abs(b.d))[0];
        const nearestPreview = [...preview.querySelectorAll("h1,h2,h3,h4,h5,h6")]
            .map((h) => ({
                d: h.getBoundingClientRect().top - previewTop,
                t: h.textContent!.trim(),
            }))
            .sort((a, b) => Math.abs(a.d) - Math.abs(b.d))[0];
        return {
            editorScene: scene(nearestEditor?.t ?? null),
            previewScene: scene(nearestPreview?.t ?? null),
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

test("the editor and preview scroll in sync, anchored on scenes", async ({ page }) => {
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

    // Scrolling the editor down carries the preview to the same scene. The panes have
    // different heights, so this is a real mapping (not a shared scrollbar), and the exact
    // within-scene pixel offset depends on the platform's line metrics — so the robust
    // invariant is that the follower moved off the top and shows the same scene.
    await load();
    await scrollBy(page, ".source-pane .cm-scroller", 1600, 150);
    await page.waitForTimeout(300);
    const byEditor = await headingsAtTop(page);
    expect(await scrollTopOf(".source-preview")).toBeGreaterThan(100); // the preview followed
    expect(byEditor.editorScene).toBe(byEditor.previewScene);

    // Reload for a clean slate (neither pane owns the sync), then drive from the preview:
    // scrolling it down carries the editor to the same scene (bidirectional).
    await load();
    await scrollBy(page, ".source-preview", 1600, 150);
    await page.waitForTimeout(300);
    const byPreview = await headingsAtTop(page);
    expect(await scrollTopOf(".source-pane .cm-scroller")).toBeGreaterThan(100); // the editor followed
    expect(byPreview.editorScene).toBe(byPreview.previewScene);
});

// A multi-node document with a speaker-less line, so the Dialogue AST has a synthetic
// (filled default speaker) node to prove is read-only.
const NODE_DOC = "# Market\n\nGuide: Welcome.\n\nA line with no speaker.\n";

test("edits a node's source in the inspector, and Save recompiles from it", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Select a Text node: its source shows in the inspector editor, editable in Edit.
    await selectNode(page, "Text");
    const nodeEditor = page.locator(".node-source .cm-editor");
    await expect(nodeEditor).toBeVisible();

    // Editing the node re-renders the inspector preview as you type (before Save).
    await editNode(page, " EDITED");
    await expect(page.locator(".node-source .source-preview")).toContainText("EDITED");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Save splices the edit into the document, writes the file, and recompiles the graphs.
    await page.keyboard.press("ControlOrMeta+s");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("EDITED");
});

test("an idle autosave keeps the open node inspector, rebinding it to the recompiled node", async ({
    page,
}) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    // Auto so the edit below is saved by the idle debounce, whose recompile rebuilds the graph.
    await page.context().addCookies([{ name: "dd-save-mode-source", value: "auto", url: base }]);
    await page.goto(`${base}/`);
    await expect(page.locator(".save-mode-option[aria-pressed='true']")).toHaveText("Auto");
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Select and edit a node: its inspector editor is open, and editing arms the idle autosave.
    await selectNode(page, "Text");
    const nodeEditor = page.locator(".node-source .cm-editor");
    await expect(nodeEditor).toBeVisible();
    await editNode(page, " EDITED");
    await expect(page.locator(".node-source .source-preview")).toContainText("EDITED");

    // The idle debounce writes and recompiles — rebuilding the graph tabs — without touching Save.
    await expect(page.locator(".tab.dirty")).toHaveCount(0, { timeout: 4000 });
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("EDITED");

    // The rebuild must not close the inspector: it stays open on the same node, its editor now
    // bound to the recompiled node's source (no reset to the placeholder, no lost selection).
    await expect(nodeEditor).toBeVisible();
    await expect(page.locator("#detail-title")).not.toHaveText("Node details");
    await expect(page.locator("section.stage.active g.node.selected")).toHaveCount(1);
    await expect(page.locator(".node-source .cm-content")).toContainText("EDITED");
});

// The inspector editor must swallow graph-navigation keys so arrows move the cursor, not
// the graph, and Space types a space instead of collapsing a node. The global keydown
// handler otherwise routes those keys to the active tree view. Cover both the read-only
// editor (View) and the editable one (Edit), a fresh page each so neither run's editor
// focus bleeds into the other.
for (const mode of ["view", "edit"] as const) {
    test(`editor keys stay in the editor and never move the graph selection (${mode})`, async ({
        page,
    }) => {
        writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
        await page.goto(`${base}/`);
        if (mode === "edit") {
            await page.locator('.mode-toggle-option[data-mode="edit"]').click();
        }
        await page.locator(".tab", { hasText: "Dialogue AST" }).click();
        await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

        // Select a node so the graph has a selection that arrow keys could move.
        await selectNode(page, "Text");
        const selected = page.locator("section.stage.active g.node.selected");
        await expect(selected).toHaveCount(1);
        const before = await selected.getAttribute("data-tip");
        const collapsedBefore = await page.locator("section.stage.active g.node.collapsed").count();

        // Press the very keys the tree view navigates by, from inside the editor.
        await page.locator(".node-source .cm-content").click();
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
}

test("a synthetic node offers no editor, only an inserted note", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    // The default speaker is inserted by desugar, so it appears on the Desugared AST tab.
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // The speaker-less line's filled default speaker is synthetic: no source to edit. The
    // note explains why and points the reader at the editable parent line instead.
    await selectNode(page, "default");
    const detailNote = page.locator("#detail-body .node-note");
    await expect(detailNote).toContainText("names no speaker");
    await expect(detailNote).toContainText("Edit the line to name one");
});

test("jumps from a graph node to its source, selecting the node's span", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Select a Text node; the inspector offers a "Jump to source" icon beside the title.
    await selectNode(page, "Text");
    const nodeSource = (
        (await page.locator(".node-source .cm-content").textContent()) ?? ""
    ).trim();
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

test("navigation locks while a node edit is unsaved", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await selectNode(page, "Text");
    await editNode(page, " X");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Cancelling the prompt keeps you on the tab with your edit intact.
    page.once("dialog", (d) => void d.dismiss());
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Dialogue AST");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Accepting the prompt discards the edit and lets navigation proceed.
    page.once("dialog", (d) => void d.accept());
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
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

    // Right-click offers the same surround actions; choosing Quote re-quotes the selection.
    await page.keyboard.press("ControlOrMeta+a");
    await editor.click({ button: "right" });
    const menu = page.locator(".context-menu");
    await expect(menu).toBeVisible();
    await expect(menu.locator(".context-menu-item")).toHaveText([
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

test("the fake line debugger prototypes breakpoints, stepping, path choice, and stale rebinding", async ({
    page,
}) => {
    // Ordinary reports never expose the fake debugger.
    await page.goto(`${base}/`);
    await expect(page.locator(".dd-debug-toolbar")).toHaveCount(0);

    writeFileSync(LIVE_EDIT_DOC, LINE_DEBUGGER_SOURCE);
    await page.goto(
        `${base}/r/?debug=fake&fixture=${encodeURIComponent(LINE_DEBUGGER_FIXTURE_ID)}`,
    );
    expect(page.url()).toContain("debug=fake");

    const toolbar = page.locator(".dd-debug-toolbar");
    await expect(toolbar).toBeVisible();
    await expect(toolbar.locator(".dd-debug-prototype")).toHaveText("Prototype · fake program");
    await expect(toolbar.locator(".dd-debug-controls")).toHaveText("");

    // Controls are icon-only with their descriptions in Tippy, and the detached palette can
    // move out of the writer's way.
    await toolbar.getByRole("button", { name: "Continue" }).locator("..").hover();
    await expect(page.locator(".tippy-box").last()).toContainText("Continue");
    await page.getByRole("button", { name: "Hide preview" }).click();
    const beforeDrag = await toolbar.boundingBox();
    const handle = toolbar.getByRole("button", { name: "Move debugger panel" });
    const handleBox = await handle.boundingBox();
    if (!beforeDrag || !handleBox) throw new Error("Could not locate the debugger panel.");
    await page.mouse.move(handleBox.x + handleBox.width / 2, handleBox.y + handleBox.height / 2);
    await page.mouse.down();
    await page.mouse.move(handleBox.x - 120, handleBox.y + 70);
    await page.mouse.up();
    const afterDrag = await toolbar.boundingBox();
    expect(afterDrag?.x).toBeLessThan(beforeDrag.x - 30);
    expect(afterDrag?.y).toBeGreaterThan(beforeDrag.y + 30);

    // Showing the preview shrinks the Source pane; maximizing expands it. The dragged palette
    // re-clamps after either layout change instead of being clipped outside the editor.
    await page.getByRole("button", { name: "Show preview" }).click();
    await expect
        .poll(async () => {
            const [panel, pane] = await Promise.all([
                toolbar.boundingBox(),
                page.locator(".source-stage .source-pane").boundingBox(),
            ]);
            return (
                !!panel &&
                !!pane &&
                panel.x >= pane.x &&
                panel.x + panel.width <= pane.x + pane.width
            );
        })
        .toBe(true);
    await page.getByRole("button", { name: "Full screen" }).click();
    await expect
        .poll(async () => {
            const [panel, pane] = await Promise.all([
                toolbar.boundingBox(),
                page.locator(".source-stage .source-pane").boundingBox(),
            ]);
            return (
                !!panel &&
                !!pane &&
                panel.x >= pane.x &&
                panel.x + panel.width <= pane.x + pane.width
            );
        })
        .toBe(true);
    await page.keyboard.press("Escape");

    // A blank-line request stays hollow; the final execution point binds as a filled dot.
    const blank = await breakpointCoordinates(page, 2);
    const lineNumbersBox = await page.locator(".source-stage .cm-lineNumbers").boundingBox();
    if (!lineNumbersBox) throw new Error("Could not locate line numbers.");
    expect(Math.abs(blank.gutterBox.x + blank.gutterBox.width - lineNumbersBox.x)).toBeLessThan(2);
    await page.mouse.move(blank.x, blank.y);
    await expect(page.locator(".tippy-box").last()).toContainText("Click to add breakpoint");
    await clickBreakpoint(page, 2);
    await clickBreakpoint(page, 13);
    await expect(page.locator(".dd-debug-breakpoint-unverified")).toHaveCount(1);
    await expect(page.locator(".dd-debug-breakpoint-verified")).toHaveCount(1);

    await toolbar.getByRole("button", { name: "Start debugging" }).click();
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Paused · line 3");
    await expect(page.locator(".dd-debug-current-arrow")).toHaveCount(1);

    // A clean View/Edit switch reconfigures the editor but must not restart the debug session.
    await page.getByRole("button", { name: "View", exact: true }).click();
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Paused · line 3");
    await page.getByRole("button", { name: "Edit", exact: true }).click();

    await toolbar.getByRole("button", { name: "Step over" }).click();
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Paused · line 4");
    await toolbar.getByRole("button", { name: "Step over" }).click();

    const paths = toolbar.locator(".dd-debug-paths");
    await expect(paths).toContainText("Choose path");
    await paths.getByRole("button", { name: "Take the forest" }).click();
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Paused · line 7");

    await toolbar.getByRole("button", { name: "Continue" }).click();
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Paused · line 13");

    await toolbar.getByRole("button", { name: "Stop debugging" }).click();
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Ready");
    await expect(page.locator(".dd-debug-current-arrow")).toHaveCount(0);
    await expect(page.locator(".dd-debug-current-line")).toHaveCount(0);

    // Editing invalidates execution but maps the requests with their lines; saving rebinds the
    // exact fixture anchors and verifies the final-line breakpoint at its shifted line.
    await toolbar.getByRole("button", { name: "Start debugging" }).click();
    const editor = page.locator(".source-stage .cm-content");
    await editor.click();
    await page.keyboard.press("ControlOrMeta+Home");
    await page.keyboard.insertText("\n");
    await expect(toolbar.locator(".dd-debug-status")).toHaveText(
        "Source changed — save and restart.",
    );
    await expect(page.locator(".dd-debug-breakpoint-unverified")).toHaveCount(2);

    await page.locator(".save-button").click();
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(toolbar.locator(".dd-debug-status")).toHaveText("Ready");
    await expect(toolbar.getByRole("button", { name: "Start debugging" })).toBeEnabled();
    await expect(page.locator(".dd-debug-breakpoint-unverified")).toHaveCount(1);
    await expect(page.locator(".dd-debug-breakpoint-verified")).toHaveCount(1);
});
