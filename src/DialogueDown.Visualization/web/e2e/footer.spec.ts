import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// A deliberately long absolute path (long directory, ordinary filename) so the
// directory must ellipsise and the layout must cope on a narrow screen.
const LONG_PATH =
    "/Users/pengzhengyi/Documents/Dev/PersonalProjects/Game/DialogueDown/very/deep/nested/folders/scene.dialogue.md";

const REPORT_WITH_PATH: Report = {
    mode: "view",
    path: LONG_PATH,
    source: SAMPLE_SOURCE,
    stages: SAMPLE_STAGES,
};

// Light scheme is the harder case for the historic bug: the button-inverse text
// color (white) would be invisible on the light footer.
test.use({ colorScheme: "light" });

test("the document path stays legible on hover (not white-on-white)", async ({ page }) => {
    await page.goto(writeReport(REPORT_WITH_PATH));
    const path = page.locator("#doc-path");
    await expect(path).toBeVisible();

    await path.hover();

    const color = await path.evaluate((el) => getComputedStyle(el).color);
    const [r, g, b] = color.match(/\d+/g)!.map(Number);
    const luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    // The bug rendered the hover text as the button inverse (white, luminance 255),
    // which vanished on the light footer. Require a clearly darker foreground.
    expect(luminance).toBeLessThan(180);
});

test("the help expands full-width above the status line, clear of it", async ({ page }) => {
    await page.goto(writeReport(REPORT_WITH_PATH));

    const toggle = page.locator("#help-toggle");
    const content = page.locator("#help-content");

    // Collapsed by default.
    await expect(content).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");

    await toggle.click();
    await expect(content).toBeVisible();
    await expect(toggle).toHaveAttribute("aria-expanded", "true");

    // The panel opens *above* the status line, so the line stays pinned to the bottom edge as
    // the report's constant footer and the panel reads as attached to the content it describes.
    // Measure the drawer, not the help body: the body is the scroller's content, so its box
    // reports the full unclipped text rather than what is on screen.
    const pathBox = (await page.locator("#doc-path").boundingBox())!;
    const drawerBox = (await page.locator("#footer-drawer").boundingBox())!;
    expect(drawerBox.y + drawerBox.height).toBeLessThanOrEqual(pathBox.y + 1);

    // Collapsing hides it again.
    await toggle.click();
    await expect(content).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
});

test("the document path is a compact ellipsised chip, not a full-width block", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(writeReport(REPORT_WITH_PATH));

    // Even with plenty of room, the chip stays capped instead of stretching the
    // long path across the row.
    const box = (await page.locator("#doc-path").boundingBox())!;
    const footerWidth = (await page.locator(".app-footer").boundingBox())!.width;
    expect(box.width).toBeLessThan(footerWidth * 0.5);

    // The directory head is actually ellipsised: its content overflows its box.
    const headTruncated = await page
        .locator("#doc-path .path-head")
        .evaluate((el) => el.scrollWidth > el.clientWidth);
    expect(headTruncated).toBe(true);

    // The filename tail stays fully visible.
    await expect(page.locator("#doc-path .path-tail")).toHaveText("/scene.dialogue.md");
});
