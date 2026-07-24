import { test, expect } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";
import type { Report } from "../src/model";

// "Alice @alice #happy: Hi." on line 0 — the speaker's name, @id, tag, and : separator as
// disjoint tokens — and a jump indicator on line 1.
const REPORT: Report = {
    source: "Alice @alice #happy: Hi.\n=> #scene-2\n",
    stages: SAMPLE_STAGES,
    semanticTokens: [
        {
            range: { start: { line: 0, character: 0 }, end: { line: 0, character: 5 } },
            kind: "SpeakerName",
        },
        {
            range: { start: { line: 0, character: 6 }, end: { line: 0, character: 12 } },
            kind: "SpeakerId",
        },
        {
            range: { start: { line: 0, character: 13 }, end: { line: 0, character: 19 } },
            kind: "CustomTag",
        },
        {
            range: { start: { line: 0, character: 19 }, end: { line: 0, character: 20 } },
            kind: "Separator",
        },
        {
            range: { start: { line: 1, character: 0 }, end: { line: 1, character: 2 } },
            kind: "JumpIndicator",
        },
    ],
};

const url = writeReport(REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

test("colors the speaker's name, id, separator, tag, and jump distinctly", async ({ page }) => {
    const active = page.locator("section.stage.active");

    await expect(active.locator(".dd-tok-speaker-name")).toHaveText("Alice");
    await expect(active.locator(".dd-tok-speaker-id")).toHaveText("@alice");
    await expect(active.locator(".dd-tok-custom-tag")).toHaveText("#happy");
    await expect(active.locator(".dd-tok-separator")).toHaveText(":");
    await expect(active.locator(".dd-tok-jump")).toHaveText("=>");

    // Each kind resolves to its own color (the theme's token variables) — five distinct.
    const colors = await active.evaluate((root) => {
        const colorOf = (selector: string) =>
            getComputedStyle(root.querySelector(selector) as Element).color;
        return [
            colorOf(".dd-tok-speaker-name"),
            colorOf(".dd-tok-speaker-id"),
            colorOf(".dd-tok-separator"),
            colorOf(".dd-tok-custom-tag"),
            colorOf(".dd-tok-jump"),
        ];
    });
    expect(new Set(colors).size).toBe(5);
});

test("keeps the tag a separate token, not nested inside a speaker token", async ({ page }) => {
    const active = page.locator("section.stage.active");

    // Precise tokens are disjoint: the tag is its own decoration, not a child of a speaker one.
    await expect(active.locator(".dd-tok-speaker-name .dd-tok-custom-tag")).toHaveCount(0);
    await expect(active.locator(".dd-tok-speaker-id .dd-tok-custom-tag")).toHaveCount(0);
    await expect(active.locator(".dd-tok-custom-tag")).toHaveText("#happy");
});

// A jump indicator, a custom tag, and a code span, each appearing once at the top level and
// once nested inside a choice (a Markdown list item). Highlighting must look the same in both
// places — the list nesting must not mute the dialogue tokens or the code span.
const NESTED_REPORT: Report = {
    source: "=> A #x `c1`\n- => B #y `c2`\n",
    stages: SAMPLE_STAGES,
    semanticTokens: [
        {
            range: { start: { line: 0, character: 0 }, end: { line: 0, character: 2 } },
            kind: "JumpIndicator",
        },
        {
            range: { start: { line: 0, character: 5 }, end: { line: 0, character: 7 } },
            kind: "CustomTag",
        },
        {
            range: { start: { line: 1, character: 2 }, end: { line: 1, character: 4 } },
            kind: "JumpIndicator",
        },
        {
            range: { start: { line: 1, character: 7 }, end: { line: 1, character: 9 } },
            kind: "CustomTag",
        },
    ],
};

test("keeps dialogue tokens and code spans colored when nested inside a choice list", async ({
    page,
}) => {
    await page.goto(writeReport(NESTED_REPORT));
    await expect(page.locator(".tab")).toHaveCount(2);
    const active = page.locator("section.stage.active");
    await expect(active.locator(".dd-tok-jump")).toHaveCount(2);
    await expect(active.locator(".dd-tok-custom-tag")).toHaveCount(2);

    // The color the reader actually sees is the innermost element's — a Markdown highlight
    // span nested inside the token decoration would override it, which is the bug this guards.
    const colors = await active.evaluate((root) => {
        const effectiveColor = (element: Element | null): string => {
            if (element == null) return "missing";
            let node: Element = element;
            while (node.firstElementChild != null) node = node.firstElementChild;
            return getComputedStyle(node).color;
        };
        const leafWithText = (text: string): Element | null =>
            [...root.querySelectorAll(".cm-content *")].find(
                (element) => element.childElementCount === 0 && element.textContent === text,
            ) ?? null;
        const jumps = [...root.querySelectorAll(".dd-tok-jump")];
        const customTags = [...root.querySelectorAll(".dd-tok-custom-tag")];
        return {
            jumpTop: effectiveColor(jumps[0]),
            jumpNested: effectiveColor(jumps[1]),
            tagTop: effectiveColor(customTags[0]),
            tagNested: effectiveColor(customTags[1]),
            codeTop: effectiveColor(leafWithText("c1")),
            codeNested: effectiveColor(leafWithText("c2")),
        };
    });

    // A nested token/code span must render the same color as its top-level twin.
    expect(colors.jumpNested).toBe(colors.jumpTop);
    expect(colors.tagNested).toBe(colors.tagTop);
    expect(colors.codeNested).toBe(colors.codeTop);
});

test("a report with no semantic tokens shows no dialogue highlighting", async ({ page }) => {
    const plainUrl = writeReport({
        source: "Alice: Hi.\n",
        stages: SAMPLE_STAGES,
        semanticTokens: [],
    });
    await page.goto(plainUrl);
    await expect(page.locator(".tab")).toHaveCount(2);

    const active = page.locator("section.stage.active");
    await expect(active.locator(".dd-tok-speaker-name")).toHaveCount(0);
    await expect(active.locator(".dd-tok-custom-tag")).toHaveCount(0);
});
