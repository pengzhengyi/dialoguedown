import { test, expect } from "@playwright/test";
import { writeReport, SAMPLE_STAGES } from "./report";
import type { Report } from "../src/model";

// A report whose applied configuration comes from a dialogue.toml with one speaker that
// carries a custom tag and the reserved `default` tag — enough to show the Config tab's
// two columns and its reserved-vs-custom tag coloring.
const configured: Report = {
    source: "# Market\n\nGuide: Hello.\n",
    path: "/proj/scene.dialogue.md",
    stages: SAMPLE_STAGES,
    configuration: {
        file: {
            path: "/proj/dialogue.toml",
            source: '[[speakers]]\nname = "Guide"\nid = "G"\ntags = ["role=host"]\ndefault = true\n',
        },
        speakers: [
            {
                name: "Guide",
                id: "G",
                tags: [
                    { name: "role", value: "host", reserved: false },
                    { name: "default", reserved: true },
                ],
            },
        ],
    },
};

// A report with a configuration context but no dialogue.toml — the no-config state.
const noConfig: Report = {
    source: "# Market\n\nGuide: Hello.\n",
    path: "/proj/scene.dialogue.md",
    stages: SAMPLE_STAGES,
    configuration: { speakers: [] },
};

test.describe("Config tab — a configured project", () => {
    test.beforeEach(async ({ page }) => {
        await page.goto(writeReport(configured));
    });

    test("leads the tabs with a gear icon but the report opens on Source", async ({ page }) => {
        await expect(page.locator(".tab").first()).toHaveText("Config");
        await expect(page.locator(".tab").first().locator("svg.tab-icon")).toBeVisible();
        await expect(page.locator(".tab.active")).toHaveText("Source");
    });

    test("shows the TOML source beside the configured speakers", async ({ page }) => {
        await page.locator(".tab", { hasText: "Config" }).click();

        await expect(page.locator(".config-source .cm-editor")).toBeVisible();
        await expect(page.locator(".config-source")).toContainText("[[speakers]]");

        const row = page.locator(".config-speakers-table tbody tr");
        await expect(row).toHaveCount(1);
        await expect(row).toContainText("Guide");
        // The id shows with its @ sigil (what you'd type in a script), not a bare "G".
        await expect(page.locator(".config-speakers-table tbody td").nth(1)).toHaveText("@G");
        await expect(page.locator(".config-tag-custom")).toHaveText("#role=host");
        await expect(page.locator(".config-tag-reserved")).toHaveText("##default");
    });

    test("copies a cell's text on click and confirms with a toast", async ({ page }) => {
        await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
        await page.locator(".tab", { hasText: "Config" }).click();

        await page.locator(".config-speakers-table tbody td").first().click();
        await expect(page.locator(".toast.visible")).toContainText("Copied Guide");
    });

    test("maximizes the whole tab from the tab-bar control", async ({ page }) => {
        await page.locator(".tab", { hasText: "Config" }).click();
        await expect(page.locator("body.maximized")).toHaveCount(0);
        await expect(page.locator(".config-controls")).toHaveCount(0);

        await page.locator(".tabbar-maximize").click();
        await expect(page.locator("body.maximized")).toHaveCount(1);
        // Both panes still show while maximized, and the app chrome is hidden.
        await expect(page.locator(".config-source .cm-editor")).toBeVisible();
        await expect(page.locator(".app-header")).toBeHidden();
    });

    test("hides and reopens the speakers panel from the divider", async ({ page }) => {
        await page.locator(".tab", { hasText: "Config" }).click();
        await expect(page.locator(".config-side")).toBeVisible();

        await page.locator(".config-divider .collapse-toggle").click();
        await expect(page.locator(".config-side")).toBeHidden();

        await page.locator(".config-divider .collapse-toggle").click();
        await expect(page.locator(".config-side")).toBeVisible();
    });

    test("shows both the dialogue and config file paths in the status bar", async ({ page }) => {
        await expect(page.locator("#doc-path")).toBeVisible();
        await expect(page.locator("#doc-path")).toContainText("scene.dialogue.md");
        await expect(page.locator("#config-path")).toBeVisible();
        await expect(page.locator("#config-path")).toContainText("dialogue.toml");
    });

    test("copies a status-bar path on click and confirms with a toast", async ({ page }) => {
        await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
        await page.locator("#config-path").click();
        await expect(page.locator(".toast.visible")).toContainText("Copied");
        await expect(page.locator(".toast.visible")).toContainText("dialogue.toml");
    });
});

test.describe("Config tab — no config file", () => {
    test.beforeEach(async ({ page }) => {
        await page.goto(writeReport(noConfig));
    });

    test("shows a friendly explanation instead of an empty editor", async ({ page }) => {
        await page.locator(".tab", { hasText: "Config" }).click();

        await expect(page.locator(".config-source .cm-editor")).toHaveCount(0);
        await expect(page.locator(".config-empty-state")).toContainText("dialogue.toml");
        await expect(page.locator(".config-side .config-empty")).toContainText(
            "No configured speakers",
        );
    });

    test("labels the config path as absent, without a broken path", async ({ page }) => {
        await expect(page.locator("#config-path")).toContainText("No config file");
    });
});
