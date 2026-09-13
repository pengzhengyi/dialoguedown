import { EditorSelection, type EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { depthOf, opensBlock } from "./playbook-json";

/**
 * Finding a place in a rendered playbook, so a table can send the reader to the JSON it summarizes.
 *
 * Like the rest of the playbook's reading, this works off the text rather than a syntax tree: the
 * document is `WriteIndented` output and therefore exactly regular, and text answers the same way
 * however far the reader has scrolled (see [`playbook-json`](./playbook-json.ts)).
 */

/** Where a table cell sends the reader: one element of a named top-level array. */
export type PlaybookTarget =
    /** The node carrying this **id** — which is not its position, since ids need not be dense. */
    | { readonly kind: "node"; readonly id: number }
    /** The speaker at this index, bound when the table is built so sorting cannot move it. */
    | { readonly kind: "speaker"; readonly index: number };

/** The line a top-level `"name": [` opens on, or null when the document has no such array. */
function arrayLine(state: EditorState, name: string): number | null {
    const opener = new RegExp(`^\\s*"${name}":\\s*\\[`);
    for (let n = 1; n <= state.doc.lines; n++) {
        if (opener.test(state.doc.line(n).text)) return n;
    }
    return null;
}

/**
 * The line opening the `index`-th element of a named array, or null when it holds fewer.
 *
 * Counts the lines that open a block one level inside the array, so it steps over an element's
 * own nested objects however deep they run.
 */
export function elementLine(state: EditorState, name: string, index: number): number | null {
    const start = arrayLine(state, name);
    if (start === null) return null;
    const inside = depthOf(state.doc.line(start).text) + 1;
    let seen = 0;
    for (let n = start + 1; n <= state.doc.lines; n++) {
        const text = state.doc.line(n).text;
        const depth = depthOf(text);
        if (depth < inside) return null; // the array closed before reaching `index`
        if (depth === inside && opensBlock(text)) {
            if (seen === index) return n;
            seen += 1;
        }
    }
    return null;
}

/**
 * The line opening the node with this **id**.
 *
 * A node's id is not its position in the array. The conformance corpus carries a case whose ids
 * run `0, 5` precisely so a runtime cannot get away with indexing, and neither can this: the
 * search reads each element's own `"id"` and matches on it.
 */
export function nodeLine(state: EditorState, id: number): number | null {
    const start = arrayLine(state, "nodes");
    if (start === null) return null;
    const inside = depthOf(state.doc.line(start).text) + 1;
    const wanted = new RegExp(`^\\s*"id":\\s*${id}\\s*,?\\s*$`);
    let opener: number | null = null;
    for (let n = start + 1; n <= state.doc.lines; n++) {
        const text = state.doc.line(n).text;
        const depth = depthOf(text);
        if (depth < inside) return null;
        if (depth === inside && opensBlock(text)) opener = n;
        // Only the element's own properties, so a nested object cannot answer for it.
        if (depth === inside + 1 && wanted.test(text)) return opener;
    }
    return null;
}

/** The line a target opens on, or null when the document does not hold it. */
export function lineOf(state: EditorState, target: PlaybookTarget): number | null {
    return target.kind === "node"
        ? nodeLine(state, target.id)
        : elementLine(state, "speakers", target.index);
}

/**
 * Put the reader on `line`, centered, with the cursor at its start.
 *
 * Centering rather than merely scrolling into view: a line revealed at the very bottom of the
 * pane is technically visible and practically useless, because the object it opens runs off the
 * screen below it.
 */
export function revealLine(view: EditorView, line: number): void {
    const target = view.state.doc.line(line);
    const selection = EditorSelection.cursor(target.from);
    view.dispatch({
        selection,
        effects: EditorView.scrollIntoView(target.from, { y: "center" }),
        scrollIntoView: true,
    });
    view.focus();
}
