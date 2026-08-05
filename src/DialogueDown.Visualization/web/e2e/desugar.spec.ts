import { test, expect } from "@playwright/test";
import { writeReport } from "./report";
import type { Report } from "../src/model";

// A minimal Desugared AST report whose speaker-less line carries a synthetic default
// speaker — a node with a zero-width span and no source, exactly what the desugarer
// inserts. It is its own fixture so the source-tab suite's tab/node counts are untouched.
const report: Report = {
    source: "The room is quiet.",
    stages: [
        {
            title: "Desugared AST",
            description: "The normalized dialogue tree the desugarer produces.",
            nodes: [
                {
                    id: "n0",
                    label: "Document",
                    attributes: [],
                    category: "document",
                    source: "The room is quiet.",
                },
                {
                    id: "n1",
                    label: "Line",
                    attributes: [{ name: "span", value: "[0, 18)" }],
                    category: "speech",
                    source: "The room is quiet.",
                },
                {
                    id: "n2",
                    label: "Speaker (default)",
                    attributes: [{ name: "span", value: "[0, 0)" }],
                    category: "speech",
                },
            ],
            edges: [
                { fromId: "n0", toId: "n1", kind: "Child" },
                { fromId: "n1", toId: "n2", kind: "Child" },
            ],
        },
    ],
};

const url = writeReport(report);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
});

test("a synthetic default-speaker node shows an inserted note, not an empty source block", async ({
    page,
}) => {
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await page.locator("g.node", { hasText: "Speaker (default)" }).first().click();

    await expect(page.locator("#detail-title")).toContainText("Speaker (default)");
    await expect(page.locator("#detail-body .inserted-note")).toContainText(
        "Inserted by the compiler",
    );
    // No misleading empty Source/Preview block for a node that has no source.
    await expect(page.locator("#detail-body pre code")).toHaveCount(0);
});
