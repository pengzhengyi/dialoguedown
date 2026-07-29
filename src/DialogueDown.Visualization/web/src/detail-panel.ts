import type { DialogueSymbolProvider, DisplayNode } from "./model";
import { colorOf } from "./palette";
import { escapeHtml, renderMarkdown } from "./text";
import { createSourceView, type SourceViewHandle } from "./source-view";
import { spanSplice, type Span } from "./span-splice";
import { createJumpButton, type JumpButton } from "./jump-button";

/** How the inspector participates in editing; absent for a static, read-only report. */
export interface DetailEditOptions {
    /** Whether the session is in Edit mode right now (so a node's editor is editable). */
    isEditable(): boolean;
    /** The current document a node edit splices into — the last-compiled source. */
    getDocument(): string;
    /** Apply a node edit: the whole document after splicing the node's new text in. */
    onNodeEdit(nextDocument: string): void;
    /** Symbols for the node editor's autocompletion (parity with the Source tab). */
    symbols?: DialogueSymbolProvider;
}

export interface DetailPanelOptions {
    /** Present for a served session (enables the editor); absent for a static export. */
    edit?: DetailEditOptions;
    /**
     * Jump to the selected node's source in the Source tab — selecting its span, or placing the
     * cursor at a synthetic node's zero-width position. Absent when there is no Source tab to jump
     * to (a single-graph render), which also hides the inspector's jump affordance.
     */
    jumpToSource?: (span: Span) => void;
}

export interface DetailPanel {
    show(node: DisplayNode): void;
    clear(): void;
    /** Reconfigure the shown node's editor when the session toggles View ⇄ Edit. */
    setEditable(editable: boolean): void;
}

/** The body HTML shown when no node is selected. */
export const NODE_DETAIL_PLACEHOLDER =
    "<p>Click any node to see the source it was produced from, and a rendered preview.</p>";

/** The plain-text placeholder the inspector shows before any node is selected. */
const PLACEHOLDER_TEXT = "Click any node to see and edit the source it was produced from.";

/** The note shown for a synthetic node — one the compiler inserts for a line that names no
 *  speaker (the only sourceless node this editor inspector ever shows). In a served session
 *  the reader can act on it, so a call to action points them at the editable parent line. */
const SYNTHETIC_SPEAKER_NOTE = "Inserted by the compiler because the line names no speaker.";
const EDIT_THE_LINE_CTA = " Edit the line to name one.";

/** The title HTML for a node's detail: a category color dot beside the node's label. */
export function nodeDetailTitle(node: DisplayNode): string {
    return categoryDot(node.category) + escapeHtml(node.label);
}

/** The body HTML for a node's detail: its attributes, then its source and a rendered preview. */
export function nodeDetailBody(node: DisplayNode): string {
    return attributesTable(node.attributes) + sourceSection(node.source);
}

/**
 * The side panel showing a selected node's category, attributes, and source. For a served
 * session the source is a **reused Source editor** — read-only in View, editable in Edit —
 * so a writer can change the source behind a node in place; editing a node splices its new
 * text back into the document (see {@link spanSplice}) and the session's Save recompiles.
 * A synthetic node (no source) shows a note instead; a static export is read-only.
 */
