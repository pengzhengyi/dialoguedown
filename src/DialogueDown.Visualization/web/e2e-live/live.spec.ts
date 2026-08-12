import { test, expect } from "@playwright/test";
import { writeFileSync, rmSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { LIVE_DOC, INITIAL_SOURCE } from "./fixture.mjs";

// A 1×1 PNG, written next to the document so a relative image link can resolve.
const PNG_1x1 = Buffer.from(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "base64",
);

// These tests share one server and document (the config runs them serially).
// Each test restores the document to its initial content first and waits for the
// page to reflect it, so tests are order-independent.
test.beforeEach(async ({ page }) => {
    writeFileSync(LIVE_DOC, INITIAL_SOURCE);
    await page.goto("/");
    await expect(page.locator(".source-pane .cm-content")).toContainText("Original Scene");
});

test("serves a view report bound to the document", async ({ page }) => {
    // The payload is a served session, so the tabs render. Config leads (it always has a
    // configuration context here), while the report still opens on Source.
    await expect(page.locator(".tab")).toHaveCount(7); // Config + Source + Markdown/Dialogue/Desugared AST + Semantic Model + Dialogue Graph
    await expect(page.locator(".tab").first()).toHaveText("Config");
    await expect(page.locator(".tab.active")).toHaveText("Source");
    // The View/Edit toggle is shown, starting in View.
    await expect(page.locator('.mode-toggle-option[data-mode="view"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );
    // The document path is shown in the status bar.
    await expect(page.locator("#doc-path")).toBeVisible();
});

test("highlights the dialogue speaker from the compiler's semantic tokens", async ({ page }) => {
    // The served payload carries the compiler's projected tokens, so the editor colors the
    // speaker of "Alice: The original line." with no browser-side lexer.
    await expect(page.locator(".source-pane .dd-tok-speaker-name").first()).toContainText("Alice");

    // A hot reload re-highlights in place: the new speaker is colored after the SSE push.
    writeFileSync(LIVE_DOC, "# New Scene\n\nBob: A different speaker.\n");
    await expect(page.locator(".source-pane .dd-tok-speaker-name").first()).toContainText("Bob");
});

test("serves images alongside the document so relative links resolve", async ({ page }) => {
    const assets = join(dirname(LIVE_DOC), "assets");
    mkdirSync(assets, { recursive: true });
    writeFileSync(join(assets, "pic.png"), PNG_1x1);
    writeFileSync(LIVE_DOC, "# Gallery\n\n![a picture](assets/pic.png)\n");

    const image = page.locator(".source-preview img");
    await expect(image).toBeVisible();
    // The image actually loaded (served by the live server), not a broken link.
    await expect
        .poll(async () => image.evaluate((img: HTMLImageElement) => img.naturalWidth))
        .toBeGreaterThan(0);

    rmSync(assets, { recursive: true, force: true });
});

test("hot-reloads the report when the document changes on disk", async ({ page }) => {
    writeFileSync(LIVE_DOC, "# Rewritten Scene\n\nBob: A brand new line.\n");

    // The server watches the file, recompiles, and pushes over SSE; the client
    // rebuilds in place. No reload/navigation here — the DOM updates itself.
    await expect(page.locator(".source-pane .cm-content")).toContainText("Rewritten Scene");
    await expect(page.locator(".source-preview")).toContainText("A brand new line");
});

test("keeps the active tab across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active")).not.toHaveClass(/source-stage/);

    writeFileSync(LIVE_DOC, "# Another Scene\n\nAlice: Still on the graph tab.\n");

    // After the rebuild the Markdown AST tab is still the active one.
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
    await expect(page.locator("section.stage.active g.node")).not.toHaveCount(0);
});

