import { test, expect, type Page } from "@playwright/test";
import type { Report } from "../src/model";
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
