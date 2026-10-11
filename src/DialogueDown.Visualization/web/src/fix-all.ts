/**
 * @file Planning a batch of diagnostic fixes: which preferred fixes to apply, in what order, and
 * which to skip when they collide. It reads diagnostics and a position resolver, never an editor,
 * so the plan is unit-tested on its own.
 */

import type { LspDiagnostic, LspFix, LspRange } from "./model";

/** Resolves an LSP range to a half-open `[start, end)` offset pair against the current buffer. */
export type RangeResolver = (range: LspRange) => { start: number; end: number };

/** One absolute text replacement, ready for a single CodeMirror transaction. */
export interface PlannedEdit {
    from: number;
    to: number;
    insert: string;
}

/** What a batch did: how many fixable problems it applied and how many it skipped. */
export interface FixAllOutcome {
    applied: number;
    skipped: number;
}

/** The batch: the edits to dispatch and the outcome to report. */
export interface FixPlan extends FixAllOutcome {
    edits: readonly PlannedEdit[];
}

/** One preferred fix resolved to absolute edits, with the keys that order it. */
interface Candidate {
    code: string;
    title: string;
    edits: PlannedEdit[];
}

/**
 * The number of **fixable problems** — diagnostics carrying at least one fix. This is the counted
 * unit: the panel's label and the batch's toast both count these, so they never disagree.
 */
export function fixableCount(diagnostics: readonly LspDiagnostic[]): number {
    return diagnostics.filter((diagnostic) => (diagnostic.fixes?.length ?? 0) > 0).length;
}

/**
 * Plan a batch: each fixable problem's preferred (first) fix, ordered by where it starts and then
 * by code and title, dropping any fix that overlaps an already-kept one. A fix is also dropped when
 * its diagnostic names no text (a collapsed range, whose text is gone), edits nothing, or carries a
 * malformed offset. The CLI's `FixApplier` uses the same ordering and overlap policy.
 */
export function planFixAll(diagnostics: readonly LspDiagnostic[], resolve: RangeResolver): FixPlan {
    const candidates: Candidate[] = [];
    let skipped = 0;

    for (const diagnostic of diagnostics) {
        const preferred = diagnostic.fixes?.[0];
        if (preferred == null) {
            continue;
        }

        const { start, end } = resolve(diagnostic.range);
        const edits = absoluteEdits(preferred, start);
        if (start === end || edits === null || edits.length === 0) {
            skipped += 1;
            continue;
        }

        candidates.push({ code: diagnostic.code, title: preferred.title, edits });
    }

    candidates.sort(byPosition);

    const kept: Candidate[] = [];
    for (const candidate of candidates) {
        if (kept.some((other) => overlapsAny(candidate, other))) {
            skipped += 1;
            continue;
        }
        kept.push(candidate);
    }

    const edits = kept.flatMap((candidate) => candidate.edits).sort(byStart);
    return { edits, applied: kept.length, skipped };
}

/**
 * The outcome as a sentence — `Fixed 2 problems.`, or the skipped count appended when a fix could
 * not be applied, so a squiggle that survives the batch is explained.
 */
export function fixAllMessage({ applied, skipped }: FixAllOutcome): string {
    if (applied === 0) {
        return skipped > 0
            ? `No fix could be applied (${skipped} skipped).`
            : "No fix could be applied.";
    }

    const problems = `${applied} ${applied === 1 ? "problem" : "problems"}`;
    return skipped > 0 ? `Fixed ${problems}; ${skipped} skipped.` : `Fixed ${problems}.`;
}

// The preferred fix's edits, absolute against the buffer, or null when an offset is malformed.
function absoluteEdits(fix: LspFix, start: number): PlannedEdit[] | null {
    const edits: PlannedEdit[] = [];
    for (const edit of fix.edits) {
        const from = start + edit.start;
        const to = start + edit.end;
        if (from < 0 || to < from) {
            return null;
        }
        edits.push({ from, to, insert: edit.newText });
    }

    return edits;
}

// Document order decides: the fix that starts earlier is applied, so the compiler's emission order
// never does. A tie falls to the code, then the fix's title.
function byPosition(left: Candidate, right: Candidate): number {
    return (
        left.edits[0].from - right.edits[0].from ||
        compare(left.code, right.code) ||
        compare(left.title, right.title)
    );
}

function byStart(left: PlannedEdit, right: PlannedEdit): number {
    return left.from - right.from || left.to - right.to;
}

// Ordinal comparison, matching the CLI's StringComparer.Ordinal tie-break.
function compare(left: string, right: string): number {
    return left < right ? -1 : left > right ? 1 : 0;
}

function overlapsAny(candidate: Candidate, kept: Candidate): boolean {
    return candidate.edits.some((edit) => kept.edits.some((keptEdit) => overlaps(edit, keptEdit)));
}

// Half-open ranges, with a zero-width edit (an insertion) widened by one so two insertions at one
// offset conflict rather than both applying. The CLI's FixApplier uses the same test.
function overlaps(left: PlannedEdit, right: PlannedEdit): boolean {
    return left.from < endOf(right) && right.from < endOf(left);
}

function endOf(edit: PlannedEdit): number {
    return Math.max(edit.to, edit.from + 1);
}
