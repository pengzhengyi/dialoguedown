import { test, expect, type Page } from "@playwright/test";
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
        json:
            '{\n  "format": {\n    "version": 0,\n    "requires": [\n      "core"\n    ]\n  },\n' +
            '  "script": "scene.dialogue.md"\n}',
        metadata: {
            script: "scene.dialogue.md",
            formatVersion: 0,
            schemaUrl: "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
            requires: ["core"],
            uses: [],
            entry: 0,
            nodeCount: 4,
            anchorCount: 1,
        },
        anchors: [{ name: "the-tavern", node: 0 }],
        speakers: [
            {
                id: "guide",
                name: "Guide",
                default: false,
                tags: [{ name: "role", value: "host", reserved: false }],
            },
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
        anchors: [],
        speakers: [],
        unavailable: "A playbook is written only for a script that compiles without errors.",
    },
};

const playbookTab = "#tabs .tab:last-child";

/** One named table panel in the Playbook tab's right pane. */
const panel = (page: Page, title: string) =>
    page
        .locator(".playbook-side .table-panel")
        .filter({ has: page.locator(".table-panel-title", { hasText: new RegExp(`^${title}$`) }) });

test.describe("Playbook tab — a compiled script", () => {
    test.beforeEach(async ({ page }) => {
        await page.goto(writeReport(compiled));
    });

    test("comes last, after the graph the playbook is compiled from", async ({ page }) => {
        const titles = await page.locator("#tabs .tab").allTextContents();

        expect(titles.at(-1)).toBe("Playbook");
        expect(titles.at(-2)).toBe("Dialogue Graph");
    });

    test("gives a key, a string, and a number each their own hue", async ({ page }) => {
        await page.click(playbookTab);
        const content = page.locator(".playbook-source .cm-content");

        const colorOf = (text: string) =>
            content
                .getByText(text, { exact: false })
                .first()
                .evaluate((node) => getComputedStyle(node).color);

        // A key and a string value are both quoted, so only a real parser can tell them apart —
        // the previous tokenizer emitted one token for both and could not have passed this.
        const [key, string, number] = [
            await colorOf('"script"'),
            await colorOf('"scene.dialogue.md"'),
            await colorOf("0"),
        ];

        expect(new Set([key, string, number]).size).toBe(3);
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

    test("summarizes the playbook in the report's own table panels", async ({ page }) => {
        await page.click(playbookTab);

        // Same chrome as the Semantic tab's tables: a caret, a search, and a live row count.
        await expect(page.locator(".playbook-side .table-panel-title")).toHaveText([
            "Playbook",
            "Speakers",
            "Anchors",
        ]);
        await expect(panel(page, "Playbook")).toContainText("scene.dialogue.md");
        await expect(panel(page, "Speakers").locator("tbody tr")).toHaveCount(2);
        await expect(panel(page, "Anchors")).toContainText("#the-tavern");
    });

    test("filters a table down through its search box", async ({ page }) => {
        await page.click(playbookTab);
        const speakers = panel(page, "Speakers");
        await speakers.locator(".table-panel-search").click();

        await speakers.getByRole("searchbox").fill("Guide");

        await expect(speakers.locator("tbody tr")).toHaveCount(1);
        await expect(speakers.locator(".table-panel-count")).toHaveText("1");
    });

    test("remembers its panels apart from the Semantic tab's same-named ones", async ({ page }) => {
        // Both tabs show Speakers and Anchors; one storage key would collapse both at once.
        await page.click(playbookTab);

        await panel(page, "Speakers").locator(".table-panel-toggle").click();

        const keys = await page.evaluate(() => Object.keys(window.localStorage));
        expect(keys).toContain("dd-playbook-panel-speakers");
        expect(keys).not.toContain("dd-sem-panel-speakers");
    });

    test("collapses a table panel to its title", async ({ page }) => {
        await page.click(playbookTab);
        const anchors = panel(page, "Anchors");

        await anchors.locator(".table-panel-toggle").click();

        await expect(anchors.locator(".table-panel-body")).toBeHidden();
        await expect(anchors.locator(".table-panel-toggle")).toHaveAttribute(
            "aria-expanded",
            "false",
        );
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

    test("explains a property from the published schema on hover", async ({ page }) => {
        await page.click(playbookTab);

        // `requires` is described by the format itself, so the tooltip quotes the schema.
        await page.locator(".playbook-source .cm-content").getByText('"requires"').hover();

        const tip = page.locator(".playbook-hover");
        await expect(tip).toBeVisible();
        await expect(tip.locator(".playbook-hover-path")).toHaveText("format/requires");
        await expect(tip.locator(".playbook-hover-text")).toContainText("Capabilities a runtime");
    });

    test("washes in the stretch a hovered description applies to", async ({ page }) => {
        await page.click(playbookTab);
        const wash = page.locator(".playbook-source .dd-jump-preview");
        await expect(wash).toHaveCount(0);

        // `format` opens an object, so the rule covers the whole block, not just its name.
        await page.locator(".playbook-source .cm-content").getByText('"format"').hover();

        await expect(wash.first()).toBeVisible();
        // A mark spanning lines is drawn one span per line, so the block is their union.
        expect((await wash.allTextContents()).join("\n")).toContain('"requires"');
        // It has to actually paint: a decoration whose style is scoped to another pane is
        // present, sized, and "visible" to a selector while washing nothing at all.
        const painted = await wash
            .first()
            .evaluate((node) => getComputedStyle(node).backgroundColor);
        expect(painted).not.toBe("rgba(0, 0, 0, 0)");

        // The wash lifts with the tooltip rather than lingering over the document.
        await page.locator(".playbook-side").hover();
        await expect(page.locator(".playbook-hover")).toHaveCount(0);
        await expect(wash).toHaveCount(0);
    });

    test("marks only the line when the property holds a scalar", async ({ page }) => {
        await page.click(playbookTab);

        await page.locator(".playbook-source .cm-content").getByText('"script"').hover();

        const wash = page.locator(".playbook-source .dd-jump-preview");
        await expect(wash).toHaveCount(1);
        await expect(wash).toContainText("scene.dialogue.md");
    });

    test("folds an object from the gutter, the way the other editors do", async ({ page }) => {
        await page.click(playbookTab);
        const content = page.locator(".playbook-source .cm-content");
        await expect(content).toContainText('"requires"');

        // The gutter markers the grammar's own fold ranges produce. CodeMirror keeps a hidden
        // measurement element in the gutter, so only the drawn ones are counted.
        const markers = page.locator(".playbook-source .cm-fold-marker:visible");
        await expect(markers).not.toHaveCount(0);

        // Markers run in line order: the root object, then `"format": {`, then its array.
        await markers.nth(1).click();

        // The block's members are hidden while its own line, and the rest of the document, stay.
        await expect(content).not.toContainText('"requires"');
        await expect(content).toContainText('"format"');
        await expect(content).toContainText('"script"');
        await expect(page.locator(".playbook-source .cm-foldPlaceholder")).toBeVisible();

        await page.locator(".playbook-source .cm-foldPlaceholder").click();
        await expect(content).toContainText('"requires"');
    });

    test("links out to the published schema", async ({ page }) => {
        await page.click(playbookTab);

        const link = page.locator(".playbook-schema-link");
        await expect(link).toHaveText("playbook-0.schema.json");
        await expect(link).toHaveAttribute("rel", "noopener noreferrer");
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