test("keeps a graph's zoom across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    const viewport = page.locator("section.stage.active svg.tree > g").first();
    const zoomIn = page.locator("section.stage.active .zoom-controls button", { hasText: "+" });

    // Reading the transform first lets the initial default framing settle before we zoom.
    const framed = await viewport.getAttribute("transform");
    await zoomIn.click();
    await zoomIn.click();
    const zoomed = await viewport.getAttribute("transform");
    expect(zoomed).not.toEqual(framed);

    // A disk change rebuilds the graph tabs in place (View mode auto-updates).
    writeFileSync(LIVE_DOC, "# Zoom Scene\n\nAlice: The graph is rebuilt but stays put.\n");
    await expect(page.locator(".source-preview")).toContainText("stays put");

    // The rebuilt Markdown AST graph kept the zoom it had before the reload, rather
    // than snapping back to the default framing.
    await expect(page.locator("section.stage.active svg.tree > g").first()).toHaveAttribute(
        "transform",
        zoomed ?? "",
    );
});

test("keeps a graph's collapsed nodes across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    // Overlays (legend, zoom, detail) sit above the SVG; let the collapse click through.
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });

    const nodes = page.locator("section.stage.active g.node");
    const collapsed = page.locator("section.stage.active g.node.collapsed");
    await expect(nodes.first()).toBeVisible();
    await expect(collapsed).toHaveCount(0);

    // Collapse the root Document node, hiding its whole subtree.
    await nodes.filter({ hasText: "Document" }).first().locator("circle").first().click();
    await expect(collapsed).toHaveCount(1);
    await expect(nodes).toHaveCount(1); // only the collapsed root remains

    // A disk change rebuilds the graph tabs in place (View mode auto-updates).
    writeFileSync(LIVE_DOC, INITIAL_SOURCE + "\n## Added Section\n\nBob: A brand new line.\n");
    // Scope to the Source tab's preview — the node inspector now has its own preview too.
    await expect(page.locator(".source-stage .source-preview")).toContainText("brand new line");

    // The rebuilt graph kept the Document node collapsed rather than expanding every
    // node on reload — the new section stays hidden under the still-collapsed root.
    await expect(collapsed).toHaveCount(1);
    await expect(nodes).toHaveCount(1);
});

test("an untouched graph inherits the current zoom; an adjusted one keeps its own", async ({
    page,
}) => {
    const zoom = () => page.locator("section.stage.active .zoom-input");

    // Set the Markdown AST graph to a distinct zoom (pins its own camera).
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await zoom().fill("150");
    await zoom().press("Enter");
    await expect(zoom()).toHaveValue("150");

    // The untouched Dialogue AST tab inherits the current 150%; then give it its own 80%.
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(zoom()).toHaveValue("150");
    await zoom().fill("80");
    await zoom().press("Enter");
    await expect(zoom()).toHaveValue("80");

    // Coming straight from Dialogue, the untouched Desugared AST inherits the current 80%.
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await expect(zoom()).toHaveValue("80");

    // Each adjusted graph kept its own: Markdown 150%, Dialogue 80%.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(zoom()).toHaveValue("150");
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(zoom()).toHaveValue("80");
});

test("shows a banner when the document is deleted", async ({ page }) => {
    await expect(page.locator("#live-banner")).toBeHidden();

    rmSync(LIVE_DOC);

    await expect(page.locator("#live-banner")).toBeVisible();
    await expect(page.locator("#live-banner")).toContainText("not found");
});

test("toggles to Edit and back to View, reconfiguring the one editor in place", async ({
    page,
}) => {
    const view = page.locator('.mode-toggle-option[data-mode="view"]');
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');

    // Starts in View: read-only (edits are rejected) and no Save button.
    await expect(view).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeHidden();
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.type("XX");
    await expect(page.locator(".source-pane .cm-content")).not.toContainText("XX");

    // Switch to Edit: editable and the Save button appears; the buffer is unchanged.
    await edit.click();
    await expect(edit).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeVisible();
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.type("YY");
    await expect(page.locator(".source-pane .cm-content")).toContainText("YY"); // edits land now

    // Switch back to View (accepting the discard prompt) — read-only again.
    page.once("dialog", (dialog) => void dialog.accept());
    await view.click();
    await expect(view).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeHidden();
});

