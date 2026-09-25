import { describe, it, expect, vi } from "vitest";
import { createProblemsPanel, type ProblemsPanelOptions } from "./problems-panel";
import type { LspDiagnostic } from "./model";

/** The panel with inert callbacks unless a test supplies its own — navigation and fixes are the
 *  caller's job, so most tests only care about what the rows render. */
function createPanel(options: Partial<ProblemsPanelOptions> = {}) {
    return createProblemsPanel({ goTo: vi.fn(), applyFix: vi.fn(), ...options });
}

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
        const panel = createPanel();

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
        const panel = createPanel();

        panel.setDiagnostics([INFO, ERROR, WARNING]);

        const lines = [...panel.element.querySelectorAll(".problem-location")].map(
            (el) => el.textContent,
        );
        // Reading order, so stepping down the list walks forward through the document.
        expect(lines).toEqual(["Ln 1, Col 9", "Ln 5, Col 3", "Ln 10, Col 1"]);
    });

    it("orders diagnostics at one position by severity", () => {
        const panel = createPanel();
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
        const panel = createPanel();

        panel.setDiagnostics([ERROR, WARNING, INFO]);

        const severities = [...panel.element.querySelectorAll(".problem-row")].map((el) =>
            el.getAttribute("data-severity"),
        );
        expect(severities).toEqual(["warning", "error", "info"]);
    });

    it("says the document is clean rather than showing an empty box", () => {
        const panel = createPanel();
        panel.setDiagnostics([ERROR]);

        panel.setDiagnostics([]);

        expect(panel.element.querySelectorAll(".problem-row")).toHaveLength(0);
        expect(panel.element.textContent).toContain("No problems");
    });

    it("navigates to the diagnostic a row describes", () => {
        const goTo = vi.fn();
        const panel = createPanel({ goTo });
        panel.setDiagnostics([ERROR, WARNING]);

        (panel.element.querySelectorAll(".problem-jump")[1] as HTMLElement).click();

        // The panel hands over the whole diagnostic; resolving its range to offsets is the
        // caller's job, so the panel never needs the editor.
        expect(goTo).toHaveBeenCalledWith(ERROR);
    });

    it("links each code to its entry in the error-code reference", () => {
        const panel = createPanel();

        panel.setDiagnostics([ERROR]);

        const code = panel.element.querySelector<HTMLAnchorElement>(".problem-code")!;
        // What a rule *means* is a different question from where the problem is, so the code
        // is its own control rather than part of the jump target.
        expect(code.tagName).toBe("A");
        expect(code.href).toContain("error-codes.html#dlg1101");
        expect(code.rel).toBe("noreferrer");
    });

    it("keeps the docs link and the jump as separate controls", () => {
        const panel = createPanel();

        panel.setDiagnostics([ERROR]);

        const row = panel.element.querySelector(".problem-row")!;
        // An anchor nested inside a button is invalid and unreachable for assistive tech.
        expect(row.querySelector(".problem-jump a")).toBeNull();
        // The fix slot (always reserved), the jump control, and the docs link.
        expect(row.querySelectorAll(":scope > *")).toHaveLength(3);
    });

    it("reserves the fix slot in every row so rows with and without a fix align", () => {
        const panel = createPanel();
        panel.setDiagnostics([ERROR]);

        const row = panel.element.querySelector(".problem-row")!;

        expect(row.firstElementChild?.className).toBe("problem-fix-slot");
        expect(row.querySelector(".problem-fix")).toBeNull();
    });

    it("counts each severity for the status-line summary", () => {
        const panel = createPanel();

        panel.setDiagnostics([ERROR, WARNING, INFO, INFO]);

        expect(panel.counts()).toEqual({ error: 1, warning: 1, info: 2 });
    });

    it("reports zero counts for a clean compile", () => {
        const panel = createPanel();

        panel.setDiagnostics([]);

        expect(panel.counts()).toEqual({ error: 0, warning: 0, info: 0 });
    });

    it("replaces the previous list instead of appending to it", () => {
        const panel = createPanel();
        panel.setDiagnostics([ERROR, WARNING, INFO]);

        panel.setDiagnostics([WARNING]);

        expect(panel.element.querySelectorAll(".problem-row")).toHaveLength(1);
        expect(panel.counts()).toEqual({ error: 0, warning: 1, info: 0 });
    });

    it("offers a fix as a lightbulb whose hover help names the repair", () => {
        const applyFix = vi.fn();
        const goTo = vi.fn();
        const fix = {
            title: "Escape as literal text",
            edits: [{ start: 0, end: 0, newText: "\\" }],
        };
        const fixed = { ...ERROR, fixes: [fix] };
        const panel = createPanel({ applyFix, goTo });
        panel.setEditable(true);
        panel.setDiagnostics([fixed]);

        const button = panel.element.querySelector<HTMLButtonElement>(".problem-fix")!;
        expect(button.title).toBe("Escape as literal text");
        expect(button.getAttribute("aria-label")).toContain("Escape as literal text");
        expect(button.querySelector(".problem-fix-icon")).not.toBeNull();
        // The lightbulb leads the row, in the reserved slot.
        expect(button.parentElement?.className).toBe("problem-fix-slot");
        expect(button.parentElement?.parentElement?.firstElementChild).toBe(button.parentElement);

        button.click();

        expect(applyFix).toHaveBeenCalledWith(fixed, fix);
        // Applying a fix is not navigating: the row's own jump must not also fire.
        expect(goTo).not.toHaveBeenCalled();
    });

    it("offers fixes only while the editor is editable", () => {
        const fixed = {
            ...ERROR,
            fixes: [
                { title: "Escape as literal text", edits: [{ start: 0, end: 0, newText: "\\" }] },
            ],
        };
        const panel = createPanel();
        panel.setDiagnostics([fixed]);

        // A read-only editor (View mode, an exported report) offers no edit.
        expect(panel.element.querySelector(".problem-fix")).toBeNull();

        panel.setEditable(true);
        expect(panel.element.querySelector(".problem-fix")).not.toBeNull();

        panel.setEditable(false);
        expect(panel.element.querySelector(".problem-fix")).toBeNull();
    });

    it("offers no fix button when the compiler knows no repair", () => {
        const panel = createPanel();
        panel.setEditable(true);
        panel.setDiagnostics([ERROR]);

        expect(panel.element.querySelector(".problem-fix")).toBeNull();
    });
});
