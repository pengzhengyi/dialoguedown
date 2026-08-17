import AxeBuilder from "@axe-core/playwright";
import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { writeReport } from "./report";

const source = [
    "# Ignored regions",
    "",
    "| A | B |",
    "| - | - |",
    "| x | y |",
    "",
    "Alice: Visit <https://example.com/road> before sundown.",
    "",
    "---",
    "",
].join("\n");

const autolinkLine = 6;
const autolinkStart = source.split("\n")[autolinkLine].indexOf("<https");

const report: Report = {
    source,
    stages: [],
    semanticTokens: [
        {
            kind: "IgnoredMarkdown",
            range: { start: { line: 2, character: 0 }, end: { line: 4, character: 9 } },
        },
        {
            kind: "IgnoredMarkdown",
            range: {
                start: { line: autolinkLine, character: autolinkStart },
                end: { line: autolinkLine, character: autolinkStart + 26 },
            },
        },
        {
            kind: "IgnoredMarkdown",
            range: { start: { line: 8, character: 0 }, end: { line: 8, character: 3 } },
        },
    ],
};

test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => localStorage.removeItem("dd-ignored-preview-collapsed"));
    await page.goto(writeReport(report));
    await expect(page.locator(".source-preview .dd-preview-ignored-region")).toHaveCount(3);
});

function footer(page: import("@playwright/test").Page) {
    return page.locator(".dd-ignored-preview-footer");
}

function regions(page: import("@playwright/test").Page) {
    return page.locator(".source-preview .dd-preview-ignored-region");
}

function hidden(page: import("@playwright/test").Page) {
    return page.locator(".source-preview .dd-preview-ignored-region.dd-ignored-region-hidden");
}

test("hides one region from its own marker, leaving the others shown", async ({ page }) => {
    await expect(footer(page)).toContainText("3 ignored");
    await expect(footer(page)).toContainText("all shown in Preview");

    const table = regions(page).first();
    await table.locator(".dd-ignored-region-toggle").click();

    await expect(hidden(page)).toHaveCount(1);
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");
    await expect(table.locator(".dd-preview-ignored")).toBeHidden();
    await expect(table.locator(".dd-ignored-region-toggle")).toHaveAttribute(
        "aria-expanded",
        "false",
    );
});

test("keeps a region's control in one place across a toggle", async ({ page }) => {
    // Repeated show/hide must not require chasing the control: a reader toggling the same region
    // twice should be able to leave the pointer still.
    const block = regions(page).first().locator(".dd-ignored-region-toggle");
    const inline = page.locator(".dd-preview-ignored-region-inline .dd-ignored-region-toggle");

    for (const control of [block, inline]) {
        const shown = await control.boundingBox();
        await control.click();
        const hidden = await control.boundingBox();
        await control.click();
        const restored = await control.boundingBox();

        expect(hidden).toEqual(shown);
        expect(restored).toEqual(shown);
    }
});

test("reaches a region's control from the keyboard", async ({ page }) => {
    const control = regions(page).nth(2).locator(".dd-ignored-region-toggle");

    await control.focus();
    await expect(control).toBeFocused();
    await page.keyboard.press("Enter");

    await expect(regions(page).nth(2)).toHaveClass(/dd-ignored-region-hidden/);
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");
});

test("a global command overrides every individual choice", async ({ page }) => {
    await regions(page).first().locator(".dd-ignored-region-toggle").click();
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");

    await footer(page).getByRole("button", { name: "Hide all ignored content in Preview" }).click();
    await expect(hidden(page)).toHaveCount(3);
    await expect(footer(page)).toContainText("all hidden in Preview");

    // The region that was individually hidden must not survive as an exception to "show all".
    await regions(page).nth(1).locator(".dd-ignored-region-toggle").click();
    await footer(page).getByRole("button", { name: "Show all ignored content in Preview" }).click();

    await expect(hidden(page)).toHaveCount(0);
    await expect(footer(page)).toContainText("all shown in Preview");
});

type Rgba = readonly [number, number, number, number];

/** Read a CSS color in either notation Chrome may compute: `rgb()/rgba()` or `color(srgb ...)`. */
function parseColor(value: string): Rgba | null {
    const srgb = /color\(srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\)/.exec(value);
    if (srgb) {
        const [r, g, b] = [srgb[1], srgb[2], srgb[3]].map((c) => Number(c) * 255);
        return [r, g, b, srgb[4] === undefined ? 1 : Number(srgb[4])];
    }

    const rgb = /rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)/.exec(value);
    if (!rgb) return null;
    return [
        Number(rgb[1]),
        Number(rgb[2]),
        Number(rgb[3]),
        rgb[4] === undefined ? 1 : Number(rgb[4]),
    ];
}

