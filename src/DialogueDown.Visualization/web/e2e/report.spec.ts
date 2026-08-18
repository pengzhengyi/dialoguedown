import { test, expect, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { SAMPLE_REPORT, writeReport } from "./report";

const url = writeReport(SAMPLE_REPORT);
const nodeCount = SAMPLE_REPORT.stages[0].nodes.length;
const stageDescription = SAMPLE_REPORT.stages[0].description;

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

/** Switch to the Markdown AST tab and wait for its graph to render. */
async function showAst(page: Page): Promise<void> {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node")).toHaveCount(nodeCount);
}

/** Read the zoom scale from a viewport's `transform` attribute. */
async function scaleOf(viewport: Locator): Promise<number> {
    const transform = (await viewport.getAttribute("transform")) ?? "";
    const match = transform.match(/scale\(([\d.]+)/);
    return match ? Number(match[1]) : Number.NaN;
}

/** Resolve a theme CSS variable to the browser's computed color representation. */
async function themeColor(page: Page, variable: string): Promise<string> {
    return page.evaluate((name) => {
        const probe = document.createElement("span");
        probe.style.color = `var(${name})`;
        document.body.appendChild(probe);
        const color = getComputedStyle(probe).color;
        probe.remove();
        return color;
    }, variable);
}

/**
 * Click a node by label. Overlays (legend, zoom controls, detail panel) sit above the
 * SVG and can cover a node, so disable their pointer-events first — the same approach
 * the hover test uses — so the click reaches the node beneath.
 */
async function clickNodeInView(page: Page, label: string): Promise<void> {
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });
    await page.locator("section.stage.active g.node", { hasText: label }).first().click();
}

// --- Source tab (first) ---

test("the Source tab is first and active, showing the document beside a preview", async ({
    page,
}) => {
    await expect(page.locator(".tab").first()).toHaveText("Source");
    const active = page.locator("section.stage.active");
    await expect(active).toHaveClass(/source-stage/);
    await expect(active.locator(".source-pane .cm-content")).toContainText("# Scene");
    await expect(active.locator(".source-preview")).toBeVisible();
    // The node-detail panel is only for graph tabs; it is hidden here.
    await expect(page.locator("#detail")).toBeHidden();
});

test("the Source editor renders leading front matter as YAML metadata", async ({ page }) => {
    const lines = page.locator(".source-pane .cm-line");
    await expect(lines.nth(0)).toHaveText("---");
    await expect(lines.nth(1)).toHaveText("title: Demo");
    await expect(lines.nth(2)).toHaveText("---");
    await expect(lines.nth(4)).toHaveText("# Scene");

    const fence = lines.nth(0).locator("span").first();
    await expect(fence).toBeVisible();
    expect(await fence.evaluate((element) => getComputedStyle(element).color)).toBe(
        await themeColor(page, "--md-muted"),
    );

    // YAML styles the property name and plain scalar independently. Markdown would leave the
    // whole line unclassified.
    const yamlSpans = lines.nth(1).locator("span");
    await expect(yamlSpans).toHaveCount(2);
    const yamlColors = await yamlSpans.evaluateAll((elements) => [
        ...new Set(elements.map((element) => getComputedStyle(element).color)),
    ]);
    expect(yamlColors.length).toBeGreaterThan(1);

    // The YAML `content` rule is language-scoped: ordinary Markdown prose in the body stays
    // unwrapped and keeps the editor's default text color.
    const bodyParagraph = lines.filter({ hasText: "Paragraph 1:" }).first();
    await expect(bodyParagraph).toBeVisible();
    await expect(bodyParagraph.locator("span")).toHaveCount(0);
});

test("a static export carries no Explorer control, and its slot takes no room", async ({
    page,
}) => {
    // The exported report has no project to browse, so the tab bar's leading slot stays empty —
    // and an empty slot must not leave a gap where a control would have been.
    await expect(page.locator(".tabbar-explorer")).toHaveCount(0);

    const slot = page.locator("#tabbar-leading");
    await expect(slot).toBeAttached();
    await expect(slot).toBeHidden();
    expect(await slot.evaluate((node) => node.getBoundingClientRect().width)).toBe(0);
});

