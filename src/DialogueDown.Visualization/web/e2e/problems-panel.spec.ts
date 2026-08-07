import { test, expect } from "@playwright/test";
import { SAMPLE_REPORT, SAMPLE_SOURCE, writeReport } from "./report";

// Two problems on known lines of the sample document, so the assertions can name exact text.
// Line 6 (zero-based) is the "Alice:" line; line 12 is inside "The Market" scene.
const url = writeReport({
    ...SAMPLE_REPORT,
    diagnostics: [
        {
            range: { start: { line: 12, character: 0 }, end: { line: 12, character: 3 } },
            severity: 2,
            code: "DLG2003",
            message: "This scene is never reached.",
            source: "dialoguedown",
        },
        {
            range: { start: { line: 6, character: 0 }, end: { line: 6, character: 5 } },
            severity: 1,
            code: "DLG1101",
            message: "A jump must be '=> [label](target)'.",
            source: "dialoguedown",
        },
    ],
});

test("summarizes the diagnostics on the status line, from every tab", async ({ page }) => {
    await page.goto(url);
    const summary = page.locator(".diagnostic-summary");

    await expect(summary).toBeVisible();
    await expect(summary).toHaveAttribute("aria-label", /1 errors, 1 warnings, 0 infos/);

    // The point of putting it on the status line: it is the only diagnostic signal that
    // survives leaving the Source tab, where the editor's squiggles are not on screen.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await expect(summary).toBeVisible();
});

test("lists every problem, ordered by position, with its code and location", async ({ page }) => {
    await page.goto(url);

    await page.locator(".diagnostic-summary").click();
    const rows = page.locator(".problem-row");

    await expect(rows).toHaveCount(2);
    // Reported error-first, listed position-first: reading order, so the list walks the document.
    await expect(rows.nth(0)).toContainText("A jump must be");
    await expect(rows.nth(0)).toContainText("DLG1101");
    await expect(rows.nth(0)).toContainText("Ln 7, Col 1");
    await expect(rows.nth(1)).toContainText("This scene is never reached.");
    await expect(rows.nth(1)).toContainText("Ln 13, Col 1");
});

test("navigates to the offending text when a row is activated", async ({ page }) => {
    await page.goto(url);
    await page.locator(".diagnostic-summary").click();

    await page.locator(".problem-jump").first().click();

    // It lands on the Source tab with the diagnostic's range selected — the same save-safe
    // jump the node inspector uses, not a bare tab switch.
    await expect(page.locator(".tab.active")).toHaveText("Source");
    const selected = await page.evaluate(() => window.getSelection()?.toString() ?? "");
    expect(selected.length).toBeGreaterThan(0);
});

test("opens the Problems panel from the keyboard", async ({ page }) => {
    await page.goto(url);
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    await page.locator("#stages").click({ position: { x: 5, y: 5 } });
    await page.keyboard.press("p");

    await expect(page.locator(".problems-panel")).toBeVisible();
    await expect(page.locator('.drawer-tab[data-panel="problems"]')).toHaveAttribute(
        "aria-selected",
        "true",
    );
});

test("shares one drawer with the help, switching tabs rather than stacking panels", async ({
    page,
}) => {
    await page.goto(url);

    await page.locator(".diagnostic-summary").click();
    await expect(page.locator(".problems-panel")).toBeVisible();

    await page.locator('.drawer-tab[data-panel="help"]').click();

    // One surface, two contents: the help replaces the list instead of opening beside it.
    await expect(page.locator("#help-content")).toBeVisible();
    await expect(page.locator(".problems-panel")).toBeHidden();
});

test("says so when the document compiles cleanly", async ({ page }) => {
    await page.goto(writeReport({ ...SAMPLE_REPORT, source: SAMPLE_SOURCE, diagnostics: [] }));

    await page.locator(".diagnostic-summary").click();

    await expect(page.locator(".problem-row")).toHaveCount(0);
    await expect(page.locator(".problem-empty")).toContainText("No problems");
});

const narrowUrl = writeReport({
    ...SAMPLE_REPORT,
    path: "/Users/somebody/projects/a-long-project-name/scripts/chapter-one.dialogue.md",
    configuration: { speakers: [] },
    diagnostics: [
        {
            range: { start: { line: 6, character: 0 }, end: { line: 6, character: 5 } },
            severity: 1,
            code: "DLG1101",
            message: "A jump must be '=> [label](target)'.",
            source: "dialoguedown",
        },
    ],
});

test("condenses the status line to icons rather than wrapping it on a phone", async ({ page }) => {
    await page.setViewportSize({ width: 420, height: 780 });
    await page.goto(narrowUrl);
    await expect(page.locator(".diagnostic-summary")).toBeVisible();

    const line = await page.evaluate(() => {
        const root = document.querySelector(".status-line")!;
        const controls = [...root.querySelectorAll<HTMLElement>(":scope > *, .status-bar > *")]
            .filter((el) => !el.classList.contains("status-bar"))
            .filter((el) => !el.hidden && el.getBoundingClientRect().width > 0);
        // Group by row with a few pixels of slack: controls on one line still differ slightly
        // in `y` because they are baseline-aligned, not top-aligned.
        const tops = controls.map((el) => el.getBoundingClientRect().y).sort((a, b) => a - b);
        return {
            rows: tops.filter((y, i) => i === 0 || y - tops[i - 1] > 6).length,
            pathWidth: document.querySelector("#doc-path")!.getBoundingClientRect().width,
            pathTextShown:
                (document.querySelector("#doc-path .path-tail") as HTMLElement).offsetParent !==
                null,
        };
    });

    // The long paths were what wrapped the line onto three rows; their tooltips still carry them.
    expect(line.pathTextShown).toBe(false);
    expect(line.pathWidth).toBeLessThan(60);
    expect(line.rows).toBe(1);
});

