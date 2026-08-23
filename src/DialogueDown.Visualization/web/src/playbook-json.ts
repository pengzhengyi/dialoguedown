import { foldService } from "@codemirror/language";
import type { EditorState } from "@codemirror/state";

/**
 * The shape of a rendered playbook, read off the text.
 *
 * The document is written by `JsonSerializer` with `WriteIndented`, whose output is exactly
 * regular: two spaces per level, one property or one bracket per line, and no literal newline
 * inside a string. That regularity is what lets the report fold a block and mark the stretch a
 * schema rule covers without parsing the document a second time — the reader is looking at a
 * generated artifact, not at something hand-written that might not hold the shape.
 */

/** How deep a line sits, in the two-space levels the writer emits. */
export function depthOf(line: string): number {
    return (/^\s*/.exec(line)?.[0].length ?? 0) / 2;
}

/** Whether a line opens an object or an array, so a block hangs beneath it. */
export function opensBlock(line: string): boolean {
    return /[[{]\s*$/.test(line);
}

/**
 * The line closing the block a line opens, or null when the line opens none. The close is the
 * first line at or above the opener's own depth — the writer indents every member deeper, so
 * nothing shallower can belong to the block.
 */
export function blockEnd(state: EditorState, lineNumber: number): number | null {
    const line = state.doc.line(lineNumber);
    if (!opensBlock(line.text)) return null;
    const depth = depthOf(line.text);
    for (let n = lineNumber + 1; n <= state.doc.lines; n++) {
        if (depthOf(state.doc.line(n).text) <= depth) return n;
    }
    return state.doc.lines;
}

/**
 * Folds an object or an array between its brackets, leaving both in view so a folded line still
 * reads as `"nodes": [⋯]`.
 *
 * The `json` mode in `@codemirror/legacy-modes` is a tokenizer, not a parser, so it exposes no
 * syntax tree to fold from — the same gap the Config tab's TOML editor fills with its own
 * section-folding service.
 */
export const foldJsonBlocks = foldService.of((state, lineStart) => {
    const line = state.doc.lineAt(lineStart);
    const end = blockEnd(state, line.number);
    if (end == null || end <= line.number + 1) return null;
    const closing = state.doc.line(end);
    const indent = /^\s*/.exec(closing.text)?.[0].length ?? 0;
    return { from: line.to, to: closing.from + indent };
});
