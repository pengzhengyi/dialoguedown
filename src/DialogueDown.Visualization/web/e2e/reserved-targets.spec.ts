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
