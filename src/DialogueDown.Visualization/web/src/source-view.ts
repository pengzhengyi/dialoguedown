import {
    EditorView,
    keymap,
    lineNumbers,
    highlightActiveLine,
    highlightActiveLineGutter,
    drawSelection,
    rectangularSelection,
    crosshairCursor,
    Decoration,
    type DecorationSet,
} from "@codemirror/view";
import {
    EditorState,
    EditorSelection,
    Prec,
    Compartment,
    StateField,
    StateEffect,
    type ChangeDesc,
    type Extension,
    type StateCommand,
} from "@codemirror/state";
import {
    defaultKeymap,
    history,
    historyKeymap,
    indentMore,
    indentLess,
} from "@codemirror/commands";
import {
    syntaxHighlighting,
    HighlightStyle,
    bracketMatching,
    foldGutter,
    codeFolding,
    foldKeymap,
    foldService,
    indentUnit,
} from "@codemirror/language";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { foldGutterMarker } from "./fold-glyph";
import { compactSearch } from "./search-panel";
import { closeBrackets, closeBracketsKeymap } from "@codemirror/autocomplete";
import { yamlLanguage } from "@codemirror/lang-yaml";
import { tags } from "@lezer/highlight";
import {
    toggleWrap,
    insertLink,
    quoteSelection,
    unquoteSelection,
    headingFoldEndLine,
} from "./editor-commands";
import { openContextMenu, type ContextMenuItem } from "./context-menu";
import { initCollapsiblePanel } from "./collapse-toggle";
import { dialogueAutocompletion } from "./editor-completions";
import { diagnosticsOverlay, setEditorDiagnostics } from "./diagnostics-overlay";
import { positionToOffset } from "./lsp-position";
import { annotateHeadingAnchors, wireHeadingAnchorCopy } from "./heading-anchors";
import { headingSlugHints } from "./heading-slug-hints";
import { createIgnoredPreviewController } from "./ignored-preview";
import {
    semanticTokens as semanticTokensExtension,
    setEditorSemanticTokens,
} from "./semantic-tokens";
import {
    type DialogueSymbolProvider,
    EMPTY_SYMBOLS,
    type LspDiagnostic,
    type LspRange,
    type ReservedTarget,
    type SemanticToken,
    type Span,
} from "./model";
import { initScrollSync } from "./scroll-sync";
import { renderDocument, type PreviewSemantics } from "./text";
import { mountPreviewHtml } from "./preview-html";
import { mermaidPreviews } from "./mermaid-preview";
import type { DebugController } from "./debug-controller";
import { debugEditor, toggleBreakpointAt } from "./debug-editor";
import { createDebugToolbar, type DebugToolbar } from "./debug-toolbar";
import { reservedTargetsPanel, setEditorReservedTargets } from "./reserved-targets-panel";
import { sourceLanguage } from "./source-language";

/**
 * Markdown syntax highlighting driven by CSS variables (`--md-*`), so the editor
 * follows the page's light/dark theme live — the colors resolve in the document, so
 * switching the theme re-colors the editor without rebuilding it. Strong and emphasis
 * keep the foreground color and lean on weight/slant, which reads in both themes.
 */
export const markdownHighlightStyle = HighlightStyle.define([
    { tag: tags.heading, color: "var(--md-heading)", fontWeight: "600" },
    { tag: tags.strong, fontWeight: "700" },
    { tag: tags.emphasis, fontStyle: "italic" },
    { tag: [tags.link, tags.url], color: "var(--md-link)", textDecoration: "underline" },
    { tag: tags.monospace, color: "var(--md-code)" },
    { tag: tags.meta, color: "var(--md-muted)" },
    // A blockquote is never decoration here: a marker-headed quote is a control block, and any
    // other quote is a transparent wrapper whose contents are dialogue. Muting it would gray out
    // live dialogue, and the compiler's own tokens already color what is inside.
    {
        tag: tags.comment,
        color: "var(--md-muted)",
        fontStyle: "italic",
        opacity: "0.45",
    },
    // Mute the list MARKER (`-`, `1.`) and separators, but NOT list content: @lezer/markdown
    // tags a list's whole content `tags.list` (not just its marker, which is a
    // processingInstruction), so muting `tags.list` here would gray out every token nested in
    // a choice — the compiler's dialogue tokens and code spans included.
    { tag: [tags.processingInstruction, tags.contentSeparator], color: "var(--md-muted)" },
]);

