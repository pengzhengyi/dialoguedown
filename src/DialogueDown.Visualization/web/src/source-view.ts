import {
    EditorView,
    keymap,
    lineNumbers,
    highlightActiveLine,
    highlightActiveLineGutter,
    drawSelection,
    rectangularSelection,
    crosshairCursor,
} from "@codemirror/view";
import { EditorState, EditorSelection, Prec, Compartment, type Extension } from "@codemirror/state";
import { defaultKeymap, history, historyKeymap } from "@codemirror/commands";
import { markdown } from "@codemirror/lang-markdown";
import {
    syntaxHighlighting,
    HighlightStyle,
    bracketMatching,
    foldGutter,
    codeFolding,
    foldKeymap,
    foldService,
} from "@codemirror/language";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { compactSearch } from "./search-panel";
import { closeBrackets, closeBracketsKeymap } from "@codemirror/autocomplete";
import { tags } from "@lezer/highlight";
import { toggleWrap, insertLink, headingFoldEndLine } from "./editor-commands";
import { initCollapsiblePanel } from "./collapse-toggle";
import { dialogueAutocompletion } from "./editor-completions";
import { diagnosticsOverlay, setEditorDiagnostics } from "./diagnostics-overlay";
import { annotateHeadingAnchors, wireHeadingAnchorCopy } from "./heading-anchors";
import { headingSlugHints } from "./heading-slug-hints";
import {
    semanticTokens as semanticTokensExtension,
    setEditorSemanticTokens,
} from "./semantic-tokens";
import {
    type DialogueSymbolProvider,
    EMPTY_SYMBOLS,
    type LspDiagnostic,
    type SemanticToken,
} from "./model";
import { initScrollSync } from "./scroll-sync";
import { renderDocument } from "./text";

/**
 * Markdown syntax highlighting driven by CSS variables (`--md-*`), so the editor
 * follows the page's light/dark theme live — the colors resolve in the document, so
 * switching the theme re-colors the editor without rebuilding it. Strong and emphasis
 * keep the foreground color and lean on weight/slant, which reads in both themes.
 */
const markdownHighlightStyle = HighlightStyle.define([
    { tag: tags.heading, color: "var(--md-heading)", fontWeight: "600" },
    { tag: tags.strong, fontWeight: "700" },
    { tag: tags.emphasis, fontStyle: "italic" },
    { tag: [tags.link, tags.url], color: "var(--md-link)", textDecoration: "underline" },
    { tag: tags.monospace, color: "var(--md-code)" },
    { tag: tags.quote, color: "var(--md-muted)" },
    // Mute the list MARKER (`-`, `1.`) and separators, but NOT list content: @lezer/markdown
    // tags a list's whole content `tags.list` (not just its marker, which is a
    // processingInstruction), so muting `tags.list` here would gray out every token nested in
    // a choice — the compiler's dialogue tokens and code spans included.
    { tag: [tags.processingInstruction, tags.contentSeparator], color: "var(--md-muted)" },
]);

/**
 * Fold Markdown sections: a heading folds everything down to the next heading of the
 * same or higher level (so a scene collapses to its `##` line). See
 * {@link headingFoldEndLine}.
 */
const foldHeadings = foldService.of((state, lineStart) => {
    const line = state.doc.lineAt(lineStart);
    const endLine = headingFoldEndLine((n) => state.doc.line(n).text, state.doc.lines, line.number);
    return endLine == null ? null : { from: line.to, to: state.doc.line(endLine).to };
});

/** Emphasis markers that surround a selection when typed over it (auto-surround). */
const EMPHASIS_MARKS = new Set(["*", "_", "~"]);

/**
 * Type `*`, `_`, or `~` over a selection to wrap it (e.g. select a word, press `*` →
 * `*word*`). Typing with no selection is left alone, so a lone marker stays literal.
 */
const emphasisSurround = EditorView.inputHandler.of((view, from, to, text) => {
    if (from === to || !EMPHASIS_MARKS.has(text)) return false;
    const selected = view.state.sliceDoc(from, to);
    view.dispatch(
        view.state.update({
            changes: { from, to, insert: `${text}${selected}${text}` },
            selection: EditorSelection.range(from + text.length, to + text.length),
            userEvent: "input",
        }),
    );
    return true;
});

/** VS Code-style Markdown formatting shortcuts (bold, italic, link). */
const formatKeymap = [
    { key: "Mod-b", run: toggleWrap("**"), preventDefault: true },
    { key: "Mod-i", run: toggleWrap("*"), preventDefault: true },
    { key: "Mod-k", run: insertLink, preventDefault: true },
];

