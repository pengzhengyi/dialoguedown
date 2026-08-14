import type { Diagnostic as EditorDiagnostic } from "@codemirror/lint";
import type { LspDiagnostic, LspSeverity } from "./model";

const LSP_SEVERITY_RANK: Record<LspSeverity, number> = {
    1: 0,
    2: 1,
    3: 2,
    4: 3,
};

const EDITOR_SEVERITY_RANK: Record<EditorDiagnostic["severity"], number> = {
    error: 0,
    warning: 1,
    info: 2,
    hint: 3,
};

/** Compare diagnostics in source order, using severity only to break a shared start position. */
export function compareDiagnostics(a: LspDiagnostic, b: LspDiagnostic): number {
    return (
        compareNumber(a.range.start.line, b.range.start.line) ||
        compareNumber(a.range.start.character, b.range.start.character) ||
        compareNumber(severityRank(a.severity), severityRank(b.severity)) ||
        compareNumber(a.range.end.line, b.range.end.line) ||
        compareNumber(a.range.end.character, b.range.end.character) ||
        compareString(a.code, b.code) ||
        compareString(a.message, b.message)
    );
}

/** Return a canonically ordered copy without mutating the report payload. */
export function orderDiagnostics(diagnostics: readonly LspDiagnostic[]): LspDiagnostic[] {
    return [...diagnostics].sort(compareDiagnostics);
}

/** Order gutter-tooltip items by offset, then severity, then range end. */
export function orderGutterDiagnostics(
    diagnostics: readonly EditorDiagnostic[],
): EditorDiagnostic[] {
    return [...diagnostics].sort(
        (a, b) =>
            compareNumber(a.from, b.from) ||
            compareNumber(EDITOR_SEVERITY_RANK[a.severity], EDITOR_SEVERITY_RANK[b.severity]) ||
            compareNumber(a.to, b.to),
    );
}

function severityRank(severity: LspSeverity): number {
    return LSP_SEVERITY_RANK[severity] ?? LSP_SEVERITY_RANK[1];
}

function compareNumber(a: number, b: number): number {
    return a - b;
}

function compareString(a: string, b: string): number {
    return a < b ? -1 : a > b ? 1 : 0;
}