/** YAML colors scoped to front matter, so their generic tags never recolor Markdown. */
export const yamlHighlightStyle = HighlightStyle.define(
    [
        { tag: tags.definition(tags.propertyName), color: "var(--md-heading)" },
        { tag: tags.string, color: "var(--md-code)" },
        { tag: tags.content, color: "var(--md-link)" },
        {
            tag: tags.lineComment,
            color: "var(--md-muted)",
            fontStyle: "italic",
            opacity: "0.45",
        },
        { tag: tags.bracket, color: "var(--md-muted)" },
    ],
    { scope: yamlLanguage },
);

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

/** VS Code-style Markdown formatting shortcuts (bold, italic, link). Blockquote quote/unquote are
 *  handled by a dedicated keydown handler below, not here, so they can match the physical key. */
const formatKeymap = [
    { key: "Mod-b", run: toggleWrap("**"), preventDefault: true },
    { key: "Mod-i", run: toggleWrap("*"), preventDefault: true },
    { key: "Mod-k", run: insertLink, preventDefault: true },
];

/** Run an editor command and return focus to the editor, so a menu choice leaves you typing. */
function runInEditor(view: EditorView, command: StateCommand): void {
    command(view);
    view.focus();
}

/** Move keyboard focus out of the editor — the escape hatch that pairs with Tab-to-indent so the
 *  editor is never a keyboard trap. Bound to Escape at low precedence, below a completion or search
 *  dismiss, so it only fires when there is nothing else to close. */
const blurEditor = (view: EditorView): boolean => {
    view.contentDOM.blur();
    return true;
};

/**
 * Smart Tab, like a usual code editor: a selection that spans lines — or the caret sitting in a
 * line's leading whitespace — indents the whole line(s); anywhere else, Tab just inserts one indent
 * unit (spaces) at the caret. Shift-Tab always outdents (bound separately to indentLess).
 */
const smartTab = (view: EditorView): boolean => {
    const { state } = view;
    if (state.readOnly) return false;
    const range = state.selection.main;
    const spansLines = state.doc.lineAt(range.from).number !== state.doc.lineAt(range.to).number;
    const line = state.doc.lineAt(range.head);
    const inLeadingSpace =
        range.empty && /^\s*$/.test(state.doc.sliceString(line.from, range.head));
    if (spansLines || inLeadingSpace) return indentMore(view);
    view.dispatch(
        state.update(state.replaceSelection(state.facet(indentUnit)), {
            scrollIntoView: true,
            userEvent: "input",
        }),
    );
    return true;
};

/**
 * The editor's surround handlers (Edit mode only):
 *
 * - **keydown** binds the blockquote shortcut to the **Period key** (which bears `.` and `>`),
 *   with Shift choosing the direction: **Cmd/Ctrl+.** quotes, **Cmd/Ctrl+Shift+.** unquotes. It
 *   matches the physical key (`event.code === "Period"`) as well as the reported character, because
 *   on macOS a held Cmd can surface `event.key` as the unshifted `.` even with Shift — which made
 *   the plain CodeMirror keymap binding for `>` / `<` unreliable in Chrome and Safari. Comma is
 *   deliberately avoided, since `Cmd/Ctrl+,` is Preferences in most apps.
 * - **contextmenu** is handled per instance (it must also open in read-only View), so it is not
 *   wired here — see the editor's context-menu handler built in {@link createSourceView}.
 *
 * In View, the keydown shortcuts defer to the browser (selection copy, its own menu).
 */
const surroundHandlers = EditorView.domEventHandlers({
    keydown(event, view) {
        if (view.state.readOnly || event.altKey || !(event.metaKey || event.ctrlKey)) return false;
        if (event.code !== "Period" && event.key !== "." && event.key !== ">") return false;
        event.preventDefault();
        if (event.shiftKey) unquoteSelection(view);
        else quoteSelection(view);
        return true;
    },
});

/** Toggles the faint highlight over the source span a hovered Jump-to target would reveal. */
const setJumpPreviewEffect = StateEffect.define<{ from: number; to: number } | null>();

const jumpPreviewMark = Decoration.mark({ class: "dd-jump-preview" });

const jumpPreviewField = StateField.define<DecorationSet>({
    create: () => Decoration.none,
    update(preview, transaction) {
        for (const effect of transaction.effects) {
            if (effect.is(setJumpPreviewEffect)) {
                const span = effect.value;
                return span && span.to > span.from
                    ? Decoration.set([jumpPreviewMark.range(span.from, span.to)])
                    : Decoration.none;
            }
        }
        return preview.map(transaction.changes);
    },
    provide: (field) => EditorView.decorations.from(field),
});