test("the header brand shows the logo and reveals the name on hover", async ({ page }) => {
    const name = page.locator(".brand-name");
    // The wordmark stays in the DOM (so the <h1> keeps accessible text) but is clipped by default.
    await expect(name).toHaveText("DialogueDown");
    expect(await name.evaluate((el) => el.getBoundingClientRect().width)).toBeLessThan(1);
    // Hovering the brand expands the wordmark into view.
    await page.locator(".brand").hover();
    await expect
        .poll(async () => name.evaluate((el) => el.getBoundingClientRect().width))
        .toBeGreaterThan(20);
});

test("clicking a preview anchor link scrolls to its heading", async ({ page }) => {
    await page.locator('.source-preview a[href="#the-market"]').click();
    await expect(page).toHaveURL(/#the-market$/);
    await expect(page.locator("#the-market")).toBeInViewport();
});

test("the preview cycles nested unordered bullets by depth and keeps ordered lists numbered", async ({
    page,
}) => {
    // Regression: Pico's base stylesheet forces every `ul li` to a square, flattening nested
    // unordered lists to one marker. The preview must restore the browser/VSCode cascade
    // (disc -> circle -> square) while leaving ordered lists numbered.
    await page.goto(writeReport({ source: "- a\n  - b\n    - c\n\n1. x\n1. y\n", stages: [] }));
    const preview = page.locator("section.stage.active .source-preview");
    await expect(preview.locator("ul")).toHaveCount(3);

    const markerOf = (selector: string) =>
        preview
            .locator(selector)
            .first()
            .evaluate((el) => getComputedStyle(el).listStyleType);
    expect(await markerOf("ul > li")).toBe("disc");
    expect(await markerOf("ul ul > li")).toBe("circle");
    expect(await markerOf("ul ul ul > li")).toBe("square");
    expect(await markerOf("ol > li")).toBe("decimal");
    await expect(preview.locator("ol > li")).toHaveCount(2);
});

test("the preview keeps an even vertical rhythm across items around a nested list", async ({
    page,
}) => {
    // Regression: Pico gives every list a 1rem bottom margin, which on a *nested* list opened a
    // gap several times the spacing between sibling items — so stepping out of a sub-list looked
    // uneven. Every consecutive item pair should sit at about the same vertical step.
    await page.goto(writeReport({ source: "1. one\n   - a\n   - b\n1. two\n", stages: [] }));
    const preview = page.locator("section.stage.active .source-preview");
    await expect(preview.locator("li")).toHaveCount(4);

    const tops = await preview
        .locator("li")
        .evaluateAll((items) => items.map((li) => li.getBoundingClientRect().top));
    const gaps = tops.slice(1).map((top, i) => top - tops[i]);
    // The step out of the nested list (last "b" -> "two") must match the step between the
    // nested siblings ("a" -> "b"), not balloon to ~1rem more.
    expect(Math.abs(gaps[2] - gaps[1])).toBeLessThan(6);
});

test("the theme toggle forces light/dark and returns to following the system", async ({ page }) => {
    const html = page.locator("html");
    await expect(page.locator(".theme-select")).toBeVisible();

    await page.selectOption(".theme-select", "dark");
    await expect(html).toHaveAttribute("data-theme", "dark");

    await page.selectOption(".theme-select", "light");
    await expect(html).toHaveAttribute("data-theme", "light");

    // "System" removes the override so the page follows prefers-color-scheme again.
    await page.selectOption(".theme-select", "system");
    await expect(html).not.toHaveAttribute("data-theme");
});

test("the editor supports search and code folding (read-only)", async ({ page }) => {
    await page.locator(".cm-content").click();

    // Search panel opens with the shortcut and closes with Escape.
    await page.keyboard.press("ControlOrMeta+f");
    await expect(page.locator(".dd-search")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.locator(".dd-search")).toHaveCount(0);

    // Folding a section from the gutter chevron collapses it to a placeholder.
    await page.locator('.cm-foldGutter .cm-fold-marker[title="Fold line"]').first().click();
    await expect(page.locator(".cm-foldPlaceholder")).toBeVisible();
});

test("the editor selection uses the themed color when focused, not CodeMirror's default", async ({
    page,
}) => {
    await page.locator(".cm-content").click();
    await page.keyboard.press("ControlOrMeta+a"); // focused selection — the historic bug's case
    const bg = await page
        .locator(".cm-selectionBackground")
        .first()
        .evaluate((el) => getComputedStyle(el).backgroundColor);

    // CodeMirror's default focused selection is an opaque lavender (rgb(215, 212, 240));
    // ours is a themed, semi-transparent tint (alpha < 1) so the text stays readable.
    expect(bg).not.toBe("rgb(215, 212, 240)");
    const alpha = Number(bg.match(/[\d.]+/g)?.[3] ?? "1");
    expect(alpha).toBeLessThan(1);
});

test("switching from Source to the Markdown AST tab shows the graph and detail panel", async ({
    page,
}) => {
    await expect(page.locator("section.stage.active")).toHaveClass(/source-stage/);
    await showAst(page);
    await expect(page.locator("#detail")).toBeVisible();
});

test("the tabs sit together at the start, not spread across the header", async ({ page }) => {
    const first = await page.locator(".tab").nth(0).boundingBox();
    const second = await page.locator(".tab").nth(1).boundingBox();
    // Adjacent (grouped), not pushed to opposite ends by Pico's nav space-between.
    expect(second!.x - (first!.x + first!.width)).toBeLessThan(20);
});

test("a refresh returns to the tab that was open, not the default Source tab", async ({ page }) => {
    // Leave the default Source tab for a graph tab, then reload as a reader would.
    await showAst(page);
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");

    await page.reload();

    // The report reopens on the Markdown AST tab it was left on (remembered in sessionStorage),
    // instead of resetting to Source.
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
    await expect(page.locator("section.stage.active")).toHaveClass(/stage/);
    await expect(page.locator("section.stage.active g.node")).toHaveCount(nodeCount);
});

test("a stage the compile could not produce is a disabled, non-navigable tab", async ({ page }) => {
    const reason = "This stage is unavailable due to compilation errors.";
    const url = writeReport({
        source: "# Broken\n",
        stages: [
            {
                title: "Markdown AST",
                description: "The Markdown syntax tree.",
                nodes: [{ id: "n0", label: "Document", attributes: [] }],
                edges: [],
            },
            {
                title: "Desugared AST",
                description: "The normalized dialogue tree.",
                nodes: [],
                edges: [],
                unavailable: { reason },
            },
        ],
    });
    await page.goto(url);

    const disabled = page.locator(".tab", { hasText: "Desugared AST" });
    await expect(disabled).toHaveClass(/unavailable/);
    await expect(disabled).toHaveAttribute("aria-disabled", "true");
    await expect(disabled).toHaveAttribute("data-tip", reason);

    // Clicking a disabled tab does nothing — the reader cannot enter the missing stage.
    await disabled.click({ force: true });
    await expect(disabled).not.toHaveClass(/active/);
    await expect(page.locator(".tab.active")).toHaveText("Source");

    // The produced stages stay fully interactive.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
});

// --- Markdown AST graph (second tab) ---

test("renders every node with a colored circle and a legend of counts", async ({ page }) => {
    await showAst(page);
    await expect(page.locator("section.stage.active g.node circle")).toHaveCount(nodeCount);
    const legendItems = page.locator("section.stage.active .legend .legend-item");
    await expect(legendItems).toHaveCount(10); // one row per category present
    await expect(legendItems.filter({ hasText: "Code span" })).toBeVisible();
});

test("clicking a node shows its source and a rendered preview", async ({ page }) => {
    await showAst(page);
    await clickNodeInView(page, "Image");
    await expect(page.locator("#detail-title")).toContainText("Image");
    await expect(page.locator("#detail-body .preview img")).toHaveAttribute("src", "x.jpg");
});

test("the Document preview renders front matter as metadata, not a heading", async ({ page }) => {
    await showAst(page);
    await page.locator("g.node", { hasText: "Document" }).first().click();
    await expect(page.locator("#detail-body pre.frontmatter")).toContainText("title: Demo");
    // The front matter must not be mis-rendered as a heading (the body's own
    // "# Scene" heading is expected and fine).
    await expect(
        page.locator("#detail-body .preview :is(h1, h2)", { hasText: "Demo" }),
    ).toHaveCount(0);
});

test("hovering a node shows a Tippy tooltip with the full attribute text", async ({ page }) => {
    await showAst(page);
    // The overlays (legend, zoom, detail panel) sit above the SVG and can cover a
    // node; disable their pointer-events so the hover reaches the node beneath.
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });
    // Hover the circle: it is a solid, filled hit target (unlike the node's
    // pointer-events:none labels), so the delegated Tippy fires deterministically.
    await page.locator('g.node[data-tip*="ellipsised"] circle').hover();
    const tooltip = page.locator(".tippy-box");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("should be ellipsised");
});

