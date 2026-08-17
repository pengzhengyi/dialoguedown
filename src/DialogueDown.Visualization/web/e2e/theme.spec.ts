import { test, expect } from "@playwright/test";
import { writeReport, SAMPLE_REPORT } from "./report";
import { selectTheme, runningTransitions } from "./theme";

test.beforeEach(async ({ page }) => {
    await page.goto(writeReport(SAMPLE_REPORT));
});

test("switching the theme leaves no color transition mid-flight", async ({ page }) => {
    // The guard behind every themed appearance check in this suite. Sampling a color while it is
    // still animating reads a value belonging to neither theme, which is how a contrast check can
    // fail in CI and pass locally on identical styling.
    await selectTheme(page, "dark");

    expect(await runningTransitions(page)).toBe(0);
});

test("a theme switch really does animate, so the wait is not decorative", async ({ page }) => {
    // If themes ever stopped transitioning, the helper above would be a no-op that silently
    // passes forever. Prove the race it guards against is real: without the wait, the switch
    // leaves transitions running.
    await page.locator(".theme-select").selectOption("dark");

    expect(await runningTransitions(page)).toBeGreaterThan(0);
});