/** Bounds for the draggable split, as a fraction of the container width. */
const MIN_RATIO = 0.2;
const MAX_RATIO = 0.8;

/** How the Source tab behaves — read-only in View, an editor in Edit. */
export interface SourceViewOptions {
    /** When true the source pane starts editable (Edit mode); otherwise read-only (View). */
    editable?: boolean;
    /** Called with the new buffer on every edit — for the preview and dirty state. */
    onChange?: (value: string) => void;
    /**
     * Where the editor's autocompletion draws its symbols — the compiler's resolved symbols
     * from the report payload. Defaults to {@link EMPTY_SYMBOLS} (a bare render offers no
     * completions); a served session supplies a provider over its latest compile.
     */
    symbols?: DialogueSymbolProvider;
    /**
     * The `localStorage` key remembering whether the preview is collapsed. Defaults to the
     * Source tab's key; a second source view (the node inspector) passes its own so the two
     * do not share one collapse state.
     */
    previewStorageKey?: string;
}

/** A handle to a live source view, letting the mode controller reconfigure it in place. */
export interface SourceViewHandle {
    /** The source-view element (editor + divider + preview) to mount. */
    readonly element: HTMLElement;
    /** Switch the editor between editable (Edit) and read-only (View) without a rebuild. */
    setEditable(editable: boolean): void;
    /** Replace the buffer (a View-mode hot-reload), keeping the one editor instance. */
    setContent(source: string): void;
    /** The editor's current text. */
    getContent(): string;
    /**
     * Replace the compiler's diagnostics shown as the editor overlay (squiggles, gutter
     * markers, tooltips). An empty list clears the overlay — called on a hot-reload or save.
     */
    setDiagnostics(diagnostics: readonly LspDiagnostic[]): void;
    /**
     * Replace the compiler's semantic tokens shown as dialogue highlighting. An empty list
     * clears the highlighting — called on load, a hot-reload, or a save.
     */
    setSemanticTokens(tokens: readonly SemanticToken[]): void;
}

const editability = new Compartment();

/**
 * The extensions that depend on whether the editor is editable: read-only vs editable,
 * the content's accessibility attributes, and the authoring aids (close-brackets,
 * emphasis auto-surround, and the format shortcuts) that only make sense in Edit. Kept in
 * a {@link Compartment} so the mode controller can flip them at runtime — the document,
 * cursor, scroll, and undo history survive the switch.
 */
function editableConfig(editable: boolean, editableExtras: Extension[] = []) {
    return [
        // Read-only (View) keeps the editor focusable and selectable — it just rejects
        // edits — so the scrollable pane stays keyboard-accessible.
        EditorState.readOnly.of(!editable),
        EditorView.contentAttributes.of(
            editable
                ? { "aria-label": "Document source editor", tabindex: "0" }
                : { "aria-label": "Document source", "aria-readonly": "true", tabindex: "0" },
        ),
        ...(editable
            ? [
                  closeBrackets(),
                  emphasisSurround,
                  Prec.high(keymap.of(formatKeymap)),
                  ...editableExtras,
              ]
            : []),
    ];
}

/**
 * The Source tab: a CodeMirror editor of the document (left) beside a live rendered
 * preview (right), split like an editor's side-by-side preview. The editor is read-only
 * in View and editable in Edit — the same instance, reconfigured via {@link editability}
 * — so the tab looks the same in every mode. A draggable divider re-proportions the two
 * panes; preview anchor links scroll to their headings (see {@link renderDocument}).
 */
