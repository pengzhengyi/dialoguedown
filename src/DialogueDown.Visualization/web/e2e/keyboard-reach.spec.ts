import { test, expect, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { writeReport } from "./report";
import type { Report } from "../src/model";

/**
 * A table of the things a writer lifts into a script: an `@id`, an anchor, and a tag.
 *
 * Each is copied by pressing it, which makes it a control — and a control has to be reachable
 * without a mouse. These tests use the real keyboard, because that is the only place the claim
 * can be checked: jsdom does not turn Enter into a click the way a browser does.
 */
const report: Report = {
    source: "# The Market\n\nGuide @guide: Welcome.\n",
    stages: [
        {
            title: "Semantic Model",
            description: "The semantic model the analyzer resolves.",
            nodes: [{ id: "n0", label: "Document root", attributes: [], category: "document" }],
            edges: [],
            tables: [
                {
                    title: "Speakers",
                    columns: ["Name", "@id", "Tags"],
                    emptyText: "No speakers are declared.",
                    rows: [
                        {
                            cells: [
                                { text: "Guide" },
                                { text: "@guide", copyable: true },
                                { text: "#wise", tags: [{ name: "wise", reserved: false }] },
                            ],
                        },
                    ],
                },
                {
                    title: "Anchors",
                    columns: ["Anchor", "Scene"],
                    emptyText: "No anchors appear in this script.",
                    rows: [
                        {
                            cells: [
                                { text: "#the-market", copyable: true },
                                { text: "The Market" },
                            ],
                        },
                        {
                            cells: [
                                { text: "#the-square", copyable: true },
                                { text: "The Square" },
                            ],
                        },
                    ],
                },
            ],
        },
    ],
};

const url = writeReport(report);

async function showTables(page: Page): Promise<void> {
    await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
    await page.goto(url);
    await page.locator(".tab", { hasText: "Semantic Model" }).click();
    await expect(page.locator("td.dd-copy button.cell-action").first()).toBeVisible();
}

/** What the clipboard now holds. */
function clipboard(page: Page): Promise<string> {
    return page.evaluate(() => navigator.clipboard.readText());
}

test("copies an identifier with Enter, so the keyboard can do what the mouse can", async ({
    page,
}) => {
    await showTables(page);

    await page.locator("td.dd-copy button.cell-action").first().focus();
    await page.keyboard.press("Enter");

    await expect(page.locator(".toast.visible")).toContainText("Copied @guide");
    expect(await clipboard(page)).toBe("@guide");
});

test("copies with Space too, because a button answers both", async ({ page }) => {
    await showTables(page);

    await page.locator("td.dd-copy button.cell-action").nth(1).focus();
    await page.keyboard.press(" ");

    expect(await clipboard(page)).toBe("#the-market");
});

test("copies a tag capsule from the keyboard", async ({ page }) => {
    await showTables(page);

    await page.locator("button.dd-tag").first().focus();
    await page.keyboard.press("Enter");

    expect(await clipboard(page)).toBe("#wise");
});

test("reaches the next identifier with Tab, so the cells are in the reading order", async ({
    page,
}) => {
    // Focusable is not the same as reachable. Tab must walk from one actionable cell to the next
    // in the order they are written, or a reader has to hunt for them.
    await showTables(page);
    const anchors = page.locator("table:has-text('Anchor') td.dd-copy button.cell-action");

    await anchors.first().focus();
    await page.keyboard.press("Tab");

    await expect(anchors.nth(1)).toBeFocused();
});

test("shows a focus ring, so a keyboard reader can see where they are", async ({ page }) => {
    await showTables(page);
    const anchors = page.locator("table:has-text('Anchor') td.dd-copy button.cell-action");

    // Reached by Tab rather than by a script's `focus()`, because `:focus-visible` — and so the
    // ring — is exactly the browser's judgement about whether the keyboard did the focusing.
    await anchors.first().focus();
    await page.keyboard.press("Tab");
    await expect(anchors.nth(1)).toBeFocused();

    const outline = await anchors.nth(1).evaluate((el) => getComputedStyle(el).outlineWidth);
    expect(outline).not.toBe("0px");
});

test("leaves the cell a cell, so the table is still a table", async ({ page }) => {
    // The button is nested inside the `<td>` rather than replacing its role. A cell given a
    // button role stops being a cell, and a screen reader walking the grid loses the table.
    await showTables(page);
    const cell = page.locator("td.dd-copy").first();

    await expect(cell).not.toHaveAttribute("role", "button");
    await expect(cell.locator("button.cell-action")).toHaveCount(1);
});

test("says what pressing the control does, not merely what it shows", async ({ page }) => {
    await showTables(page);

    await expect(page.locator("td.dd-copy button.cell-action").first()).toHaveAttribute(
        "aria-label",
        "Copy @guide",
    );
});

test("has no accessibility violations with the tables shown", async ({ page }) => {
    await showTables(page);

    const results = await new AxeBuilder({ page }).analyze();
    expect(results.violations).toEqual([]);
});