test("hovering a node spotlights its lineage and dims the rest", async ({ page }) => {
    await showAst(page);
    // Overlays can cover a node; let the hover reach the node beneath.
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });
    const active = page.locator("section.stage.active");

    // The Paragraph's lineage is the Document root (ancestor) plus its own children
    // (Text, Image, …); the Heading, List, and Code span siblings sit outside it.
    await active.locator("g.node", { hasText: "Paragraph" }).locator("circle").hover();

    await expect(active.locator("svg.tree")).toHaveClass(/has-focus/);
    await expect(active.locator("g.node", { hasText: "Paragraph" }).first()).toHaveClass(/related/);
    await expect(active.locator("g.node", { hasText: "Document" }).first()).toHaveClass(/related/);
    await expect(active.locator("g.node", { hasText: "Image" }).first()).toHaveClass(/related/);

    const heading = active.locator("g.node", { hasText: "Heading" }).first();
    await expect(heading).not.toHaveClass(/related/);
    await expect(heading).toHaveCSS("opacity", "0.32");

    // Moving off the node clears the spotlight.
    await page.locator(".tab", { hasText: "Source" }).hover();
    await expect(active.locator("svg.tree")).not.toHaveClass(/has-focus/);
});

test("hovering a stage tab shows a Tippy tooltip describing the stage", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).hover();
    const tooltip = page.locator(".tippy-box");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText(stageDescription);
});