test("accents blue in View and green in Edit, and keeps the footer on one aligned line", async ({
    page,
}) => {
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');
    const activeTab = page.locator("button.tab.active");
    const GREEN = "rgb(21, 128, 61)"; // #15803d, the Save-button green

    // View: the document reports view mode and the accent is not the Edit green.
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "view");
    await expect(activeTab).not.toHaveCSS("border-bottom-color", GREEN);

    // Edit: the accent switches to green on the active tab and the pressed toggle.
    await edit.click();
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "edit");
    await expect(activeTab).toHaveCSS("border-bottom-color", GREEN);
    await expect(edit).toHaveCSS("background-color", GREEN);

    // The status line keeps the toggle, path, and help toggle vertically centered together.
    const centers = await page.evaluate(() => {
        const center = (selector: string): number => {
            const rect = document.querySelector(selector)!.getBoundingClientRect();
            return rect.top + rect.height / 2;
        };
        return {
            toggle: center(".mode-toggle"),
            path: center("#doc-path"),
            help: center("#help-toggle"),
        };
    });
    expect(Math.abs(centers.toggle - centers.path)).toBeLessThan(2);
    expect(Math.abs(centers.toggle - centers.help)).toBeLessThan(2);
});

test("keeps the View/Edit toggle enabled on graph tabs so editing can begin there", async ({
    page,
}) => {
    const toggle = page.locator(".mode-toggle");
    const view = page.locator('.mode-toggle-option[data-mode="view"]');

    // Source tab is active first: the toggle is interactive.
    await expect(view).toBeEnabled();

    // A graph tab keeps the toggle interactive too — a node can be edited there, so mode is
    // no longer confined to the Source tab.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await expect(view).toBeEnabled();
    await expect(toggle).not.toHaveAttribute("aria-disabled", "true");

    // Back on Source it is still interactive.
    await page.locator(".tab", { hasText: "Source" }).click();
    await expect(view).toBeEnabled();
});

test("Zen mode hides the Explorer sidebar on a served report", async ({ page }) => {
    await expect(page.locator("#app")).toHaveClass(/has-explorer/);
    await expect(page.locator("#explorer")).toBeVisible();

    await page.keyboard.press("z");

    // The Explorer's reveal rule is id-heavy, so Zen must outrank it rather than merely
    // follow it in source order.
    await expect(page.locator("#explorer")).toBeHidden();
    await expect(page.locator("#explorer-resizer")).toBeHidden();

    await page.keyboard.press("Escape");
    await expect(page.locator("#explorer")).toBeVisible();
});

test.describe("on a phone-sized window", () => {
    test.use({ viewport: { width: 390, height: 780 } });

    test("turns the Explorer seam with the stacked layout", async ({ page }) => {
        await expect(page.locator("#app")).toHaveClass(/has-explorer/);

        const stacked = await page.evaluate(() => {
            const app = getComputedStyle(document.querySelector("#app")!).flexDirection;
            const seam = document.querySelector("#explorer-resizer")!.getBoundingClientRect();
            const explorer = document.querySelector("#explorer")!.getBoundingClientRect();
            const stage = document.querySelector("#stages")!.getBoundingClientRect();
            return {
                app,
                seamHeight: seam.height,
                seamSpansWidth: seam.width > 100,
                explorerHeight: explorer.height,
                stageHeight: stage.height,
                windowHeight: window.innerHeight,
            };
        });

        expect(stacked.app).toBe("column");
        // The seam kept `width: 1px` in a column and measured 1x0px, so its collapse toggle
        // could not be reached at all.
        expect(stacked.seamHeight).toBeGreaterThan(0);
        expect(stacked.seamSpansWidth).toBe(true);
        // Content-sized rather than a rigid 15rem: this project holds a handful of files.
        expect(stacked.explorerHeight).toBeLessThanOrEqual(stacked.windowHeight * 0.25 + 1);
        // Which leaves the stage the bulk of the column instead of a quarter of it.
        expect(stacked.stageHeight).toBeGreaterThan(stacked.explorerHeight);
    });

    test("hides and restores the Explorer from the turned seam", async ({ page }) => {
        const toggle = page.locator("#explorer-resizer .collapse-toggle");
        await expect(page.locator("#explorer")).toBeVisible();

        await toggle.click();
        await expect(page.locator("#explorer")).toBeHidden();

        await toggle.click();
        await expect(page.locator("#explorer")).toBeVisible();
    });
});

