import { test, expect, type Page } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";
import type { Stage } from "../src/model";

const url = writeReport({
    source: "=> [Go](#go)\nAlice: A => B\nAlice: `=> [literal](#literal)`\n",
    stages: SAMPLE_STAGES,
});

function jumpStage(title: string, label: "Jump indicator" | "Jump", semantic = false): Stage {
    const prefix = title.toLowerCase().replaceAll(" ", "-");
    const assembled = label === "Jump";
    const lineSource = "=> [Go](#go)\nGuide: Leave.";
    return {
        title,
        description: `${title} jump preview fixture.`,
        nodes: [
            {
                id: `${prefix}-root`,
                label: semantic ? "Document root" : "Document",
                attributes: [],
                category: "document",
            },
            {
                id: `${prefix}-line`,
                label: "Line",
                attributes: [{ name: "span", value: "[0, 26)" }],
                category: "speech",
                source: lineSource,
            },
            {
                id: `${prefix}-jump`,
                label,
                attributes: [
                    ...(assembled
                        ? [
                              { name: "target", value: "#go" },
                              { name: "label", value: "Go" },
                          ]
                        : []),
                    { name: "span", value: assembled ? "[0, 12)" : "[0, 2)" },
                ],
                category: "jump",
                source: assembled ? "=> [Go](#go)" : "=>",
            },
        ],
        edges: [
            {
                fromId: `${prefix}-root`,
                toId: `${prefix}-line`,
                kind: "Child",
            },
            {
                fromId: `${prefix}-line`,
                toId: `${prefix}-jump`,
                kind: "Child",
            },
        ],
        ...(semantic ? { tables: [] } : {}),
    };
}

const graphUrl = writeReport({
    source: "=> [Go](#go)\n",
    stages: [
        jumpStage("Parse Tree", "Jump"),
        jumpStage("Custom Tables", "Jump", true),
        jumpStage("Dialogue AST", "Jump indicator"),
        jumpStage("Desugared AST", "Jump"),
        jumpStage("Semantic Model", "Jump", true),
    ],
});

async function selectJumpNode(page: Page, title: string, label: string): Promise<void> {
    await page.locator(".tab", { hasText: title }).click();
    await page
        .locator("section.stage.active g.node", { hasText: label })
        .locator("circle")
        .click({ force: true });
}

test("uses a real preview-only Fira Code ligature for jump indicators", async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2);

    const preview = page.locator(".source-preview");
    const indicator = preview.locator(".jump-ligature");
    await expect(indicator).toHaveCount(1);
    await expect(indicator).toHaveText("=>");
    await expect(page.locator(".source-pane .jump-ligature")).toHaveCount(0);

    await page.evaluate(() => document.fonts.ready);
    expect(await page.evaluate(() => document.fonts.check('16px "Fira Code"'))).toBe(true);
    await expect(indicator).toHaveCSS("font-family", /Fira Code/);
    await expect(indicator).toHaveCSS("font-feature-settings", '"calt"');
});

test("uses the jump ligature in every semantic-stage node preview", async ({ page }) => {
    await page.goto(graphUrl);
    await page.evaluate(() => document.fonts.ready);

    await selectJumpNode(page, "Parse Tree", "Line");
    await expect(page.locator("#detail-body .preview .jump-ligature")).toHaveCount(0);

    await selectJumpNode(page, "Custom Tables", "Line");
    await expect(
        page.locator("section.stage.active .node-detail-body .preview .jump-ligature"),
    ).toHaveCount(0);

    await selectJumpNode(page, "Dialogue AST", "Line");
    const dialogueIndicator = page.locator("#detail-body .preview .jump-ligature");
    await expect(dialogueIndicator).toHaveText("=>");
    await expect(dialogueIndicator).toHaveCSS("font-family", /Fira Code/);
    await expect(dialogueIndicator).toHaveCSS("font-feature-settings", '"calt"');
    await expect(page.locator("#detail-body pre code")).toContainText("Guide: Leave.");

    await selectJumpNode(page, "Desugared AST", "Line");
    const desugaredIndicator = page.locator("#detail-body .preview .jump-ligature");
    await expect(desugaredIndicator).toHaveText("=>");
    await expect(desugaredIndicator).toHaveCSS("font-family", /Fira Code/);
    await expect(desugaredIndicator).toHaveCSS("font-feature-settings", '"calt"');
    await expect(page.locator("#detail-body pre code")).toContainText("Guide: Leave.");

    await selectJumpNode(page, "Semantic Model", "Line");
    const semanticIndicator = page.locator(
        "section.stage.active .node-detail-body .preview .jump-ligature",
    );
    await expect(semanticIndicator).toHaveText("=>");
    await expect(semanticIndicator).toHaveCSS("font-family", /Fira Code/);
    await expect(semanticIndicator).toHaveCSS("font-feature-settings", '"calt"');
    await expect(page.locator("section.stage.active .node-detail-body pre code")).toContainText(
        "Guide: Leave.",
    );
});