test("hovering the Source tab shows a tip describing the source view", async ({ page }) => {
    await page.locator(".tab", { hasText: "Source" }).hover();
    const tooltip = page.locator(".tippy-box");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("live Markdown preview");
});

test("clicking a legend entry dims that category's nodes", async ({ page }) => {
    await showAst(page);
    await page.locator(".legend-item", { hasText: "Code span" }).click();
    await expect(page.locator("section.stage.active g.node.dimmed")).toHaveCount(1);
});

test("the default view frames the root node in the left portion, near the vertical center", async ({
    page,
}) => {
    await showAst(page);
    const rootCircle = page
        .locator("section.stage.active g.node", { hasText: "Document" })
        .locator("circle")
        .first();
    const stageLoc = page.locator("section.stage.active");

    // The default framing applies on the next frame; poll until the root settles near
    // the vertical center of the stage.
    await expect
        .poll(async () => {
            const circle = await rootCircle.boundingBox();
            const stage = await stageLoc.boundingBox();
            if (!circle || !stage) return Number.POSITIVE_INFINITY;
            const rootCenterY = circle.y + circle.height / 2;
            const stageCenterY = stage.y + stage.height / 2;
            return Math.abs(rootCenterY - stageCenterY) / stage.height;
        })
        .toBeLessThan(0.1);

    // The root sits in the left portion, so its subtree fills the viewport rightward.
    const circle = (await rootCircle.boundingBox())!;
    const stage = (await stageLoc.boundingBox())!;
    expect(circle.x - stage.x).toBeLessThan(stage.width * 0.45);
});

