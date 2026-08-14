import { describe, it, expect, vi } from "vitest";
import { createProblemsPanel } from "./problems-panel";
import type { LspDiagnostic } from "./model";

/** A diagnostic at a zero-based line/character, with a one-character range by default. */
function diagnostic(
    line: number,
    character: number,
    severity: LspDiagnostic["severity"],
    code: string,
    message: string,
): LspDiagnostic {
    return {
        range: { start: { line, character }, end: { line, character: character + 1 } },
        severity,
        code,
        message,
        source: "dialoguedown",
    };
}

const ERROR = diagnostic(4, 2, 1, "DLG1101", "A jump must be '=> [label](target)'.");
const WARNING = diagnostic(0, 8, 2, "DLG2003", "This scene is unreachable.");
const INFO = diagnostic(9, 0, 3, "DLG2010", "A cross-file target is not resolved yet.");

describe("createProblemsPanel", () => {
    it("renders one row per diagnostic with its code and location", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([ERROR, WARNING]);

        const rows = panel.element.querySelectorAll(".problem-row");
        expect(rows).toHaveLength(2);
        const first = rows[0].textContent ?? "";
        expect(first).toContain("This scene is unreachable.");
        expect(first).toContain("DLG2003");
        // Locations read one-based, the way an editor reports them — the model is zero-based.
        expect(first).toContain("Ln 1, Col 9");
    });

    it("orders the list by position, not by the order the compiler reported them", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([INFO, ERROR, WARNING]);

        const lines = [...panel.element.querySelectorAll(".problem-location")].map(
            (el) => el.textContent,
        );
        // Reading order, so stepping down the list walks forward through the document.
        expect(lines).toEqual(["Ln 1, Col 9", "Ln 5, Col 3", "Ln 10, Col 1"]);
    });

    it("orders diagnostics at one position by severity", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });
        const info = diagnostic(4, 2, 3, "DLG3001", "Info");
        const warning = diagnostic(4, 2, 2, "DLG2001", "Warning");
        const error = diagnostic(4, 2, 1, "DLG1001", "Error");

        panel.setDiagnostics([info, warning, error]);

        const severities = [...panel.element.querySelectorAll(".problem-row")].map((row) =>
            row.getAttribute("data-severity"),
        );
        expect(severities).toEqual(["error", "warning", "info"]);
    });

    it("marks each row with its severity so one error among many infos still stands out", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([ERROR, WARNING, INFO]);

        const severities = [...panel.element.querySelectorAll(".problem-row")].map((el) =>
            el.getAttribute("data-severity"),
        );
        expect(severities).toEqual(["warning", "error", "info"]);
    });

    it("says the document is clean rather than showing an empty box", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });
        panel.setDiagnostics([ERROR]);

        panel.setDiagnostics([]);

        expect(panel.element.querySelectorAll(".problem-row")).toHaveLength(0);
        expect(panel.element.textContent).toContain("No problems");
    });

    it("navigates to the diagnostic a row describes", () => {
        const goTo = vi.fn();
        const panel = createProblemsPanel({ goTo });
        panel.setDiagnostics([ERROR, WARNING]);

        (panel.element.querySelectorAll(".problem-jump")[1] as HTMLElement).click();

        // The panel hands over the whole diagnostic; resolving its range to offsets is the
        // caller's job, so the panel never needs the editor.
        expect(goTo).toHaveBeenCalledWith(ERROR);
    });

    it("links each code to its entry in the error-code reference", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([ERROR]);

        const code = panel.element.querySelector<HTMLAnchorElement>(".problem-code")!;
        // What a rule *means* is a different question from where the problem is, so the code
        // is its own control rather than part of the jump target.
        expect(code.tagName).toBe("A");
        expect(code.href).toContain("error-codes.html#dlg1101");
        expect(code.rel).toBe("noreferrer");
    });

    it("keeps the docs link and the jump as separate controls", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([ERROR]);

        const row = panel.element.querySelector(".problem-row")!;
        // An anchor nested inside a button is invalid and unreachable for assistive tech.
        expect(row.querySelector(".problem-jump a")).toBeNull();
        expect(row.querySelectorAll(":scope > *")).toHaveLength(2);
    });

    it("counts each severity for the status-line summary", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([ERROR, WARNING, INFO, INFO]);

        expect(panel.counts()).toEqual({ error: 1, warning: 1, info: 2 });
    });

    it("reports zero counts for a clean compile", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });

        panel.setDiagnostics([]);

        expect(panel.counts()).toEqual({ error: 0, warning: 0, info: 0 });
    });

    it("replaces the previous list instead of appending to it", () => {
        const panel = createProblemsPanel({ goTo: vi.fn() });
        panel.setDiagnostics([ERROR, WARNING, INFO]);

        panel.setDiagnostics([WARNING]);

        expect(panel.element.querySelectorAll(".problem-row")).toHaveLength(1);
        expect(panel.counts()).toEqual({ error: 0, warning: 1, info: 0 });
    });
});
