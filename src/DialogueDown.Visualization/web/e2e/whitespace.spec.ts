import { test, expect, type Locator, type Page } from "@playwright/test";
import type { Report } from "../src/model";
import { contrastRatio, over, parseColor } from "./contrast";
import { SAMPLE_STAGES, writeReport } from "./report";

/** A script indented with spaces and with a tab, so both kinds of mark have something to draw. */
const INDENTED_SOURCE = [
    "# Scene",
    "",
    "> **if** gold > 10",
    ">",
    ">   Bob: Indented by three spaces.",
    ">\tBob: Indented by a tab.",
    "",
].join("\n");

const REPORT: Report = { mode: "view", source: INDENTED_SOURCE, stages: SAMPLE_STAGES };

const toggle = (page: Page) => page.locator(".whitespace-toggle");
const spaces = (page: Page) => page.locator(".source-pane .cm-content .cm-highlightSpace");
const tabs = (page: Page) => page.locator(".source-pane .cm-content .cm-highlightTab");

test("the status-line toggle shows whitespace and the choice survives a reload", async ({
    page,
}) => {
    await page.goto(writeReport(REPORT));
    await expect(page.locator(".source-pane .cm-content")).toBeVisible();
    await expect(spaces(page)).toHaveCount(0);

    await toggle(page).click();

    await expect(toggle(page)).toHaveAttribute("aria-pressed", "true");
    await expect(spaces(page).first()).toBeVisible();
    await expect(tabs(page)).toHaveCount(1);

    await page.reload();

    await expect(toggle(page)).toHaveAttribute("aria-pressed", "true");
    await expect(spaces(page).first()).toBeVisible();
});

test("Alt-W toggles whitespace from inside the editor without typing", async ({ page }) => {
    await page.goto(writeReport(REPORT));
    const content = page.locator(".source-pane .cm-content");
    await content.click();

    await page.keyboard.press("Alt+KeyW");

    await expect(spaces(page).first()).toBeVisible();
    await expect(toggle(page)).toHaveAttribute("aria-pressed", "true");
    await expect(content).toBeFocused();
    expect(await content.textContent()).toBe(INDENTED_SOURCE.replaceAll("\n", ""));

    await page.keyboard.press("Alt+KeyW");

    await expect(spaces(page)).toHaveCount(0);
});

test("whitespace is drawn fainter than CodeMirror's default", async ({ page }) => {
    await page.goto(writeReport(REPORT));
    await toggle(page).click();

    const dot = await spaces(page)
        .first()
        .evaluate((element) => getComputedStyle(element).backgroundImage);

    // CodeMirror's own dot fills 20% of its cell in a fixed gray; the report's fills 15% in the
    // theme's muted color, mixed toward transparent.
    expect(dot).toContain("15%");
    expect(dot).not.toContain("rgb(170, 170, 170)");
});

test("the whitespace toggle sits beside Help, as close as an editor's status-bar icons", async ({
    page,
}) => {
    await page.goto(writeReport(REPORT));

    const toggleBox = (await toggle(page).boundingBox())!;
    const helpBox = (await page.locator("#help-toggle").boundingBox())!;

    // VS Code separates neighboring status-bar items by 6px; the line's own group gap is wider.
    expect(helpBox.x - (toggleBox.x + toggleBox.width)).toBeCloseTo(6, 0);
});

for (const scheme of ["light", "dark"] as const) {
    test(`the pressed toggle stands out from the status line (${scheme})`, async ({ page }) => {
        await page.emulateMedia({ colorScheme: scheme });
        await page.goto(writeReport(REPORT));
        await toggle(page).click();

        const contrast = await fillContrastOf(toggle(page));

        // A faint wash reads as unpressed; Word's ¶ tile is plainly darker than its ribbon.
        expect(contrast).toBeGreaterThan(1.4);
    });
}

/** The contrast between an element's own fill, as painted, and the background behind it. */
async function fillContrastOf(locator: Locator): Promise<number> {
    const rendered = await locator.evaluate(async (element) => {
        // Pico eases a button's background; read the settled fill, not a frame of the fade.
        await Promise.all(element.getAnimations().map((animation) => animation.finished));
        let node = element.parentElement;
        let backdrop = "rgb(255, 255, 255)";
        while (node !== null) {
            const color = getComputedStyle(node).backgroundColor;
            // Transparent computes as `rgba(…, 0)` or `color(srgb … / 0)`.
            if (color !== "transparent" && !/[,/]\s*0\)$/.test(color)) {
                backdrop = color;
                break;
            }
            node = node.parentElement;
        }
        return { fill: getComputedStyle(element).backgroundColor, backdrop };
    });

    const fill = parseColor(rendered.fill);
    const backdrop = parseColor(rendered.backdrop);
    if (!fill || !backdrop) {
        throw new Error(`Unreadable color: ${rendered.fill} over ${rendered.backdrop}`);
    }
    return contrastRatio(over(fill, backdrop), backdrop);
}
