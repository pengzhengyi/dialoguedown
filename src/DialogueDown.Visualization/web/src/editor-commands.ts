import { EditorSelection, type EditorState, type Line, type StateCommand } from "@codemirror/state";

/**
 * A command that toggles a wrapping marker (e.g. `**` for bold, `*` for italic) around
 * each selection range. If a range is already wrapped by the marker it is unwrapped;
 * otherwise the marker is added on both sides. With an empty selection it inserts the
 * pair and drops the cursor between them, so ⌘B on nothing gives `**|**`.
 */
export function toggleWrap(marker: string): StateCommand {
    const len = marker.length;
    return ({ state, dispatch }) => {
        if (state.readOnly) return false;
        const changes = state.changeByRange((range) => {
            const before = state.sliceDoc(Math.max(0, range.from - len), range.from);
            const after = state.sliceDoc(range.to, Math.min(state.doc.length, range.to + len));
            if (range.from !== range.to && before === marker && after === marker) {
                return {
                    changes: [
                        { from: range.from - len, to: range.from },
                        { from: range.to, to: range.to + len },
                    ],
                    range: EditorSelection.range(range.from - len, range.to - len),
                };
            }
            return {
                changes: [
                    { from: range.from, insert: marker },
                    { from: range.to, insert: marker },
                ],
                range: EditorSelection.range(range.from + len, range.to + len),
            };
        });
        dispatch(state.update(changes, { userEvent: "input", scrollIntoView: true }));
        return true;
    };
}

/**
 * A command that wraps each selection as a Markdown link `[text]()`, leaving the cursor
 * where you type next: inside the `()` when there was selected text (paste the URL),
 * inside the `[]` when the selection was empty (type the label first).
 */
export const insertLink: StateCommand = ({ state, dispatch }) => {
    if (state.readOnly) return false;
    const changes = state.changeByRange((range) => {
        const text = state.sliceDoc(range.from, range.to);
        const cursor = range.from === range.to ? range.from + 1 : range.from + 1 + text.length + 2;
        return {
            changes: { from: range.from, to: range.to, insert: `[${text}]()` },
            range: EditorSelection.cursor(cursor),
        };
    });
    dispatch(state.update(changes, { userEvent: "input", scrollIntoView: true }));
    return true;
};

/** Every line the selection touches — a line counts even when only partially covered — in
 *  document order, de-duplicated across multiple selection ranges. */
function coveredLines(state: EditorState): Line[] {
    const byNumber = new Map<number, Line>();
    for (const range of state.selection.ranges) {
        const first = state.doc.lineAt(range.from).number;
        const last = state.doc.lineAt(range.to).number;
        for (let n = first; n <= last; n++) byNumber.set(n, state.doc.line(n));
    }
    return [...byNumber.keys()].sort((a, b) => a - b).map((n) => byNumber.get(n)!);
}

// Re-select the just-quoted/unquoted lines from the first line's start through the last line's end,
// so the covered lines stay selected and the command can be repeated to nest or unnest further.
function selectCoveredLines(
    state: EditorState,
    lines: Line[],
    changes: readonly { from: number; to?: number; insert?: string }[],
): ReturnType<EditorState["update"]> {
    const changeSet = state.changes(changes);
    const from = changeSet.mapPos(lines[0].from, -1);
    const to = changeSet.mapPos(lines[lines.length - 1].to, 1);
    return state.update({
        changes: changeSet,
        selection: EditorSelection.single(from, to),
        userEvent: "input",
        scrollIntoView: true,
    });
}

/**
 * A command that adds a blockquote marker to the start of every line the selection covers. A line
 * with content gains `> ` and a blank line gains a bare `>`; an already-quoted line nests, so
 * `> foo` becomes `> > foo`. The covered lines are left selected so the command can be repeated.
 */
export const quoteSelection: StateCommand = ({ state, dispatch }) => {
    if (state.readOnly) return false;
    const lines = coveredLines(state);
    const changes = lines.map((line) => ({
        from: line.from,
        insert: line.length === 0 ? ">" : "> ",
    }));
    dispatch(selectCoveredLines(state, lines, changes));
    return true;
};

/**
 * A command that removes one blockquote level from every covered line: a leading `>` and the one
 * optional space after it, so `> > foo` becomes `> foo` and `>foo` becomes `foo`. A line with no
 * marker is left untouched; if no covered line is quoted the command does nothing.
 */
export const unquoteSelection: StateCommand = ({ state, dispatch }) => {
    if (state.readOnly) return false;
    const lines = coveredLines(state);
    const changes: { from: number; to: number }[] = [];
    for (const line of lines) {
        if (line.text.startsWith("> ")) changes.push({ from: line.from, to: line.from + 2 });
        else if (line.text.startsWith(">")) changes.push({ from: line.from, to: line.from + 1 });
    }
    if (changes.length === 0) return false;
    dispatch(selectCoveredLines(state, lines, changes));
    return true;
};

const HEADING = /^(#{1,6})\s/;

/**
 * The last line of the section a heading opens: everything up to (but not including) the
 * next heading of the same or higher level, or the end of the document. Returns `null`
 * when the given line is not a heading or the section is empty (nothing to fold).
 *
 * Line numbers are 1-based to match CodeMirror's `Text` API.
 */
export function headingFoldEndLine(
    lineText: (lineNumber: number) => string,
    lineCount: number,
    headingLine: number,
): number | null {
    const match = HEADING.exec(lineText(headingLine));
    if (!match) return null;
    const level = match[1].length;
    let end = headingLine;
    for (let n = headingLine + 1; n <= lineCount; n++) {
        const next = HEADING.exec(lineText(n));
        if (next && next[1].length <= level) break;
        end = n;
    }
    return end > headingLine ? end : null;
}
