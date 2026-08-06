import { test, expect, type Page } from "@playwright/test";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// The Source editor's completions are sourced solely from the compiler's resolved symbols in
// the report payload (no browser-side document scan). An editable (Edit-mode) report carries
// those symbols so the Edit-only completions are active; a static (read-only) one proves they
// are absent there. Several of these names are deliberately not in SAMPLE_SOURCE, so a
// completion offering them can only have come from the payload.
const editUrl = writeReport({
    source: SAMPLE_SOURCE,
    stages: SAMPLE_STAGES,
    mode: "edit",
    symbols: {
        jumpTargets: [
            { slug: "the-market", heading: "The Market" },
            { slug: "the-docks", heading: "The Docks" },
        ],
        speakers: ["Alice", "Merchant"],
        speakerIds: ["guide", "merchant"],
        tags: ["happy", "wise"],
        reservedTargets: [],
    },
});
const staticUrl = writeReport({ source: SAMPLE_SOURCE, stages: SAMPLE_STAGES });

/** Put the cursor at the end of the document, ready to type a fresh line. */
async function focusEditorEnd(page: Page): Promise<void> {
    await page.locator(".cm-content").click();
    await page.keyboard.press("ControlOrMeta+End");
}

const tooltip = ".cm-tooltip-autocomplete";

test("completes a jump target from the payload's resolved symbols", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nAlice: Go [x](#the-m");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["the-market"]);
});

test("completes a speaker id after @", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\n@gu");
    await expect(page.locator(tooltip)).toBeVisible();
    const labels = await page.locator(`${tooltip} li`).allInnerTexts();
    expect(labels).toContain("guide");
    expect(labels).not.toContain("gu"); // the half-typed id does not suggest itself
});

test("completes a tag after a mid-line #", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nBob #ha");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["happy"]);
});

test("completes a known speaker at the start of a line", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nAli");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["Alice"]);
});

test("offers a resolved name that is absent from the document", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    // `Merchant` is not typed anywhere in SAMPLE_SOURCE, so completing it proves the list
    // comes from the payload's resolved symbols rather than a scan of the buffer.
    await page.keyboard.type("\nMer");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["Merchant"]);
});

test("offers scenes when the => jump indicator is typed", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\n=> ");
    await expect(page.locator(tooltip)).toBeVisible();
    // Each scene is offered by its heading (the label); order is CodeMirror's, so match as a set.
    const labels = (await page.locator(`${tooltip} li`).allInnerTexts()).join(" ");
    expect(labels).toContain("The Market");
    expect(labels).toContain("The Docks");
});

test("enriches the => jump indicator into a full [Heading](#slug) target", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\n=> The Mar");
    await expect(page.locator(tooltip)).toBeVisible();
    // CodeMirror blocks accepting a just-opened completion for 75 ms (an anti-misclick guard);
    // wait it out so Enter accepts the scene rather than inserting a newline.
    await page.waitForTimeout(150);
    await page.keyboard.press("Enter");
    // Accepting a scene writes the whole jump target, not just the typed heading.
    await expect(page.locator(".cm-content")).toContainText("=> [The Market](#the-market)");
});

test("does not offer completions in the read-only static report", async ({ page }) => {
    await page.goto(staticUrl);
    await page.locator(".cm-content").click();
    // Even an explicit completion request surfaces nothing when the editor is read-only.
    await page.keyboard.press("ControlOrMeta+ ");
    await expect(page.locator(tooltip)).toHaveCount(0);
});
