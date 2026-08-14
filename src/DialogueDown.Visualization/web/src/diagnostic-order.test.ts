import { describe, expect, it } from "vitest";
import type { Diagnostic as EditorDiagnostic } from "@codemirror/lint";
import type { LspDiagnostic } from "./model";
import { orderDiagnostics, orderGutterDiagnostics } from "./diagnostic-order";

function diagnostic(
    severity: LspDiagnostic["severity"],
    code: string,
    overrides: Partial<LspDiagnostic> = {},
): LspDiagnostic {
    return {
        range: {
            start: { line: 2, character: 7 },
            end: { line: 2, character: 20 },
        },
        severity,
        code,
        message: code,
        source: "dialoguedown",
        ...overrides,
    };
}

function permutations<T>(values: readonly T[]): T[][] {
    if (values.length <= 1) return [[...values]];
    return values.flatMap((value, index) =>
        permutations(values.filter((_candidate, candidateIndex) => candidateIndex !== index)).map(
            (rest) => [value, ...rest],
        ),
    );
}

describe("orderDiagnostics", () => {
    it("orders every exact-collision permutation by severity", () => {
        const error = diagnostic(1, "DLG1001");
        const warning = diagnostic(2, "DLG2001");
        const info = diagnostic(3, "DLG3001");

        for (const input of permutations([info, warning, error])) {
            expect(orderDiagnostics(input).map((item) => item.code)).toEqual([
                "DLG1001",
                "DLG2001",
                "DLG3001",
            ]);
        }
    });

    it("keeps document position before severity", () => {
        const earlierInfo = diagnostic(3, "DLG3001", {
            range: { start: { line: 1, character: 20 }, end: { line: 1, character: 21 } },
        });
        const laterError = diagnostic(1, "DLG1001", {
            range: { start: { line: 2, character: 0 }, end: { line: 2, character: 1 } },
        });

        expect(orderDiagnostics([laterError, earlierInfo])).toEqual([earlierInfo, laterError]);
    });

    it("breaks equal-severity ties by end position, code, then message", () => {
        const laterEnd = diagnostic(1, "DLG1000", {
            range: { start: { line: 2, character: 7 }, end: { line: 2, character: 30 } },
        });
        const laterCode = diagnostic(1, "DLG1002");
        const laterMessage = diagnostic(1, "DLG1001", { message: "z message" });
        const first = diagnostic(1, "DLG1001", { message: "a message" });

        expect(orderDiagnostics([laterEnd, laterCode, laterMessage, first])).toEqual([
            first,
            laterMessage,
            laterCode,
            laterEnd,
        ]);
    });

    it("treats an unknown runtime severity as an error", () => {
        const unknown = diagnostic(99 as LspDiagnostic["severity"], "DLG0000");
        const warning = diagnostic(2, "DLG2001");

        expect(orderDiagnostics([warning, unknown])).toEqual([unknown, warning]);
    });

    it("returns a new array without mutating the payload", () => {
        const input = [diagnostic(3, "DLG3001"), diagnostic(1, "DLG1001")];

        const ordered = orderDiagnostics(input);

        expect(ordered).not.toBe(input);
        expect(input.map((item) => item.code)).toEqual(["DLG3001", "DLG1001"]);
    });
});

describe("orderGutterDiagnostics", () => {
    it("keeps position first and severity first at the same position", () => {
        const editor = (
            from: number,
            to: number,
            severity: EditorDiagnostic["severity"],
            message: string,
        ): EditorDiagnostic => ({ from, to, severity, message });

        const ordered = orderGutterDiagnostics([
            editor(5, 20, "info", "outer info"),
            editor(5, 10, "warning", "warning"),
            editor(5, 30, "error", "error"),
            editor(2, 3, "hint", "earlier hint"),
        ]);

        expect(ordered.map((item) => item.message)).toEqual([
            "earlier hint",
            "error",
            "warning",
            "outer info",
        ]);
    });
});