function setJumpPreview(view: EditorView, span: { from: number; to: number } | null): void {
    view.dispatch({ effects: setJumpPreviewEffect.of(span) });
}

/**
 * Carry compiler-projected preview spans through unsaved edits. Insertions at a span's opening
 * boundary belong before it; insertions at its closing boundary belong after it. Edits inside the
 * span still grow or shrink it, while deleting the whole construct removes the empty range.
 */
export function mapPreviewSpans(spans: readonly Span[], changes: ChangeDesc): Span[] {
    return spans
        .map((span) => ({
            start: changes.mapPos(span.start, 1),
            end: changes.mapPos(span.end, -1),
        }))
        .filter((span) => span.end > span.start);
}

function annotatePreviewControlRegions(preview: HTMLElement): void {
    const regions = new Set(
        [...preview.querySelectorAll(".dd-preview-control-keyword")]
            .map((keyword) => keyword.closest("blockquote"))
            .filter((region): region is HTMLQuoteElement => region !== null),
    );
    for (const region of regions) {
        region.classList.add("dd-preview-control-region");
        region.title = "Conditional dialogue";
    }
}

/** The stage rows for the reverse Jump-to menu, each carrying the current source selection. */
function jumpMenuItems(
    view: EditorView,
    jumpTargets: readonly SourceJumpTarget[],
): ContextMenuItem[] {
    const { from, to } = view.state.selection.main;
    return jumpTargets.map((target) => ({
        label: target.title,
        run: () => target.run(from, to),
        // On hover, preview the span this jump would land on — the enclosing node in that stage.
        onHover: () => {
            const span = target.preview(from, to);
            setJumpPreview(view, span ? { from: span.start, to: span.end } : null);
        },
        onBlur: () => setJumpPreview(view, null),
    }));
}

/** The caret's screen coordinates, or `null` off-screen or without layout (e.g. jsdom throws). */
function caretCoords(view: EditorView): { left: number; bottom: number } | null {
    try {
        return view.coordsAtPos(view.state.selection.main.head);
    } catch {
        return null;
    }
}

/** Open the reverse Jump-to picker at the caret — the keyboard entry to the "shortcut series". */
function openJumpMenuAtCaret(view: EditorView, jumpTargets: readonly SourceJumpTarget[]): boolean {
    if (jumpTargets.length === 0) return false;
    const editor = view.dom.getBoundingClientRect();
    const caret = caretCoords(view);
    openContextMenu(
        new MouseEvent("contextmenu", {
            clientX: caret ? caret.left : editor.left + 8,
            clientY: caret ? caret.bottom : editor.top + 8,
        }),
        jumpMenuItems(view, jumpTargets),
        () => setJumpPreview(view, null),
    );
    return true;
}

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
    /** Language-owned jump targets shown in the fixed, read-only bottom panel. */
    reservedTargets?: readonly ReservedTarget[];
    /**
     * Reverse-navigation destinations for the **Jump to ▸ \<stage\>** context menu: each
     * compiler-stage tab a source selection can reach, with `run` revealing the node that
     * encloses the selection. Empty for a bare render with no stages.
     */
    jumpTargets?: readonly SourceJumpTarget[];
    /** Optional line-debugger controller. Absent in every ordinary report. */
    debug?: DebugController;
}

/** One reverse-jump destination: a stage tab, and the action that reveals its enclosing node. */
export interface SourceJumpTarget {
    /** The destination stage tab's title, shown in the Jump-to submenu. */
    title: string;
    /** Reveal, in this stage, the node whose span encloses the source selection `[from, to)`. */
    run(from: number, to: number): void;
    /** The source span of that enclosing node, previewed on hover; `null` when none matches. */
    preview(from: number, to: number): Span | null;
}