test("marks a missing config when its label is hidden", async ({ page }) => {
    await page.setViewportSize({ width: 420, height: 780 });
    await page.goto(narrowUrl);

    // With the text hidden a bare gear would read the same whether a config was found or not.
    await expect(page.locator("#config-path")).toHaveClass(/config-missing/);
});

// The status line regressed at 780px once before: an older rule stacked it into a column
// below 800px while the icon-collapse only started at 720px, leaving a band that wrapped onto
// three rows. Sweep the range rather than testing one convenient width.
for (const width of [420, 600, 760, 900, 1280]) {
    test(`keeps the status line on one row at ${width}px`, async ({ page }) => {
        await page.setViewportSize({ width, height: 800 });
        await page.goto(narrowUrl);
        await expect(page.locator(".diagnostic-summary")).toBeVisible();

        const line = await page.evaluate(() => {
            const root = document.querySelector(".status-line")!;
            const controls = [...root.querySelectorAll<HTMLElement>(":scope > *, .status-bar > *")]
                .filter((el) => !el.classList.contains("status-bar"))
                .filter((el) => !el.hidden && el.getBoundingClientRect().width > 0);
            const boxes = controls.map((el) => el.getBoundingClientRect());
            const tops = boxes.map((b) => b.y).sort((a, b) => a - b);
            // Fitting on one row is not enough on its own: a control that cannot shrink stays
            // on the row and simply overlaps its neighbour instead of wrapping. Compare only
            // siblings of the same container — the status bar scrolls when it is overfull, so
            // its children legitimately extend past it and would read as false overlaps.
            const overlapsWithin = (parent: Element): boolean => {
                const kids = [...parent.children]
                    .filter((el) => !(el as HTMLElement).hidden)
                    .map((el) => el.getBoundingClientRect())
                    .filter((b) => b.width > 0)
                    .sort((a, b) => a.x - b.x);
                return kids.some((b, i) => i > 0 && b.x < kids[i - 1].right - 1);
            };
            const overlaps =
                overlapsWithin(root) || overlapsWithin(document.querySelector(".status-bar")!);
            return { rows: tops.filter((y, i) => i === 0 || y - tops[i - 1] > 6).length, overlaps };
        });

        expect(line.rows).toBe(1);
        expect(line.overlaps).toBe(false);
    });
}

test("shows the help as a glyph whose tooltip names the panel it opens", async ({ page }) => {
    await page.goto(narrowUrl);

    const help = page.locator("#help-toggle");
    await expect(help.locator(".codicon-question")).toBeVisible();
    // The word stays in the DOM as the accessible name, but costs no status-line width.
    await expect(help).toHaveAttribute("title", /Help — Using the/);
    await expect(help).toContainText("Help");
});

test("keeps the status line a constant strip as the window widens", async ({ page }) => {
    const heightAt = async (width: number): Promise<number> => {
        await page.setViewportSize({ width, height: 800 });
        await page.goto(narrowUrl);
        await expect(page.locator(".diagnostic-summary")).toBeVisible();
        return page.evaluate(() =>
            Math.round(document.querySelector(".status-line")!.getBoundingClientRect().height),
        );
    };

    // Compare widths that show the same controls. A path button legitimately grows when its
    // label comes back at a wider window; what must not happen is the strip growing purely
    // because the framework scaled the root font size with the viewport.
    expect(await heightAt(1000)).toBe(await heightAt(1400));
    expect(await heightAt(430)).toBe(await heightAt(700));
});

test("leaves no underline on the tab you switched away from", async ({ page }) => {
    await page.goto(narrowUrl);
    await page.locator(".diagnostic-summary").click();
    await page.locator('.drawer-tab[data-panel="help"]').click();

    await page.locator('.drawer-tab[data-panel="problems"]').click();

    // The framework animates border colours on buttons, so the tab just left kept a fading
    // underline that read as a second selected tab.
    const help = page.locator('.drawer-tab[data-panel="help"]');
    await expect(help).toHaveCSS("border-bottom-color", "rgba(0, 0, 0, 0)");
    await expect(help).toHaveCSS("transition-duration", "0s");
});

test("opens the panel above the status line, which stays pinned to the bottom", async ({
    page,
}) => {
    await page.goto(narrowUrl);

    await page.locator(".diagnostic-summary").click();

    const placement = await page.evaluate(() => {
        const drawer = document.querySelector("#footer-drawer")!.getBoundingClientRect();
        const line = document.querySelector(".status-line")!.getBoundingClientRect();
        const footer = document.querySelector(".app-footer")!.getBoundingClientRect();
        return {
            drawerAbove: drawer.bottom <= line.top + 1,
            lineAtFooterBottom: Math.abs(line.bottom - footer.bottom) < 2,
        };
    });

    expect(placement).toEqual({ drawerAbove: true, lineAtFooterBottom: true });
});
