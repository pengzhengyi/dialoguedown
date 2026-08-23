import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { writeReport, SAMPLE_STAGES } from "./report";
import type { Report } from "../src/model";

// A report carrying a compiled playbook: one named speaker with an id and a tag, plus the
// anonymous default every script has. Enough to show both tables and the read-only editor.
const compiled: Report = {
    source: "# Market\n\nGuide: Hello.\n",
    path: "/proj/scene.dialogue.md",
    stages: [
        ...SAMPLE_STAGES,
        { title: "Dialogue Graph", description: "The runtime graph.", nodes: [], edges: [] },
    ],
    playbook: {
        json: '{\n  "format": {\n    "version": 0\n  },\n  "script": "scene.dialogue.md"\n}',
        metadata: {
            script: "scene.dialogue.md",
            formatVersion: 0,
            requires: ["core"],
            uses: [],
            entry: 0,
            nodeCount: 4,
            anchorCount: 1,
        },
        speakers: [
            { id: "guide", name: "Guide", default: false, tags: ["role=host"] },
            { default: true, tags: [] },
        ],
    },
};

// A script that did not compile produces no playbook, so the tab explains itself instead.
const halted: Report = {
    source: "=> [Gone](#missing)\n",
    path: "/proj/scene.dialogue.md",
    stages: SAMPLE_STAGES,
    playbook: {
        speakers: [],
        unavailable: "A playbook is written only for a script that compiles without errors.",
    },
};

const playbookTab = "#tabs .tab:last-child";

test.describe("Playbook tab — a compiled script", () => {
    test.beforeEach(async ({ page }) => {
        await page.goto(writeReport(compiled));
    });

    test("comes last, after the graph the playbook is compiled from", async ({ page }) => {
        const titles = await page.locator("#tabs .tab").allTextContents();

        expect(titles.at(-1)).toBe("Playbook");
        expect(titles.at(-2)).toBe("Dialogue Graph");
    });

    test("shows the serialized playbook in an editor a reader cannot edit", async ({ page }) => {
        await page.click(playbookTab);

        const content = page.locator(".playbook-source .cm-content");
        await expect(content).toContainText('"script"');
        await expect(content).toHaveAttribute("aria-readonly", "true");

        // Typing into it is the reader's real path to an edit; the buffer must not move.
        const before = await content.textContent();
        await content.click();
        await page.keyboard.type("tampered");
        await expect(content).toHaveText(before ?? "");
    });

    test("summarizes the header and the speakers beside the document", async ({ page }) => {
        await page.click(playbookTab);

        await expect(page.locator(".playbook-metadata-table")).toContainText("scene.dialogue.md");
        await expect(page.locator(".playbook-metadata-table")).toContainText("core");

        const rows = page.locator(".playbook-speakers-table tbody tr");
        await expect(rows).toHaveCount(2);
        await expect(rows.first()).toContainText("Guide");
        await expect(rows.nth(1).locator(".playbook-anonymous")).toHaveText("(anonymous)");
    });

    test("hides the tables to give the playbook the full width, and brings them back", async ({
        page,
    }) => {
        await page.click(playbookTab);
        const toggle = page.locator(".playbook-divider .collapse-toggle");

        await toggle.click();
        await expect(page.locator(".playbook-side")).toBeHidden();

        await toggle.click();
        await expect(page.locator(".playbook-side")).toBeVisible();
    });

    test("has no accessibility violations", async ({ page }) => {
        await page.click(playbookTab);
        await expect(page.locator(".playbook-source .cm-editor")).toBeVisible();

        expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
    });

    test("explains the playbook rather than the Source editor in the help panel", async ({
        page,
    }) => {
        await page.click(playbookTab);
        await page.click("#help-toggle");

        await expect(page.locator("#help-content")).toContainText("read-only");
    });
});

test.describe("Playbook tab — a script that did not compile", () => {
    test("says why there is no playbook instead of showing an empty editor", async ({ page }) => {
        await page.goto(writeReport(halted));

        await page.click(playbookTab);

        await expect(page.locator(".playbook-source .cm-editor")).toHaveCount(0);
        await expect(page.locator(".playbook-empty-state")).toContainText(
            "compiles without errors",
        );
    });
});
