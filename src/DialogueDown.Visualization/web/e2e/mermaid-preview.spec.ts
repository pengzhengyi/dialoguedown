import { test, expect } from "@playwright/test";
import type { Report, Stage } from "../src/model";
import { writeReport } from "./report";

const DIAGRAM_SOURCE = "```mermaid\nflowchart LR\nA --> B\n```";

function report(source: string, stageSource = source): Report {
    const stage: Stage = {
        title: "Markdown AST",
        description: "The Markdown syntax tree.",
        nodes: [
            {
                id: "root",
                label: "Document",
                attributes: [],
                category: "document",
                source: stageSource,
            },
        ],
        edges: [],
    };
    return { source, stages: [stage] };
}

test("renders Mermaid authoring aids in Source and node previews", async ({ page }) => {
    const source = `# Scene\n\n${DIAGRAM_SOURCE}\n`;
    const data = report(source, DIAGRAM_SOURCE);
    data.semanticTokens = [
        {
            kind: "IgnoredMarkdown",
            range: {
                start: { line: 2, character: 0 },
                end: { line: 5, character: 3 },
            },
        },
    ];
    await page.goto(writeReport(data));

    const sourceDiagram = page.locator(".source-preview .mermaid-diagram");
    await expect(sourceDiagram.locator("svg")).toBeVisible();
    await expect(sourceDiagram).toHaveClass(/dd-preview-ignored/);
    await expect(sourceDiagram.locator("xpath=..")).toHaveClass(/dd-preview-ignored-region/);

    const accessible = await sourceDiagram.evaluate((diagram) => {
        const svg = diagram.querySelector("svg");
        return Boolean(
            diagram.getAttribute("aria-label") ||
            svg?.getAttribute("aria-label") ||
            svg?.getAttribute("aria-labelledby"),
        );
    });
    expect(accessible).toBe(true);

    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await page.locator("section.stage.active g.node rect.hit").dispatchEvent("click");
    await expect(page.locator("#detail-body .mermaid-diagram svg")).toBeVisible();
});

test("keeps invalid Mermaid source visible without breaking the preview", async ({ page }) => {
    await page.goto(writeReport(report("```mermaid\nnot a diagram\n```")));

    const diagram = page.locator(".source-preview .mermaid-diagram");
    await expect(diagram.locator("svg")).toHaveCount(0);
    await expect(diagram.locator(".mermaid-source")).toContainText("not a diagram");
    await expect(diagram.locator(".mermaid-error")).toBeVisible();
});

test("re-renders diagrams when the effective theme changes", async ({ page }) => {
    await page.goto(writeReport(report(DIAGRAM_SOURCE)));
    const svg = page.locator(".source-preview .mermaid-diagram svg");
    await expect(svg).toBeVisible();
    const firstId = await svg.getAttribute("id");

    await page.locator(".theme-select").selectOption("dark");

    await expect.poll(() => svg.getAttribute("id")).not.toBe(firstId);
});

test("sanitizes raw Markdown HTML before it reaches the preview", async ({ page }) => {
    await page.goto(
        writeReport(report('<img src="assets/x.png" onerror="window.__dialogueDownXss = true">')),
    );

    const image = page.locator(".source-preview img");
    await expect(image).toHaveAttribute("src", "assets/x.png");
    await expect(image).not.toHaveAttribute("onerror", /.+/);
    expect(
        await page.evaluate(
            () => (window as unknown as { __dialogueDownXss?: boolean }).__dialogueDownXss,
        ),
    ).toBeUndefined();
});
