import { test, expect } from "@playwright/test";
import { writeFileSync } from "node:fs";
import {
    SEMANTIC_AUTOCOMPLETE_PORT,
    SEMANTIC_AUTOCOMPLETE_DOC,
    SEMANTIC_AUTOCOMPLETE_SOURCE,
} from "./fixture.mjs";

// Semantic autocompletion end-to-end against the real .NET --edit server. The analyzer
// resolves the document's speakers, scenes, and jumps and carries them in the report
// payload; the Source editor's completion draws solely on those resolved symbols (no
// browser-side scan). The fixture "# Scene / Alice: …" resolves to a `scene` jump target
// (plus the always-available reserved `#END` terminator), which we prove still feeds
// completion even after the heading it came from is deleted — the list comes from the last
// compile's payload, not from scanning the current buffer.
const base = `http://127.0.0.1:${SEMANTIC_AUTOCOMPLETE_PORT}`;
const tooltip = ".cm-tooltip-autocomplete";

test.beforeEach(() => {
    writeFileSync(SEMANTIC_AUTOCOMPLETE_DOC, SEMANTIC_AUTOCOMPLETE_SOURCE);
});

test("completes a jump target from the analyzer's resolved symbols", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // The server emitted the analyzer's resolved symbols alongside the source and stages: the
    // document's `scene` heading and the always-available reserved `#END` terminator.
    const symbols = await page.evaluate(
        () =>
            (window as unknown as { __DD_REPORT__?: { symbols?: unknown } }).__DD_REPORT__?.symbols,
    );
    expect(symbols).toMatchObject({
        jumpTargets: [
            { slug: "scene", heading: "Scene" },
            { slug: "END", heading: "End the run" },
        ],
    });

    // Replace the buffer with a heading-less document: the current buffer no longer contains
    // the `scene` heading, so a completion offering it can only come from the payload symbols.
    await page.locator(".cm-content").click();
    await page.keyboard.press("ControlOrMeta+A");
    await page.keyboard.type("Alice: Go [x](#s");

    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["scene"]);
});