/** A handle to a live source view, letting the mode controller reconfigure it in place. */
export interface SourceViewHandle {
    /** The source-view element (editor + divider + preview) to mount. */
    readonly element: HTMLElement;
    /** Release the editor, listeners, and optional debugger subscription. */
    destroy(): void;
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
    /** Replace the language-owned targets shown below the source document. */
    setReservedTargets(targets: readonly ReservedTarget[]): void;
    /**
     * Select the half-open `[from, to)` range, scroll it into view, and focus the editor — a
     * "jump to source" landing on the text a graph node came from. A zero-width range (`from ===
     * to`, a synthetic node's caret position) places the cursor there instead of selecting. The
     * editor need not be editable: a read-only (View) editor is still selectable and focusable.
     */
    selectRange(from: number, to: number): void;
    /**
     * Resolve an LSP line/character range to a half-open `[start, end)` offset pair against the
     * current buffer. Exposed so a caller that holds LSP-shaped data — the Problems panel — can
     * navigate without reaching for the editor state, and so it resolves positions exactly the
     * way the diagnostics overlay does.
     */
    resolveRange(range: LspRange): { start: number; end: number };
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
                  surroundHandlers,
                  ...editableExtras,
                  // Smart Tab, like a usual editor: Tab indents at a line's front (or a multi-line
                  // selection) and inserts spaces mid-line; Shift-Tab outdents. Low precedence so an
                  // open completion's Tab (accept) wins first; Escape blurs the editor so
                  // Tab-to-indent is not a keyboard trap (a completion/search dismiss, at higher
                  // precedence, consumes Escape first).
                  Prec.low(
                      keymap.of([
                          { key: "Tab", run: smartTab, shift: indentLess },
                          { key: "Escape", run: blurEditor },
                      ]),
                  ),
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
        reservedTargets = [],
        jumpTargets = [],
        debug,
    } = options;

    // The document-aware completions are an Edit-only authoring aid, so they live in the
    // editability compartment alongside the other Edit-only aids.
    const completion = dialogueAutocompletion(symbols);

    const container = document.createElement("div");
    container.className = "source-view";
    const sourcePane = document.createElement("div");
    sourcePane.className = "source-pane";
    let debugToolbar: DebugToolbar | null = null;

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
    let previewSemantics: PreviewSemantics = {
        ignored: [],
        controlKeywords: [],
    };
    const ignoredPreview = createIgnoredPreviewController(preview);
    const renderPreview = (value: string, delay = 0): void => {
        mountPreviewHtml(preview, renderDocument(value, previewSemantics));
        annotatePreviewControlRegions(preview);
        annotateHeadingAnchors(preview);
        ignoredPreview.refresh();
        mermaidPreviews.schedule(preview, delay);
    };
    renderPreview(source);
    // Delegated once on the stable preview element; each render re-annotates its headings.
    wireHeadingAnchorCopy(preview);

    // Re-render the preview and report the new buffer on every change (edits in Edit, or
    // a programmatic View-mode reload). The mode controller decides what to do with it.
    const onEdit = EditorView.updateListener.of((update) => {
        if (update.docChanged) {
            const value = update.state.doc.toString();
            previewSemantics = {
                ignored: mapPreviewSpans(previewSemantics.ignored, update.changes),
                controlKeywords: mapPreviewSpans(previewSemantics.controlKeywords, update.changes),
            };
            renderPreview(value, 200);
            onChange?.(value);
        }
    });

    // The editor's right-click menu, always installed so it opens in read-only View too: reverse
    // **Jump to ▸ <stage>** (whenever the report has stages), plus the surround actions in Edit.
    const contextMenu = EditorView.domEventHandlers({
        contextmenu(event, view) {
            const items: ContextMenuItem[] = [];
            if (jumpTargets.length > 0) {
                items.push({
                    icon: "go-to-file",
                    label: "Jump to",
                    submenu: jumpMenuItems(view, jumpTargets),
                });
            }
            if (!view.state.readOnly) {
                items.push(
                    { icon: "bold", label: "Bold", run: () => runInEditor(view, toggleWrap("**")) },
                    {
                        icon: "italic",
                        label: "Italic",
                        run: () => runInEditor(view, toggleWrap("*")),
                    },
                    {
                        icon: "strikethrough",
                        label: "Strikethrough",
                        run: () => runInEditor(view, toggleWrap("~~")),
                    },
                    { icon: "quote", label: "Quote", run: () => runInEditor(view, quoteSelection) },
                    {
                        icon: "remove",
                        label: "Unquote",
                        run: () => runInEditor(view, unquoteSelection),
                    },
                );
            }
            if (items.length === 0) return false;
            openContextMenu(event, items, () => setJumpPreview(view, null));
            return true;
        },
    });

    const view = new EditorView({
        parent: sourcePane,
        state: EditorState.create({
            doc: source,
            extensions: [
                ...(debug ? [debugEditor(debug)] : []),
                lineNumbers(),
                highlightActiveLineGutter(),
                foldGutter({ markerDOM: foldGutterMarker }),
                diagnosticsOverlay(),
                semanticTokensExtension(),
                reservedTargetsPanel(),
                jumpPreviewField,
                contextMenu,
                keymap.of([
                    { key: "Alt-j", run: (view) => openJumpMenuAtCaret(view, jumpTargets) },
                ]),
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
                sourceLanguage,
                syntaxHighlighting(markdownHighlightStyle),
                syntaxHighlighting(yamlHighlightStyle),
                EditorView.lineWrapping,
                // Indent with two spaces (Tab / Shift-Tab and the smart-Tab insert all use this).
                indentUnit.of("  "),
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
    setEditorReservedTargets(view, reservedTargets);
    if (debug) {
        debugToolbar = createDebugToolbar(debug, {
            toggleBreakpoint: () => {
                toggleBreakpointAt(view, view.state.selection.main.head);
                view.focus();
            },
        });
        sourcePane.prepend(debugToolbar.element);
    }

    const previewShell = document.createElement("div");
    previewShell.className = "source-preview-shell";
    previewShell.append(preview, ignoredPreview.footer);
    container.append(sourcePane, divider, previewShell);
    const disposeSplitDivider = initSplitDivider(container, divider);

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
        storageKey: "dd-preview-collapsed",
        name: "preview",
    });
    divider.appendChild(previewPanel.button);

    return {
        element: container,
        destroy: () => {
            narrow?.removeEventListener("change", syncScrollWithLayout);
            disposeScrollSync?.();
            disposeSplitDivider();
            debugToolbar?.destroy();
            ignoredPreview.destroy();
            mermaidPreviews.dispose(preview);
            view.destroy();
        },
        setEditable: (next) =>
            view.dispatch({ effects: editability.reconfigure(editableConfig(next, [completion])) }),
        setContent: (next) =>
            view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: next } }),
        getContent: () => view.state.doc.toString(),
        setDiagnostics: (diagnostics) => setEditorDiagnostics(view, diagnostics),
        setSemanticTokens: (tokens) => {
            setEditorSemanticTokens(view, tokens);
            const spansOf = (kind: SemanticToken["kind"]): Span[] =>
                tokens
                    .filter((token) => token.kind === kind)
                    .map((token) => ({
                        start: positionToOffset(view.state, token.range.start),
                        end: positionToOffset(view.state, token.range.end),
                    }));
            previewSemantics = {
                ignored: spansOf("IgnoredMarkdown"),
                controlKeywords: spansOf("ControlKeyword"),
            };
            renderPreview(view.state.doc.toString());
        },
        setReservedTargets: (targets) => setEditorReservedTargets(view, targets),
        resolveRange: (range) => {
            const start = positionToOffset(view.state, range.start);
            return { start, end: Math.max(start, positionToOffset(view.state, range.end)) };
        },
        selectRange: (from, to) => {
            // Clamp to the document and order the pair, so a stale span can only ever land the
            // cursor in-bounds rather than throw. A zero-width range collapses to a caret.
            const max = view.state.doc.length;
            const start = Math.max(0, Math.min(from, max));
            const end = Math.max(start, Math.min(to, max));
            view.dispatch({
                selection: EditorSelection.single(start, end),
                scrollIntoView: true,
            });
            view.focus();
        },
    };
}