function over(ink: Rgba, backdrop: Rgba): Rgba {
    const blend = (i: number, b: number) => i * ink[3] + b * (1 - ink[3]);
    return [blend(ink[0], backdrop[0]), blend(ink[1], backdrop[1]), blend(ink[2], backdrop[2]), 1];
}

function relativeLuminance([r, g, b]: Rgba): number {
    const channel = (value: number) => {
        const v = value / 255;
        return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    };
    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

function contrastRatio(a: Rgba, b: Rgba): number {
    const [high, low] = [relativeLuminance(a), relativeLuminance(b)].sort((x, y) => y - x);
    return (high + 0.05) / (low + 0.05);
}

/** The color a rule paints, and the color it paints onto. */
async function ruleAgainstBackdrop(
    rule: import("@playwright/test").Locator,
): Promise<{ ink: string; backdrop: string }> {
    return rule.evaluate((node) => {
        const style = getComputedStyle(node);
        const ink = [style.backgroundImage, style.backgroundColor, style.borderTopColor]
            .filter((value) => value && value !== "none")
            .join(" ");

        let behind: Element | null = node.parentElement;
        let backdrop = "rgb(255, 255, 255)";
        while (behind) {
            const color = getComputedStyle(behind).backgroundColor;
            if (color && !/rgba\(0,\s*0,\s*0,\s*0\)|transparent/.test(color)) {
                backdrop = color;
                break;
            }
            behind = behind.parentElement;
        }
        return { ink, backdrop };
    });
}

test("puts a shown thematic break on its marks' own line", async ({ page }) => {
    // A rule has no text to align against, so nothing anchors it to the marks unless we say so.
    const divider = regions(page).nth(2);
    const center = async (locator: import("@playwright/test").Locator) => {
        const box = await locator.boundingBox();
        if (!box) throw new Error("Nothing to measure.");
        return box.y + box.height / 2;
    };

    const [rule, toggle, status] = await Promise.all([
        center(divider.locator("hr")),
        center(divider.locator(".dd-ignored-region-toggle")),
        center(divider.locator(".dd-ignored-region-status")),
    ]);

    expect(Math.abs(rule - toggle)).toBeLessThanOrEqual(1);
    expect(Math.abs(status - toggle)).toBeLessThanOrEqual(1);
});

test("draws a shown thematic break dark enough to see", async ({ page }) => {
    // Muted ink dimmed a second time left the rule all but white, and the rule is the whole
    // content of its region. WCAG asks 3:1 of a graphical object that carries meaning.
    const { ink, backdrop } = await ruleAgainstBackdrop(regions(page).nth(2).locator("hr"));
    const painted = parseColor(ink);
    const behind = parseColor(backdrop);
    expect(painted, `no color found on the rule: ${ink}`).not.toBeNull();
    expect(behind, `no color found behind the rule: ${backdrop}`).not.toBeNull();

    expect(contrastRatio(over(painted!, behind!), behind!)).toBeGreaterThanOrEqual(3);
});

test("keeps a hidden inline region as a chip inside its sentence", async ({ page }) => {
    const inline = page.locator(".source-preview .dd-preview-ignored-region-inline");

    await inline.locator(".dd-ignored-region-toggle").click();

    await expect(inline).toHaveClass(/dd-ignored-region-hidden/);
    await expect(inline).toHaveAttribute("title", "Ignored autolink: <https://example.com/road>");
    const sentence = page.locator(".source-preview p", { hasText: "before sundown" });
    await expect(sentence).toContainText("Alice: Visit");
    await expect(sentence).toContainText("before sundown.");
});

test("leaves Source untouched whatever the Preview shows", async ({ page }) => {
    await footer(page).getByRole("button", { name: "Hide all ignored content in Preview" }).click();

    await expect(page.locator(".source-pane .dd-tok-ignored-markdown").first()).toBeVisible();
    await expect(page.locator(".source-pane .cm-content")).toContainText("| x | y |");
});

test("has no accessibility violations in a mixed view", async ({ page }) => {
    await regions(page).first().locator(".dd-ignored-region-toggle").click();
    await expect(footer(page)).toContainText("2 of 3 shown in Preview");

    const analyze = () => new AxeBuilder({ page }).include(".source-preview-shell").analyze();
    expect((await analyze()).violations).toEqual([]);

    await page.locator(".theme-select").selectOption("dark");
    expect((await analyze()).violations).toEqual([]);
});
