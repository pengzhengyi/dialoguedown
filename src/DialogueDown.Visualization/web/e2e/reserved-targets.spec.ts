import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

const url = writeReport({
    source: SAMPLE_SOURCE,
    stages: SAMPLE_STAGES,
    symbols: {
        jumpTargets: [
            { slug: "the-market", heading: "The Market" },
            { slug: "END", heading: "End the run" },
        ],
        speakers: [],
        speakerIds: [],
        tags: [],
        reservedTargets: [{ anchor: "END", label: "End", role: "Terminal" }],
    },
});

test.beforeEach(async ({ page }) => {
    await page.goto(url);
});

test("mirrors the End sentinel with an always-present Preview footer", async ({ page }) => {
    const end = page.locator(".dd-reserved-target-row");
    const ignored = page.locator(".dd-ignored-preview-footer");

    await expect(ignored).toContainText("0 ignored");
    await expect(ignored).toContainText("nothing omitted");
    const commands = ignored.getByRole("button");
    await expect(commands).toHaveCount(2);
    for (const command of await commands.all()) await expect(command).toBeDisabled();

    const [endBox, ignoredBox] = await Promise.all([end.boundingBox(), ignored.boundingBox()]);
    if (!endBox || !ignoredBox) throw new Error("Could not measure the Source/Preview footers.");
    expect(ignoredBox.height).toBeCloseTo(endBox.height, 1);
    expect(ignoredBox.y).toBeCloseTo(endBox.y, 1);
});

test("joins the two footers into one band across the split divider", async ({ page }) => {
    // The panes are separated by a draggable divider, and its column showed through as a white
    // notch in the one row where the two footers should read as a single bar.
    const end = page.locator(".dd-reserved-target-row");
    const ignored = page.locator(".dd-ignored-preview-footer");
    const [endBox, ignoredBox] = await Promise.all([end.boundingBox(), ignored.boundingBox()]);
    if (!endBox || !ignoredBox) throw new Error("Could not measure the Source/Preview footers.");

    expect(ignoredBox.x).toBeCloseTo(endBox.x + endBox.width, 1);
});

test("keeps the joined footer clear of the divider that separates the panes above it", async ({
    page,
}) => {
    // Reaching across the divider must not drag the footer's own content left with it, or the
    // Preview footer's marker would stop lining up with the pane it belongs to.
    const marker = page.locator(".dd-ignored-preview-footer-marker");
    const preview = page.locator(".source-preview");
    const [markerBox, previewBox] = await Promise.all([
        marker.boundingBox(),
        preview.boundingBox(),
    ]);
    if (!markerBox || !previewBox) throw new Error("Could not measure the Preview footer.");

    expect(markerBox.x).toBeGreaterThanOrEqual(previewBox.x);
});

test("shows a fixed, copyable End sentinel without changing source lines", async ({ page }) => {
    await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);

    const panel = page.locator(".dd-reserved-targets");
    await expect(panel).toBeVisible();
    await expect(panel).toContainText("∞");
    await expect(panel).toContainText("End");
    await expect(panel).toContainText("#END");
    expect(
        (await new AxeBuilder({ page }).include(".dd-reserved-targets").analyze()).violations,
    ).toEqual([]);
    await expect(page.locator(".source-stage .cm-line", { hasText: /^End$/ })).toHaveCount(0);

    const [panelBefore, scrollerBefore, gutter, marker] = await Promise.all([
        panel.boundingBox(),
        page.locator(".source-stage .cm-scroller").boundingBox(),
        page.locator(".source-stage .cm-gutters").boundingBox(),
        page.locator(".dd-reserved-target-marker").boundingBox(),
    ]);
    if (!panelBefore || !scrollerBefore || !gutter || !marker) {
        throw new Error("Could not measure reserved target panel.");
    }
    expect(panelBefore.y).toBeGreaterThanOrEqual(scrollerBefore.y + scrollerBefore.height - 2);
    expect(marker.width).toBeCloseTo(gutter.width, 0);

    await page.locator(".source-stage .cm-scroller").evaluate((element) => {
        element.scrollTop = element.scrollHeight;
    });
    await expect(page.locator(".source-stage .cm-lineNumbers .cm-gutterElement").last()).toHaveText(
        String(SAMPLE_SOURCE.split("\n").length),
    );
    const rightEdges = await page.evaluate(() => {
        const contentRight = (element: Element | null): number | null => {
            if (!element) return null;
            const style = getComputedStyle(element);
            return element.getBoundingClientRect().right - parseFloat(style.paddingRight);
        };
        return {
            digit: contentRight(
                document.querySelector(
                    ".source-stage .cm-lineNumbers .cm-gutterElement:last-child",
                ),
            ),
            marker: contentRight(document.querySelector(".dd-reserved-target-marker")),
        };
    });
    if (rightEdges.digit === null || rightEdges.marker === null) {
        throw new Error("Could not measure reserved marker alignment.");
    }
    expect(rightEdges.marker).toBeCloseTo(rightEdges.digit, 0);
    const panelAfter = await panel.boundingBox();
    expect(panelAfter?.y).toBeCloseTo(panelBefore.y, 0);

    await page.keyboard.press("z");
    await expect(panel).toBeVisible();
    await page.keyboard.press("z");

    await panel.getByRole("button", { name: "Copy jump link to End" }).click();
    await expect(page.locator(".toast")).toHaveText("Copied [End](#END)");
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe("[End](#END)");
});

test("stays behind the footer help overlay on a short window", async ({ page }) => {
    // On a short window the footer help panel floats up over the editor (height <= 640px).
    // CodeMirror defaults its panels to z-index 300, so without a reset the reserved End row
    // painted on top of the help text. It must sit behind the overlay instead.
    await page.setViewportSize({ width: 1150, height: 600 });

    const panel = page.locator(".dd-reserved-targets");
    await expect(panel).toBeVisible();

    await page.locator("#help-toggle").click();
    await expect(page.locator(".app-footer .shortcuts")).toBeVisible();

    const verdict = await page.evaluate(() => {
        const reserved = document.querySelector(".dd-reserved-targets");
        const help = document.querySelector(".app-footer .shortcuts:not([hidden])");
        if (!reserved || !help) return "missing";
        const r = reserved.getBoundingClientRect();
        const h = help.getBoundingClientRect();
        if (r.bottom <= h.top || r.top >= h.bottom) return "no-overlap";
        const x = Math.round(r.left + r.width / 2);
        const y = Math.round((Math.max(r.top, h.top) + Math.min(r.bottom, h.bottom)) / 2);
        const hit = document.elementFromPoint(x, y);
        if (hit?.closest(".app-footer .shortcuts")) return "help-on-top";
        if (hit?.closest(".dd-reserved-targets")) return "reserved-on-top";
        return "other";
    });
    expect(verdict).toBe("help-on-top");
});