export function createSourceView(
    source: string,
    options: SourceViewOptions = {},
): SourceViewHandle {
    const {
        editable = false,
        onChange,
        symbols = () => EMPTY_SYMBOLS,
        previewStorageKey = "dd-preview-collapsed",
    } = options;

    // The document-aware completions are an Edit-only authoring aid, so they live in the
    // editability compartment alongside the other Edit-only aids.
    const completion = dialogueAutocompletion(symbols);

    const container = document.createElement("div");
    container.className = "source-view";
    const sourcePane = document.createElement("div");
    sourcePane.className = "source-pane";

    const divider = document.createElement("div");
    divider.className = "source-divider";
    // A pointer-only resize handle (no keyboard resize), so it carries no separator role;
    // the meaningful, keyboard-accessible action is the labeled hide/show toggle it hosts.
    divider.title = "Drag to resize";

    const preview = document.createElement("div");
    preview.className = "source-preview preview";
    preview.tabIndex = 0;
    preview.setAttribute("role", "region");
    preview.setAttribute("aria-label", "Preview");
    preview.innerHTML = renderDocument(source);
    annotateHeadingAnchors(preview);
    // Delegated once on the stable preview element; each render re-annotates its headings.
    wireHeadingAnchorCopy(preview);

    // Re-render the preview and report the new buffer on every change (edits in Edit, or
    // a programmatic View-mode reload). The mode controller decides what to do with it.
    const onEdit = EditorView.updateListener.of((update) => {
        if (update.docChanged) {
            const value = update.state.doc.toString();
            preview.innerHTML = renderDocument(value);
            annotateHeadingAnchors(preview);
            onChange?.(value);
        }
    });

    const view = new EditorView({
        parent: sourcePane,
        state: EditorState.create({
            doc: source,
            extensions: [
                lineNumbers(),
                highlightActiveLineGutter(),
                foldGutter(),
                diagnosticsOverlay(),
                semanticTokensExtension(),
                headingSlugHints(),
                foldHeadings,
                codeFolding(),
                drawSelection(),
                EditorState.allowMultipleSelections.of(true),
                rectangularSelection(),
                crosshairCursor(),
                highlightActiveLine(),
                highlightSelectionMatches(),
                bracketMatching(),
                compactSearch(),
                history(),
                markdown(),
                syntaxHighlighting(markdownHighlightStyle),
                EditorView.lineWrapping,
                editability.of(editableConfig(editable, [completion])),
                keymap.of([
                    ...closeBracketsKeymap,
                    ...defaultKeymap,
                    ...historyKeymap,
                    ...searchKeymap,
                    ...foldKeymap,
                ]),
                onEdit,
            ],
        }),
    });

    container.append(sourcePane, divider, preview);
    initSplitDivider(container, divider);

    // Scroll the editor and its preview together (VS Code-style), anchored on headings — but
    // only side by side. In the stacked (narrow) layout the vertical axes don't correspond, so
    // the sync is disabled there and re-enabled if the viewport widens again. Where matchMedia
    // is unavailable (non-browser hosts), fall back to the side-by-side default.
    const narrow =
        typeof window.matchMedia === "function" ? window.matchMedia("(max-width: 800px)") : null;
    let disposeScrollSync: (() => void) | null = null;
    const syncScrollWithLayout = (): void => {
        disposeScrollSync?.();
        disposeScrollSync = narrow?.matches ? null : initScrollSync(view, preview);
    };
    syncScrollWithLayout();
    narrow?.addEventListener("change", syncScrollWithLayout);

    // The preview can be hidden to give the editor the full width. Its toggle lives on the
    // split divider, doubling as the always-present re-open handle; the choice is
    // remembered across reloads.
    const previewPanel = initCollapsiblePanel({
        container,
        collapsedClass: "preview-collapsed",
        storageKey: previewStorageKey,
        name: "preview",
    });
    divider.appendChild(previewPanel.button);

    return {
        element: container,
        setEditable: (next) =>
            view.dispatch({ effects: editability.reconfigure(editableConfig(next, [completion])) }),
        setContent: (next) =>
            view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: next } }),
        getContent: () => view.state.doc.toString(),
        setDiagnostics: (diagnostics) => setEditorDiagnostics(view, diagnostics),
        setSemanticTokens: (tokens) => setEditorSemanticTokens(view, tokens),
    };
}

/** Wire the divider so dragging it re-proportions the source pane (via a CSS split variable). */
export function initSplitDivider(
    container: HTMLElement,
    divider: HTMLElement,
    splitVar = "--source-split",
    collapsedClass = "preview-collapsed",
): void {
    let dragging = false;

    divider.addEventListener("mousedown", (event) => {
        // A collapsed side panel has nothing to resize — the divider is just its re-open
        // handle, so ignore drags (the toggle itself already swallows its own mousedown).
        if (container.classList.contains(collapsedClass)) return;
        dragging = true;
        document.body.style.userSelect = "none";
        event.preventDefault();
    });

    document.addEventListener("mousemove", (event) => {
        if (!dragging) return;
        const bounds = container.getBoundingClientRect();
        // Resize along the split axis: horizontal side by side, vertical when stacked.
        const vertical = getComputedStyle(container).flexDirection === "column";
        const extent = vertical ? bounds.height : bounds.width;
        if (extent === 0) return;
        const offset = vertical ? event.clientY - bounds.top : event.clientX - bounds.left;
        const clamped = Math.max(MIN_RATIO, Math.min(MAX_RATIO, offset / extent));
        container.style.setProperty(splitVar, `${(clamped * 100).toFixed(2)}%`);
    });

    document.addEventListener("mouseup", () => {
        dragging = false;
        document.body.style.userSelect = "";
    });
}
