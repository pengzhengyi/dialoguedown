import AxeBuilder from "@axe-core/playwright";
import { test, expect } from "@playwright/test";
import type { LspDiagnostic, Report } from "../src/model";
import { writeReport } from "./report";
import { selectTheme } from "./theme";

function diagnostic(
    line: number,
    start: number,
    end: number,
    severity: LspDiagnostic["severity"],
    code: string,
    message: string,
): LspDiagnostic {
    return {
        range: {
            start: { line, character: start },
            end: { line, character: end },
        },
        severity,
        code,
        message,
        source: "dialoguedown",
    };
}

const report: Report = {
    source: [
        "# Diagnostics",
        "",
        "Alice: exact collision target",
        "",
        "Bob: partially overlapping target",
        "",
        "Cara: zero-width hint",
        "",
    ].join("\n"),
    stages: [],
    diagnostics: [
        // Deliberately least-to-most severe: presentation must not depend on payload order.
        diagnostic(2, 7, 29, 3, "DLG3003", "Exact Info."),
        diagnostic(2, 7, 29, 2, "DLG2002", "Exact Warning."),
        diagnostic(2, 7, 29, 1, "DLG1001", "Exact Error."),
        // Geometry order is deliberate for partial overlaps: outer Info -> Warning -> inner Error.
        diagnostic(4, 5, 33, 3, "DLG3005", "Outer Info."),
        diagnostic(4, 12, 29, 2, "DLG2004", "Nested Warning."),
        diagnostic(4, 22, 28, 1, "DLG1002", "Deep Error."),
        diagnostic(6, 6, 6, 4, "DLG4001", "Zero-width Hint."),
    ],
};

test.beforeEach(async ({ page }) => {
    await page.goto(writeReport(report));
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();
});

async function tooltipMessages(page: import("@playwright/test").Page): Promise<string[]> {
    return page.locator(".cm-tooltip-lint .diagnostic-tooltip-message").allTextContents();
}

test("shows exact collisions severity first under one dominant marker", async ({ page }) => {
    const exactLine = page.locator(".source-pane .cm-line").nth(2);
    const exactRange = exactLine.locator(".cm-lintRange-error");
    await expect(exactRange).toHaveCount(1);

    const markers = page.locator(".cm-gutter-lint .cm-lint-marker");
    await expect(markers).toHaveCount(3);
    await expect(markers.first()).toHaveClass(/cm-lint-marker-error/);

    await exactRange.hover();
    await expect
        .poll(() => tooltipMessages(page))
        .toEqual(["Exact Error.", "Exact Warning.", "Exact Info."]);

    await markers.first().hover();
    await expect
        .poll(() => tooltipMessages(page))
        .toEqual(["Exact Error.", "Exact Warning.", "Exact Info."]);
});

test("keeps CodeMirror geometry order for partially overlapping ranges", async ({ page }) => {
    const partialLine = page.locator(".source-pane .cm-line").nth(4);
    await partialLine.locator(".cm-lintRange-error").first().hover();

    await expect
        .poll(() => tooltipMessages(page))
        .toEqual(["Outer Info.", "Nested Warning.", "Deep Error."]);
});

test("keeps every Problems row in position order with severity breaking exact ties", async ({
    page,
}) => {
    await page.locator(".diagnostic-summary").click();
    const rows = page.locator(".problem-row");
    await expect(rows).toHaveCount(7);
    expect(await rows.locator(".problem-message").allTextContents()).toEqual([
        "Exact Error.",
        "Exact Warning.",
        "Exact Info.",
        "Outer Info.",
        "Nested Warning.",
        "Deep Error.",
        "Zero-width Hint.",
    ]);
    await expect(page.locator(".diagnostic-summary")).toHaveAttribute(
        "aria-label",
        "2 errors, 2 warnings, 3 infos — open the Problems panel",
    );
});

test("keeps the same presentation when payload order reverses", async ({ page }) => {
    const expected = [
        "Exact Error.",
        "Exact Warning.",
        "Exact Info.",
        "Outer Info.",
        "Nested Warning.",
        "Deep Error.",
        "Zero-width Hint.",
    ];
    await page.locator(".diagnostic-summary").click();
    expect(await page.locator(".problem-message").allTextContents()).toEqual(expected);

    await page.goto(
        writeReport({
            ...report,
            diagnostics: [...(report.diagnostics ?? [])].reverse(),
        }),
    );
    await page.locator(".diagnostic-summary").click();
    expect(await page.locator(".problem-message").allTextContents()).toEqual(expected);

    await page.locator(".source-pane .cm-line").nth(2).locator(".cm-lintRange-error").hover();
    await expect
        .poll(() => tooltipMessages(page))
        .toEqual(["Exact Error.", "Exact Warning.", "Exact Info."]);
});

test("passes accessibility checks in light and dark themes", async ({ page }) => {
    await page.locator(".diagnostic-summary").click();
    const analyzeDiagnostics = () =>
        new AxeBuilder({ page }).include(".diagnostic-summary").include("#footer-drawer").analyze();
    expect((await analyzeDiagnostics()).violations).toEqual([]);

    await selectTheme(page, "dark");
    expect((await analyzeDiagnostics()).violations).toEqual([]);
});
