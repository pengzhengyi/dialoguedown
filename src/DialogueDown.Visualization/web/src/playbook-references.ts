import { RangeSetBuilder, type EditorState, type Extension } from "@codemirror/state";
import {
    Decoration,
    EditorView,
    ViewPlugin,
    type DecorationSet,
    type Command,
    type KeyBinding,
    type ViewUpdate,
} from "@codemirror/view";
import { schemaPathAt, referenceTypeAt, type ReferenceType } from "./playbook-schema";
import { elementLine, nodeLine, revealLine } from "./playbook-jump";

/**
 * Following an index in the rendered playbook to the node or speaker it names.
 *
 * The playbook is full of bare integers that point somewhere — `"entry": 0`, an edge's
 * `"target": 5`, a line's `"speaker": 1`, an anchor's node. Each becomes a link: its digits carry
 * a `dd-playbook-ref` mark, a click follows it, and `F12` follows the one on the cursor's line.
 * The jump lands in the same editor, reusing the line-finding the summary tables' jumps use.
 *
 * Like the rest of the tab's reading, this works off the text: the document is `WriteIndented`
 * output and therefore exactly regular, and text answers the same however far the reader has
 * scrolled (see [`playbook-json`](./playbook-json.ts)).
 */

/** A reference the reader can follow: which list it points into, and the index it names. */
export interface Reference {
    readonly kind: ReferenceType;
    readonly value: number;
}

/** One reference located in the document: the span of its digits, and what it points to. */
export interface ReferenceRange extends Reference {
    readonly from: number;
    readonly to: number;
}

/** A scalar property line and its integer value: `      "target": 5,`. */
const REFERENCE_LINE = /^(\s*"(?:[^"\\]|\\.)*"\s*:\s*)(\d+)(\s*,?\s*)$/;

/**
 * Whether a rendered line holds a reference the reader can follow, and to which list.
 *
 * The line's schema path says whether the format tags its value as a reference. The one exception
 * is a node's own `"id"`: the schema tags it like any node reference, but it *is* the definition,
 * not a pointer to one, so the line a jump lands on never becomes a link itself.
 */
export function referenceKindAt(state: EditorState, lineNumber: number): ReferenceType | null {
    const text = state.doc.line(lineNumber).text;
    if (!REFERENCE_LINE.test(text)) return null;
    const located = schemaPathAt(state, lineNumber);
    if (located.path.endsWith("/id")) return null;
    return referenceTypeAt(located.path, located.kinds);
}

/**
 * Every reference between two document positions, each with the span of its digits.
 *
 * Pure and line-based, so it is unit-tested without a view and can be run over just the viewport.
 */
export function referenceRanges(state: EditorState, from: number, to: number): ReferenceRange[] {
    const ranges: ReferenceRange[] = [];
    const last = state.doc.lineAt(to).number;
    for (let n = state.doc.lineAt(from).number; n <= last; n++) {
        const line = state.doc.line(n);
        const match = REFERENCE_LINE.exec(line.text);
        if (match == null) continue;
        const kind = referenceKindAt(state, n);
        if (kind == null) continue;
        const start = line.from + match[1].length;
        ranges.push({ kind, value: Number(match[2]), from: start, to: start + match[2].length });
    }
    return ranges;
}

/** The line a reference points to, or null when the document does not hold its target. */
function targetLine(state: EditorState, reference: Reference): number | null {
    return reference.kind === "node"
        ? nodeLine(state, reference.value)
        : elementLine(state, "speakers", reference.value);
}

/**
 * The definition line the reference on `lineNumber` points to, or null when the line is not a
 * reference or its target is absent from the document.
 *
 * A click and `F12` both go through here, so they can never disagree about where a reference
 * leads.
 */
export function definitionLineFor(state: EditorState, lineNumber: number): number | null {
    const kind = referenceKindAt(state, lineNumber);
    if (kind == null) return null;
    const match = REFERENCE_LINE.exec(state.doc.line(lineNumber).text);
    return match == null ? null : targetLine(state, { kind, value: Number(match[2]) });
}

/** Follow the reference on the line at `pos`, if there is one that resolves. */
function followAt(view: EditorView, pos: number): boolean {
    const target = definitionLineFor(view.state, view.state.doc.lineAt(pos).number);
    if (target == null) return false;
    revealLine(view, target);
    return true;
}

const referenceMark = Decoration.mark({
    class: "dd-playbook-ref",
    attributes: { title: "Go to definition (F12)" },
});

/** The marks over the references in the viewport, rebuilt when the viewport or document changes. */
function buildMarks(view: EditorView): DecorationSet {
    const builder = new RangeSetBuilder<Decoration>();
    for (const { from, to } of view.visibleRanges) {
        for (const range of referenceRanges(view.state, from, to)) {
            builder.add(range.from, range.to, referenceMark);
        }
    }
    return builder.finish();
}

const referenceMarks = ViewPlugin.fromClass(
    class {
        decorations: DecorationSet;

        constructor(view: EditorView) {
            this.decorations = buildMarks(view);
        }

        update(update: ViewUpdate): void {
            if (update.docChanged || update.viewportChanged) {
                this.decorations = buildMarks(update.view);
            }
        }
    },
    {
        decorations: (plugin) => plugin.decorations,
        eventHandlers: {
            mousedown(event, view): boolean {
                if (!(event.target instanceof HTMLElement)) return false;
                const span = event.target.closest(".dd-playbook-ref");
                if (span == null) return false;
                const followed = followAt(view, view.posAtDOM(span));
                if (followed) event.preventDefault();
                return followed;
            },
        },
    },
);

/** `F12` follows the reference on the cursor's line — VS Code's Go to Definition. */
const goToDefinition: Command = (view) => followAt(view, view.state.selection.main.head);

/**
 * The Playbook editor's Go to Definition binding. Spread into the editor's own `keymap.of([...])`
 * ahead of the defaults so nothing else claims `F12` there.
 */
export const playbookReferenceKeymap: readonly KeyBinding[] = [
    { key: "F12", run: goToDefinition, preventDefault: true },
];

/**
 * The reference marks and their click handler. `playbookReferenceKeymap` is exported separately
 * so the editor can order it against its other bindings.
 */
export function playbookReferences(): Extension {
    return referenceMarks;
}