test("the zoom input reflects, sets, and reverts the zoom", async ({ page }) => {
    await showAst(page);
    const input = page.locator("section.stage.active .zoom-input");
    const viewport = page.locator("section.stage.active svg.tree > g").first();
    const revert = page.locator(
        "section.stage.active .zoom-controls button[aria-label='Revert to default view']",
    );

    // A stage opens framed on what it draws, so the default is whatever shows the whole of it —
    // not a fixed 100%. The input reports that framing, and Revert returns to it.
    await expect(input).not.toHaveValue("");
    const framed = (await input.inputValue())!;
    expect(Number(framed)).toBeGreaterThan(0);

    // Focus is a restrained theme underline, not a second rounded input card inside the toolbar.
    await input.focus();
    await expect(input).toHaveCSS("box-shadow", "none");
    await expect(input).toHaveCSS("outline-style", "none");
    const zoomField = page.locator("section.stage.active .zoom-field");
    await expect(zoomField).toHaveCSS("border-bottom-style", "solid");
    await expect(zoomField).toHaveCSS("border-bottom-width", "2px");
    expect(
        await zoomField.evaluate((element) => getComputedStyle(element).borderBottomColor),
    ).not.toBe("rgba(0, 0, 0, 0)");

    await page.emulateMedia({ forcedColors: "active" });
    await input.focus();
    await expect(zoomField).toHaveCSS("border-bottom-width", "2px");
    expect(
        await zoomField.evaluate((element) => getComputedStyle(element).borderBottomColor),
    ).not.toBe("rgba(0, 0, 0, 0)");
    await page.emulateMedia({ forcedColors: "none" });

    // Stepping with + raises the zoom.
    await page.locator("section.stage.active .zoom-controls button", { hasText: "+" }).click();
    await expect(input).not.toHaveValue(framed);

    // Typing a percentage sets the zoom directly.
    await input.fill("150");
    await input.press("Enter");
    await expect(input).toHaveValue("150");
    await expect.poll(() => scaleOf(viewport)).toBeCloseTo(1.5, 1);

    // Revert returns to the default framing.
    await revert.click();
    await expect(input).toHaveValue(framed);
});

test("a stage keeps its zoom when you leave the tab and come back", async ({ page }) => {
    await showAst(page);
    const viewport = page.locator("section.stage.active svg.tree > g").first();
    const zoomIn = page.locator("section.stage.active .zoom-controls button", { hasText: "+" });

    // Reading the transform first lets the initial default framing settle before we zoom.
    const framed = await viewport.getAttribute("transform");
    await zoomIn.click();
    await zoomIn.click();
    const zoomed = await viewport.getAttribute("transform");
    expect(zoomed).not.toEqual(framed);

    // Leave for the Source tab, then return to the graph.
    await page.locator(".tab", { hasText: "Source" }).click();
    await showAst(page);

    // The graph is exactly where we left it — its own pinned camera, not re-framed.
    await expect(page.locator("section.stage.active svg.tree > g").first()).toHaveAttribute(
        "transform",
        zoomed ?? "",
    );
});

test("arrow keys move the selection", async ({ page }) => {
    await showAst(page);
    await page.locator("g.node", { hasText: "Document" }).first().click();
    await expect(page.locator("#detail-title")).toContainText("Document");
    await page.keyboard.press("ArrowRight");
    await expect(page.locator("#detail-title")).not.toContainText("Document");
});

test("has no accessibility violations (both tabs, real browser incl. color contrast)", async ({
    page,
}) => {
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]); // Source tab
    await showAst(page);
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]); // Markdown AST tab
});