/** Wire the divider so dragging it re-proportions the source pane (via a CSS split variable). */
export function initSplitDivider(
    container: HTMLElement,
    divider: HTMLElement,
    splitVar = "--source-split",
    collapsedClass = "preview-collapsed",
): () => void {
    let dragging = false;

    const onMouseDown = (event: MouseEvent): void => {
        // A collapsed side panel has nothing to resize — the divider is just its re-open
        // handle, so ignore drags (the toggle itself already swallows its own mousedown).
        if (container.classList.contains(collapsedClass)) return;
        dragging = true;
        document.body.style.userSelect = "none";
        event.preventDefault();
    };

    const onMouseMove = (event: MouseEvent): void => {
        if (!dragging) return;
        const bounds = container.getBoundingClientRect();
        // Resize along the split axis: horizontal side by side, vertical when stacked.
        const vertical = getComputedStyle(container).flexDirection === "column";
        const extent = vertical ? bounds.height : bounds.width;
        if (extent === 0) return;
        const offset = vertical ? event.clientY - bounds.top : event.clientX - bounds.left;
        const clamped = Math.max(MIN_RATIO, Math.min(MAX_RATIO, offset / extent));
        container.style.setProperty(splitVar, `${(clamped * 100).toFixed(2)}%`);
    };

    const onMouseUp = (): void => {
        dragging = false;
        document.body.style.userSelect = "";
    };

    divider.addEventListener("mousedown", onMouseDown);
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);

    return () => {
        divider.removeEventListener("mousedown", onMouseDown);
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
        if (dragging) document.body.style.userSelect = "";
        dragging = false;
    };
}
