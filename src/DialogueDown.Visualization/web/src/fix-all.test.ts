import { describe, it, expect } from "vitest";
import { fixAllMessage, fixableCount, planFixAll, type RangeResolver } from "./fix-all";
import type { LspDiagnostic, LspEdit, LspFix } from "./model";

// Line N starts at offset N*100, so a test can place diagnostics far apart or overlap them on
// purpose by choosing a line and character.
const resolve: RangeResolver = (range) => ({
    start: range.start.line * 100 + range.start.character,
    end: range.end.line * 100 + range.end.character,
});

function fix(title: string, ...edits: LspEdit[]): LspFix {
    return { title, edits };
}

/** A fixable diagnostic at `line` and `character` whose range covers the next four characters. */
function diagnostic(code: string, fixes: LspFix[], line = 0, character = 0): LspDiagnostic {
    return {
        range: { start: { line, character }, end: { line, character: character + 4 } },
        severity: 2,
        code,
        message: "A problem.",
        source: "dialoguedown",
        fixes,
    };
}

const insert = (at: number, text: string): LspEdit => ({ start: at, end: at, newText: text });
const escape = (): LspFix => fix("Escape", insert(0, "\\"));

describe("planFixAll", () => {
    it("applies each fixable problem's preferred fix", () => {
        const plan = planFixAll(
            [diagnostic("DLG1113", [escape()]), diagnostic("DLG1113", [escape()], 1)],
            resolve,
        );

        expect(plan.applied).toBe(2);
        expect(plan.skipped).toBe(0);
        expect(plan.edits).toEqual([
            { from: 0, to: 0, insert: "\\" },
            { from: 100, to: 100, insert: "\\" },
        ]);
    });

    it("counts a diagnostic without a fix as neither applied nor skipped", () => {
        const plan = planFixAll(
            [diagnostic("DLG1113", [escape()]), diagnostic("DLG1003", [])],
            resolve,
        );

        expect(plan.applied).toBe(1);
        expect(plan.skipped).toBe(0);
        expect(plan.edits).toHaveLength(1);
    });

    it("uses the preferred fix, not an alternative", () => {
        const plan = planFixAll(
            [diagnostic("DLG1113", [escape(), fix("Alternative", insert(0, "alternative"))])],
            resolve,
        );

        expect(plan.edits).toEqual([{ from: 0, to: 0, insert: "\\" }]);
    });

    it("keeps the fix that starts earlier when two overlap", () => {
        const earlier = diagnostic(
            "DLG1113",
            [fix("Replace", { start: 0, end: 10, newText: "X" })],
            1,
            0,
        );
        const later = diagnostic(
            "DLG1113",
            [fix("Replace", { start: 0, end: 10, newText: "Y" })],
            1,
            5,
        );

        // Given the later one first, so the test proves the plan orders by position, not by input
        // order.
        const plan = planFixAll([later, earlier], resolve);

        expect(plan.applied).toBe(1);
        expect(plan.skipped).toBe(1);
        expect(plan.edits).toEqual([{ from: 100, to: 110, insert: "X" }]);
    });

    it("treats two insertions at one offset as a conflict", () => {
        const plan = planFixAll(
            [
                diagnostic("DLG2001", [fix("Upper", insert(0, "upper"))]),
                diagnostic("DLG1113", [fix("Lower", insert(0, "lower"))]),
            ],
            resolve,
        );

        // Same start, so the lower code wins and the other insertion is dropped.
        expect(plan.applied).toBe(1);
        expect(plan.skipped).toBe(1);
        expect(plan.edits).toEqual([{ from: 0, to: 0, insert: "lower" }]);
    });

    it("breaks a tie at one position by code, then title", () => {
        const plan = planFixAll(
            [
                diagnostic("DLG1113", [fix("Beta", insert(0, "beta"))]),
                diagnostic("DLG1113", [fix("Alpha", insert(0, "alpha"))]),
            ],
            resolve,
        );

        // Same position and code, so the title decides and the other is dropped.
        expect(plan.applied).toBe(1);
        expect(plan.skipped).toBe(1);
        expect(plan.edits).toEqual([{ from: 0, to: 0, insert: "alpha" }]);
    });

    it("skips a fix whose diagnostic names no text", () => {
        const gone: LspDiagnostic = {
            ...diagnostic("DLG1113", [escape()]),
            range: { start: { line: 0, character: 2 }, end: { line: 0, character: 2 } },
        };

        const plan = planFixAll([gone], resolve);

        expect(plan.applied).toBe(0);
        expect(plan.skipped).toBe(1);
        expect(plan.edits).toEqual([]);
    });

    it("skips a fix that edits nothing", () => {
        const plan = planFixAll([diagnostic("DLG1113", [fix("Empty")])], resolve);

        expect(plan.applied).toBe(0);
        expect(plan.skipped).toBe(1);
    });

    it("skips a malformed edit", () => {
        const plan = planFixAll(
            [diagnostic("DLG1113", [fix("Backwards", { start: 3, end: 1, newText: "X" })])],
            resolve,
        );

        expect(plan.applied).toBe(0);
        expect(plan.skipped).toBe(1);
    });

    it("applies a multi-edit fix whole", () => {
        const plan = planFixAll(
            [
                diagnostic("DLG1113", [
                    fix(
                        "Both",
                        { start: 0, end: 4, newText: "A" },
                        { start: 6, end: 8, newText: "B" },
                    ),
                ]),
            ],
            resolve,
        );

        expect(plan.applied).toBe(1);
        expect(plan.edits).toEqual([
            { from: 0, to: 4, insert: "A" },
            { from: 6, to: 8, insert: "B" },
        ]);
    });

    it("plans nothing when no diagnostic carries a fix", () => {
        const plan = planFixAll([diagnostic("DLG1003", [])], resolve);

        expect(plan).toEqual({ edits: [], applied: 0, skipped: 0 });
    });
});

describe("fixableCount", () => {
    it("counts only the diagnostics that carry a fix", () => {
        expect(
            fixableCount([
                diagnostic("DLG1113", [escape()]),
                diagnostic("DLG1003", []),
                diagnostic("DLG2001", [fix("Upper", insert(0, "upper"))]),
            ]),
        ).toBe(2);
    });
});

describe("fixAllMessage", () => {
    it("reports the applied count, singular or plural", () => {
        expect(fixAllMessage({ applied: 1, skipped: 0 })).toBe("Fixed 1 problem.");
        expect(fixAllMessage({ applied: 3, skipped: 0 })).toBe("Fixed 3 problems.");
    });

    it("names what it skipped", () => {
        expect(fixAllMessage({ applied: 2, skipped: 1 })).toBe("Fixed 2 problems; 1 skipped.");
    });

    it("says so when nothing could be applied", () => {
        expect(fixAllMessage({ applied: 0, skipped: 2 })).toBe(
            "No fix could be applied (2 skipped).",
        );
    });
});