export function createDetailPanel(options: DetailPanelOptions = {}): DetailPanel {
    const titleEl = document.getElementById("detail-title")!;
    const bodyEl = document.getElementById("detail-body")!;
    const edit = options.edit;

    // Persistent body structure so the editor can be reused across selections: an optional
    // jump-to-source action, an attributes table, then a source area holding either the editor
    // (nodes with source) or a note.
    const attributes = document.createElement("div");
    attributes.className = "node-attributes";
    const note = document.createElement("p");
    note.className = "node-note";
    const sourceArea = document.createElement("div");
    sourceArea.className = "node-source";
    bodyEl.replaceChildren(attributes, sourceArea, note);

    // The inspector editor is created lazily on the first node with source, then reused (its
    // content and editability swapped per selection) so clicking a node never rebuilds it.
    let editor: SourceViewHandle | null = null;
    // Guards a programmatic setContent (loading a node) from looking like a user edit.
    let loading = false;
    let currentNode: DisplayNode | null = null;
    // The span being edited and the document its offsets index — captured while the session
    // is clean (navigation is locked while dirty) so the splice base is always valid.
    let editingSpan: { start: number; end: number } | null = null;
    let editingBase = "";

    // A "Jump to source" affordance (served or export, whenever there is a Source tab): an icon
    // button placed beside the node title that takes the reader to the node's span in the Source
    // editor. Shown only for a node that maps to a position — which now includes a synthetic node
    // (a zero-width caret). Reused, not rebuilt, across selections.
    const jump: JumpButton | null = options.jumpToSource
        ? createJumpButton(options.jumpToSource)
        : null;

    function ensureEditor(): SourceViewHandle {
        if (editor) return editor;
        editor = createSourceView("", {
            editable: false,
            previewStorageKey: "dd-inspector-preview-collapsed",
            ...(edit?.symbols ? { symbols: edit.symbols } : {}),
            onChange: (value) => {
                if (loading || !edit || editingSpan === null) return; // programmatic or read-only
                edit.onNodeEdit(spanSplice(editingBase, editingSpan, value));
            },
        });
        editor.element.classList.add("inspector-editor");
        sourceArea.appendChild(editor.element);
        return editor;
    }

    // Point the editor's editability (and the splice base) at the current node for the mode.
    function applyEditability(editable: boolean): void {
        const canEdit = !!edit && editable && currentNode?.span != null;
        editor?.setEditable(canEdit);
        editingSpan = canEdit ? currentNode!.span! : null;
        editingBase = canEdit && edit ? edit.getDocument() : "";
    }

    function loadNode(node: DisplayNode): void {
        const view = ensureEditor();
        loading = true;
        view.setContent(node.source ?? "");
        loading = false;
        applyEditability(edit ? edit.isEditable() : false);
        view.element.hidden = false;
        note.hidden = true;
    }

    function showNote(text: string): void {
        editingSpan = null;
        if (editor) editor.element.hidden = true;
        note.textContent = text;
        note.hidden = false;
    }

    // Render the title, then re-append the reused jump button (setting the title HTML/text drops
    // any prior child) and reflect the node on it.
    function renderTitle(html: string, node: DisplayNode | null): void {
        titleEl.innerHTML = html;
        if (jump) {
            titleEl.appendChild(jump.element);
            jump.update(node);
        }
    }

    return {
        show(node) {
            currentNode = node;
            renderTitle(nodeDetailTitle(node), node);
            attributes.innerHTML = attributesTable(node.attributes);
            if (typeof node.source === "string") loadNode(node);
            else
                showNote(
                    edit ? SYNTHETIC_SPEAKER_NOTE + EDIT_THE_LINE_CTA : SYNTHETIC_SPEAKER_NOTE,
                );
        },
        clear() {
            currentNode = null;
            renderTitle(escapeHtml("Node details"), null);
            attributes.innerHTML = "";
            showNote(PLACEHOLDER_TEXT);
        },
        setEditable(editable) {
            if (editor && currentNode && typeof currentNode.source === "string") {
                applyEditability(editable);
            }
        },
    };
}

// A color dot ties the node to its legend color without repeating a category
// name (the node's own label already appears beside it).
function categoryDot(category: string | undefined): string {
    if (!category) return "";
    return `<span class="dot" style="background:${colorOf(category)}"></span>`;
}

function attributesTable(attributes: DisplayNode["attributes"]): string {
    if (!attributes.length) return "";
    const rows = attributes
        .map(
            (attr) =>
                `<tr><th scope="row">${escapeHtml(attr.name)}</th><td>${escapeHtml(attr.value)}</td></tr>`,
        )
        .join("");
    return `<table><tbody>${rows}</tbody></table>`;
}

function sourceSection(source: string | undefined): string {
    // A node with no source is synthetic — a stage inserted it (a filled default
    // speaker), so it maps to no text. Say so, instead of an empty Source block.
    if (typeof source !== "string") {
        return `<p class="inserted-note">Inserted by the compiler — no source.</p>`;
    }
    return (
        `<h4>Source</h4><pre><code>${escapeHtml(source)}</code></pre>` +
        `<h4>Preview</h4><div class="preview">${renderMarkdown(source)}</div>`
    );
}
