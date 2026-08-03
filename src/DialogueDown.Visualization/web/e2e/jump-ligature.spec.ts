import { test, expect } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";

const url = writeReport({
    source: "=> [Go](#go)\nAlice: A => B\nAlice: `=> [literal](#literal)`\n",
    stages: SAMPLE_STAGES,
});

test("uses a real preview-only Fira Code ligature for jump indicators", async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2);

    const preview = page.locator(".source-preview");
    const indicator = preview.locator(".jump-ligature");
    await expect(indicator).toHaveCount(1);
    await expect(indicator).toHaveText("=>");
    await expect(page.locator(".source-pane .jump-ligature")).toHaveCount(0);

    await page.evaluate(() => document.fonts.ready);
    expect(await page.evaluate(() => document.fonts.check('16px "Fira Code"'))).toBe(true);
    await expect(indicator).toHaveCSS("font-family", /Fira Code/);
    await expect(indicator).toHaveCSS("font-feature-settings", '"calt"');
});
