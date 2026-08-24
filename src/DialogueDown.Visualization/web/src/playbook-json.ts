import type { EditorState } from "@codemirror/state";

/**
 * The shape of a rendered playbook, read off the text.
 *
 * The document is written by `JsonSerializer` with `WriteIndented`, whose output is exactly
 * regular: two spaces per level, one property or one bracket per line, and no literal newline
 * inside a string. That is what lets a line's schema path and the stretch a rule covers be read
 * straight from the text — the reader is looking at a generated artifact, not at something
 * hand-written that might not hold the shape.
 *
 * Reading the text rather than the syntax tree is deliberate. CodeMirror parses lazily, so
 * `syntaxTree` covers only what has been parsed; text has no such gap and answers the same way
 * at any position, however far the reader has scrolled. Folding, which only ever asks about
 * drawn lines, is left to the grammar.
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