test("renders the dialogue graph, including a cycle and unreachable content", async ({ page }) => {
    // A jump back to the scene it is inside makes the flow cyclic, and the line after an
    // unconditional divert is unreachable. Both are ordinary in a graph but not in a tree, so
    // this is the case a tree-shaped renderer fails on.
    writeFileSync(
        LIVE_DOC,
        [
            "# Loop",
            "",
            "Alice: Around again.",
            "",
            "=> [Loop](#loop)",
            "",
            "Alice: Nobody reads this.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();

    // The stage renders rather than reporting a layout failure.
    await expect(page.locator("section.stage.error")).toHaveCount(0);
    const stage = page.locator("section.stage.active");
    await expect(stage.locator("g.node")).not.toHaveCount(0);
    await expect(stage.locator('g.node:has-text("Around again")').first()).toBeVisible();
    await expect(stage.locator('g.node:has-text("Nobody reads this")').first()).toBeVisible();
});

test("keeps every edge clear of the words it runs past", async ({ page }) => {
    // A node writes its label to the right of its dot, so a line leaving from the dot would strike
    // through the very text it belongs to, and a long cross-link would lie across every row it
    // passes. Both make a busy graph unreadable, so neither is allowed.
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way do you go, traveler?",
            "",
            "- Alice: Through the gate, quickly.",
            "",
            "  Guide: It swings open before you.",
            "",
            "- Alice: Over the wall, then.",
            "",
            "  Guide: You climb, slowly, and drop.",
            "",
            "Guide: You are inside the courtyard now.",
            "",
            '> `if` `"Rich"?`',
            ">",
            "> Guard: Welcome, my lord and master.",
            ">",
            "> `else`",
            ">",
            "> Guard: State your business, stranger.",
            "",
            "=> [The Gate](#the-gate)",
            "",
            "Guide: Nobody ever reaches this line.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    await expect(page.locator("section.stage.active g.node")).not.toHaveCount(0);

    const struck = await page.evaluate(() => {
        const stage = document.querySelector("section.stage.active")!;
        const paths = [...stage.querySelectorAll<SVGPathElement>("path.link")];
        const crossings: string[] = [];
        for (const label of stage.querySelectorAll<SVGTextElement>("g.node text.label")) {
            const box = label.getBoundingClientRect();
            const middle = (box.top + box.bottom) / 2;
            for (const path of paths) {
                const length = path.getTotalLength();
                const matrix = path.getScreenCTM()!;
                for (let step = 0; step <= 60; step++) {
                    const point = path.getPointAtLength((length * step) / 60);
                    const x = matrix.a * point.x + matrix.c * point.y + matrix.e;
                    const y = matrix.b * point.x + matrix.d * point.y + matrix.f;
                    if (x > box.left + 2 && x < box.right - 2 && Math.abs(y - middle) < 5) {
                        crossings.push(
                            `${label.textContent} / ${path.querySelector("title")?.textContent}`,
                        );
                        break;
                    }
                }
            }
        }
        return crossings;
    });

    expect(struck).toEqual([]);
});

test("gives each cross-link a lane of its own, so two never share a line", async ({ page }) => {
    // Two routes drawn along the same y would read as one line with mysterious branches. Each
    // takes its own lane instead, the shorter hop nearer the drawing.
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way do you go, traveler?",
            "",
            "- Alice: Through the gate, quickly.",
            "",
            "  Guide: It swings open before you.",
            "",
            "- Alice: Over the wall, then.",
            "",
            "  Guide: You climb, slowly, and drop.",
            "",
            "Guide: You are inside the courtyard now.",
            "",
            "=> [The Gate](#the-gate)",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    await expect(page.locator("section.stage.active path.reference")).not.toHaveCount(0);

    const lanes = await page.locator("section.stage.active path.reference").evaluateAll((paths) =>
        paths.map((path) => {
            const box = (path as SVGPathElement).getBBox();
            return Math.round(box.y + box.height);
        }),
    );

    expect(new Set(lanes).size).toBe(lanes.length);
});

test("draws a succession solidly even where it is routed as a cross-link", async ({ page }) => {
    // Where a node is reached twice, the second arrival is drawn as a cross-link. It is still an
    // ordinary succession, so it must look like one rather than borrowing the reference dash.
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way?",
            "",
            "- Alice: Left.",
            "",
            "- Alice: Right.",
            "",
            "Guide: You are inside.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();

    const succession = page
        .locator("section.stage.active path.reference")
        .filter({ has: page.locator("title", { hasText: "Succession" }) })
        .first();
    await expect(succession).toHaveCount(1);
    expect(await succession.evaluate((path) => getComputedStyle(path).strokeDasharray)).toBe(
        "none",
    );
});

test("lists a node's routes in and out, and walks them", async ({ page }) => {
    // The drawing shows every edge but cannot name them all at once. The inspector names the ones
    // that touch the node the reader asked about, and each row is a way to go there.
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way?",
            "",
            "- Alice: Left.",
            "",
            "- Alice: Right.",
            "",
            "Guide: You are inside.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");
    await stage
        .locator("g.node")
        .filter({ hasText: "You are inside" })
        .first()
        .locator("circle")
        .dispatchEvent("click");

    await expect(page.locator("#detail-body h4").first()).toHaveText("Incoming");
    const incoming = page.locator("#detail-body table.neighbors").first();
    await expect(incoming.locator("thead th")).toHaveText(["Source", "Edge"]);
    await expect(incoming.locator("button.neighbor")).toHaveText(["Alice: Left.", "Alice: Right."]);
    await expect(incoming.locator("button.route")).toHaveText(["Succession", "Succession"]);

    // Walking a row selects that node — in the drawing, not only in the panel.
    await incoming.locator("button.neighbor").first().click();
    await expect(page.locator("#detail-title")).toContainText("Alice: Left.");
    await expect(stage.locator("g.node.selected text.label")).toHaveText("Alice: Left.");
});

test("leaves the neighbor lists out of a stage that is a tree, not a flow", async ({ page }) => {
    writeFileSync(LIVE_DOC, ["# The Gate", "", "Guide: Hello.", ""].join("\n"));
    await page.goto("/");
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await page
        .locator("section.stage.active g.node")
        .nth(1)
        .locator("circle")
        .dispatchEvent("click");

    await expect(page.locator("#detail-body table.neighbors")).toHaveCount(0);
});

test("stamps the not-reached line with crosses rather than a fourth dash pattern", async ({
    page,
}) => {
    writeFileSync(
        LIVE_DOC,
        [
            "# Loop",
            "",
            "Alice: Around again.",
            "",
            "=> [Loop](#loop)",
            "",
            "Alice: Nobody reads this.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();

    const barred = page
        .locator("section.stage.active path.link")
        .filter({ has: page.locator("title", { hasText: "Not reached" }) })
        .first();
    await expect(barred).toHaveAttribute("marker-mid", /url\(#tick-/);
    // A curve has only two ends; the glyph needs vertices to stand on, so the line is resampled.
    const vertices = await barred.evaluate(
        (path) => (path.getAttribute("d") ?? "").split("L").length - 1,
    );
    expect(vertices).toBeGreaterThan(2);
});

test("opens a route from the drawing, and walks off it to either end", async ({ page }) => {
    writeFileSync(
        LIVE_DOC,
        ["# The Gate", "", "Guide: Which way?", "", "Guide: You are inside.", ""].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");

    await stage.locator("path.edge-hit").first().dispatchEvent("click");

    await expect(page.locator("#detail-title")).toHaveText("Succession");
    await expect(page.locator("#detail-body .route-meaning")).toContainText("natural order");
    await expect(page.locator("#detail-body table.neighbors th")).toHaveText([
        "Source",
        "Destination",
    ]);

    await page.locator("#detail-body button.neighbor").last().click();
    await expect(stage.locator("g.node.selected text.label")).toHaveText("Guide: You are inside.");
});

test("draws a scene as a band around its nodes rather than a line under each", async ({ page }) => {
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way?",
            "",
            "- Alice: Left.",
            "",
            "# The Hall",
            "",
            "Guide: Inside.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");

    await expect(stage.locator("g.region text.region-name")).toHaveText(["The Gate", "The Hall"]);
    // The name is written once, on the band — never repeated under a node.
    const attributes = await stage.locator("g.node text.attr").allTextContents();
    expect(attributes.filter((text) => text.startsWith("scene"))).toEqual([]);
});

test("opens a region from its band, and names its border", async ({ page }) => {
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way?",
            "",
            "- Alice: Left.",
            "",
            "# The Hall",
            "",
            "Guide: Inside.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");

    await stage.locator("g.region rect").first().dispatchEvent("click");

    await expect(page.locator("#detail-title")).toHaveText("The Gate");
    await expect(page.locator("#detail-body h4")).toHaveText([
        "Entering",
        "Leaving",
        "Source",
        "Preview",
    ]);
    // A scene's text is a stretch of the document, so its preview renders as Markdown does.
    await expect(page.locator("#detail-body .preview li")).not.toHaveCount(0);
});

test("renders a list in the preview as a list, markers and all", async ({ page }) => {
    writeFileSync(
        LIVE_DOC,
        ["# The Gate", "", "- Alice: Left.", "", "- Alice: Right.", ""].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    await page
        .locator("section.stage.active g.node")
        .filter({ hasText: "Choice" })
        .first()
        .locator("circle")
        .dispatchEvent("click");

    const item = page.locator("#detail-body .preview li").first();
    await expect(item).toBeVisible();
    // A marker is drawn only for a list-item box; a plain block silently swallows the bullets.
    expect(await item.evaluate((li) => getComputedStyle(li).display)).toBe("list-item");
});

test("holds one chosen thing at a time — a node, a route, or a region", async ({ page }) => {
    // Choosing is choosing, whatever the thing is: picking a new one plainly lets the last go.
    writeFileSync(
        LIVE_DOC,
        ["# The Gate", "", "Guide: Which way?", "", "Guide: You are inside.", ""].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");

    // The node's hit area selects without folding — a click on its circle would collapse it and
    // take the edges out of the drawing along with it.
    await stage.locator("g.node rect.hit").first().dispatchEvent("click");
    await expect(stage.locator("g.node.selected")).toHaveCount(1);

    await stage.locator("path.edge-hit").first().dispatchEvent("click");
    await expect(stage.locator("path.link.selected")).toHaveCount(1);
    await expect(stage.locator("g.node.selected")).toHaveCount(0);

    await stage.locator("g.region rect").first().dispatchEvent("click");
    await expect(stage.locator("g.region.selected")).toHaveCount(1);
    await expect(stage.locator("path.link.selected")).toHaveCount(0);

    await stage.locator("g.node rect.hit").first().dispatchEvent("click");
    await expect(stage.locator("g.node.selected")).toHaveCount(1);
    await expect(stage.locator("g.region.selected")).toHaveCount(0);
});

test("keeps a placement link out of the flow it is not part of", async ({ page }) => {
    // The line after an unconditional divert is unreachable. It gets a line so it is not adrift,
    // but control never travels it — so it is neither a destination nor a route to open.
    writeFileSync(
        LIVE_DOC,
        [
            "# Loop",
            "",
            "Alice: Around again.",
            "",
            "=> [Loop](#loop)",
            "",
            "Alice: Nobody reads this.",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");

    await stage
        .locator("g.node")
        .filter({ hasText: "(jump)" })
        .first()
        .locator("rect.hit")
        .dispatchEvent("click");
    await expect(page.locator("#detail-title")).toContainText("(jump)");
    await expect(page.locator("#detail-body button.route")).not.toHaveText(["Not reached"]);

    // And clicking the line itself opens nothing: there is no route there to open. The click
    // carries real coordinates, because the pointer resolves to the nearest line rather than to
    // whichever target it was dispatched on.
    await page.evaluate(() => {
        const stage = document.querySelector("section.stage.active")!;
        const barred = [...stage.querySelectorAll<SVGPathElement>("path.link")].find(
            (path) => path.querySelector("title")?.textContent === "Not reached",
        )!;
        const point = barred.getPointAtLength(barred.getTotalLength() / 2);
        const matrix = barred.getScreenCTM()!;
        const twins = [...stage.querySelectorAll<SVGPathElement>("path.edge-hit")];
        const twin = twins.find((each) => each.getAttribute("d") === barred.getAttribute("d"))!;
        twin.dispatchEvent(
            new MouseEvent("click", {
                bubbles: true,
                clientX: matrix.a * point.x + matrix.c * point.y + matrix.e,
                clientY: matrix.b * point.x + matrix.d * point.y + matrix.f,
            }),
        );
    });
    await expect(stage.locator("path.link.selected")).toHaveCount(0);
    await expect(page.locator("#detail-title")).toContainText("(jump)");
});

test("names a region's kind and takes the reader to the heading that declares it", async ({
    page,
}) => {
    writeFileSync(LIVE_DOC, ["# The Gate", "", "Guide: Which way?", ""].join("\n"));
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();

    await page.locator("section.stage.active g.region rect").first().dispatchEvent("click");

    await expect(page.locator("#detail-body table").first()).toContainText("Scene");
    await expect(page.locator("#detail-body table").first()).toContainText("the-gate");
    await page.locator("#detail-title button").click();
    // The Source tab opens with the heading's own words selected, not the lines beneath them.
    await expect(page.locator(".source-stage")).toBeVisible();
});

test("reads a jump as a jump in every stage that has interpreted one", async ({ page }) => {
    writeFileSync(
        LIVE_DOC,
        ["# The Gate", "", "Guide: Which way?", "", "=> [The Gate](#the-gate)", ""].join("\n"),
    );
    await page.goto("/");

    for (const tab of ["Dialogue Graph", "Semantic Model", "Desugared AST", "Dialogue AST"]) {
        await page.locator(".tab", { hasText: tab }).click();
        await page
            .locator("section.stage.active g.node")
            .filter({ hasText: "=>" })
            .or(page.locator("section.stage.active g.node").filter({ hasText: "jump" }))
            .first()
            .locator("rect.hit")
            .dispatchEvent("click");
        await expect(page.locator("#detail-body .preview .jump-ligature")).toHaveCount(1);
    }
});

test("gives routes ending at one node a corridor each, and picks the nearest", async ({ page }) => {
    // Every jump into a scene lands on its entry. Climbing in that node's own column would stack
    // them into one line to the eye and a coin toss to the pointer.
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way?",
            "",
            "- Alice: Left.",
            "",
            "  => [The Gate](#the-gate)",
            "",
            "- Alice: Right.",
            "",
            "  => [The Gate](#the-gate)",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    const stage = page.locator("section.stage.active");
    await expect(stage.locator("path.reference")).not.toHaveCount(0);

    // Each climbs in its own corridor: no two share the column they rise in.
    const corridors = await stage.locator("path.reference").evaluateAll((paths) =>
        paths.map((path) => {
            const numbers = (path.getAttribute("d") ?? "").match(/-?\d+(\.\d+)?/g) ?? [];
            return numbers[numbers.length - 6];
        }),
    );
    expect(new Set(corridors).size).toBe(corridors.length);

    // And the pointer finds the line it is actually over, not whichever twin is on top: entering
    // the second route's target while aiming at a point on the first still lights the first.
    const lit = await page.evaluate(() => {
        const stage = document.querySelector("section.stage.active")!;
        const [first, second] = [...stage.querySelectorAll<SVGPathElement>("path.reference")];
        const point = first.getPointAtLength(first.getTotalLength() / 2);
        const matrix = first.getScreenCTM()!;
        const twins = [...stage.querySelectorAll<SVGPathElement>("path.edge-hit")];
        const decoy = twins.find((twin) => twin.getAttribute("d") === second.getAttribute("d"))!;
        decoy.dispatchEvent(
            new MouseEvent("mouseenter", {
                bubbles: true,
                clientX: matrix.a * point.x + matrix.c * point.y + matrix.e,
                clientY: matrix.b * point.x + matrix.d * point.y + matrix.f,
            }),
        );
        return {
            first: first.classList.contains("hovered"),
            second: second.classList.contains("hovered"),
        };
    });
    expect(lit).toEqual({ first: true, second: false });
});

test("frames a graph from its own root rather than inheriting where you were looking", async ({
    page,
}) => {
    // The dialogue graph runs far wider than the trees beside it, so carrying a pan into it
    // scrolls its nodes off-screen and leaves the reader looking at nothing.
    writeFileSync(
        LIVE_DOC,
        ["# The Gate", "", "Guide: Which way?", "", "Guide: You are inside.", ""].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    const canvas = page.locator("section.stage.active svg.tree");
    const box = (await canvas.boundingBox())!;
    await page.mouse.move(box.x + box.width * 0.7, box.y + box.height * 0.5);
    await page.mouse.down();
    await page.mouse.move(box.x + 40, box.y + 40, { steps: 12 });
    await page.mouse.up();

    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();

    const framed = await page.evaluate(() => {
        const stage = document.querySelector("section.stage.active")!;
        const view = stage.querySelector("svg.tree")!.getBoundingClientRect();
        const entry = stage.querySelector("g.node")!.getBoundingClientRect();
        return (
            entry.right > view.left &&
            entry.left < view.right &&
            entry.bottom > view.top &&
            entry.top < view.bottom
        );
    });
    expect(framed).toBe(true);
});

test("names each kind of route with its own pointer", async ({ page }) => {
    writeFileSync(
        LIVE_DOC,
        [
            "# The Gate",
            "",
            "Guide: Which way?",
            "",
            "- Alice: Left.",
            "",
            "- Alice: Right.",
            "",
            '> `if` `"Calm"?`',
            ">",
            "> Guide: All is quiet.",
            ">",
            "> `else`",
            ">",
            "> Guide: Something stirs.",
            "",
            "=> [The Gate](#the-gate)",
            "",
        ].join("\n"),
    );
    await page.goto("/");
    await page.locator(".tab", { hasText: "Dialogue Graph" }).click();
    await expect(page.locator("section.stage.active path.edge-hit")).not.toHaveCount(0);

    const pointers = await page
        .locator("section.stage.active path.edge-hit")
        .evaluateAll((hits) =>
            [
                ...new Set(
                    hits.map(
                        (hit) =>
                            `${hit.querySelector("title")?.textContent}=${(hit as SVGPathElement).style.cursor}`,
                    ),
                ),
            ].sort(),
        );

    expect(pointers).toContain("Choice=pointer");
    expect(pointers).toContain("Conditional branch=help");
    expect(pointers).toContain("Jump=alias");
    expect(pointers).toContain("Succession=e-resize");
});
