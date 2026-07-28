import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { toEditorDiagnostic, renderDiagnosticTooltip, errorCodeUrl } from "./diagnostics-overlay";
import type { LspDiagnostic } from "./model";

const ERROR_CODES_PAGE = "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html";

/** An LSP-shaped diagnostic with sensible defaults, overridable per test. */
function diagnostic(overrides: Partial<LspDiagnostic> = {}): LspDiagnostic {
    return {
        range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
        severity: 1,
        code: "DLG0001",
        message: "Something went wrong.",
        source: "dialoguedown",
        ...overrides,
    };
}

describe("toEditorDiagnostic", () => {
    it("resolves the zero-based line and character to a document offset", () => {
        const state = EditorState.create({ doc: "line one\nline two\nline three" });

        const converted = toEditorDiagnostic(
            state,
            diagnostic({
                range: { start: { line: 1, character: 0 }, end: { line: 1, character: 4 } },
            }),
        );

        expect(converted.from).toBe(9);
        expect(converted.to).toBe(13);
    });

    it.each([
        [1, "error"],
        [2, "warning"],
        [3, "info"],
        [4, "hint"],
    ] as const)("maps LSP severity %i to the %s lint kind", (severity, kind) => {
        const state = EditorState.create({ doc: "hello" });

        const converted = toEditorDiagnostic(state, diagnostic({ severity }));

        expect(converted.severity).toBe(kind);
    });

    it("carries the diagnostic message", () => {
        const state = EditorState.create({ doc: "hello" });

        const converted = toEditorDiagnostic(state, diagnostic({ message: "Boom." }));

        expect(converted.message).toBe("Boom.");
    });

    it("keeps a zero-width span as a collapsed range", () => {
        const state = EditorState.create({ doc: "hello world" });

        const converted = toEditorDiagnostic(
            state,
            diagnostic({
                range: { start: { line: 0, character: 6 }, end: { line: 0, character: 6 } },
            }),
        );

        expect(converted.from).toBe(6);
        expect(converted.to).toBe(6);
    });

    it("clamps a character past the line to the line's end", () => {
        const state = EditorState.create({ doc: "hi" });

        const converted = toEditorDiagnostic(
            state,
            diagnostic({
                range: { start: { line: 0, character: 0 }, end: { line: 0, character: 99 } },
            }),
        );

        expect(converted.to).toBe(2);
    });

    it("clamps a range that starts past the document to its end", () => {
        const state = EditorState.create({ doc: "hi" });

        const converted = toEditorDiagnostic(
            state,
            diagnostic({
                range: { start: { line: 9, character: 0 }, end: { line: 9, character: 0 } },
            }),
        );

        expect(converted.from).toBe(2);
        expect(converted.to).toBe(2);
    });
});

describe("errorCodeUrl", () => {
    it("links to the code's lowercase docs anchor", () => {
        expect(errorCodeUrl("DLG2001")).toBe(`${ERROR_CODES_PAGE}#dlg2001`);
    });
});

describe("renderDiagnosticTooltip", () => {
    it("shows the message and a docs link for the code", () => {
        const element = renderDiagnosticTooltip(diagnostic({ code: "DLG2001", message: "Boom." }));

        expect(element.textContent).toContain("Boom.");
        const link = element.querySelector("a");
        expect(link?.getAttribute("href")).toBe(`${ERROR_CODES_PAGE}#dlg2001`);
        expect(link?.textContent).toContain("DLG2001");
    });
});
