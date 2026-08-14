import { lintGutter, setDiagnostics, type Diagnostic as EditorDiagnostic } from "@codemirror/lint";
import type { EditorState, Extension } from "@codemirror/state";
import { tooltips, type EditorView } from "@codemirror/view";
import { positionToOffset } from "./lsp-position";
import type { LspDiagnostic, LspSeverity } from "./model";
import { orderDiagnostics, orderGutterDiagnostics } from "./diagnostic-order";

/** The docs page whose per-code anchors the tooltip links to (mirrors the CLI's doc links). */
const ERROR_CODES_PAGE = "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html";

/** LSP severity numbers to the lint UI's kinds (which drive the squiggle and gutter color). */
const SEVERITY_KIND: Record<LspSeverity, EditorDiagnostic["severity"]> = {
    1: "error",
    2: "warning",
    3: "info",
    4: "hint",
};

/**
 * The always-on overlay extension: the lint gutter markers. The underlines and hover
 * tooltips are enabled by {@link setEditorDiagnostics} the first time diagnostics are
 * pushed, so no linting source runs in the browser — the diagnostics come from the .NET
 * compiler.
 */
export function diagnosticsOverlay(): Extension {
    return [
        lintGutter({ tooltipFilter: (diagnostics) => orderGutterDiagnostics(diagnostics) }),
        // The Source pane clips its editor to preserve the split layout. Portal fixed-position
        // tooltips to the viewport so diagnostic popovers can cross that boundary.
        tooltips({ parent: document.body, position: "fixed" }),
    ];
}

/**
 * Push the report's diagnostics into the editor, resolving each LSP line/character range to
 * a document offset against the current buffer. An empty list clears the overlay (a clean
 * compile). Called on load, on a View-mode hot-reload, and after each Edit-mode save.
 */
export function setEditorDiagnostics(
    view: EditorView,
    diagnostics: readonly LspDiagnostic[],
): void {
    const editorDiagnostics = toEditorDiagnostics(view.state, diagnostics);
    view.dispatch(setDiagnostics(view.state, editorDiagnostics));
}

/** Resolve a canonically ordered diagnostic set to CodeMirror offsets. */
export function toEditorDiagnostics(
    state: EditorState,
    diagnostics: readonly LspDiagnostic[],
): EditorDiagnostic[] {
    return orderDiagnostics(diagnostics).map((diagnostic) => toEditorDiagnostic(state, diagnostic));
}

/**
 * Convert one LSP-shaped diagnostic to a CodeMirror lint diagnostic: its range resolved to
 * offsets, its severity mapped to a lint kind, and a tooltip that links to the code's docs.
 * Exported for unit testing.
 */
export function toEditorDiagnostic(
    state: EditorState,
    diagnostic: LspDiagnostic,
): EditorDiagnostic {
    const from = positionToOffset(state, diagnostic.range.start);
    const to = Math.max(from, positionToOffset(state, diagnostic.range.end));
    return {
        from,
        to,
        severity: SEVERITY_KIND[diagnostic.severity] ?? "error",
        message: diagnostic.message,
        renderMessage: () => renderDiagnosticTooltip(diagnostic),
    };
}

/** The docs URL for a diagnostic code — the error-codes page anchored at its lowercase slug. */
export function errorCodeUrl(code: string): string {
    return `${ERROR_CODES_PAGE}#${code.toLowerCase()}`;
}

/** The hover tooltip: the diagnostic message above a "more information" link to its docs entry. */
export function renderDiagnosticTooltip(diagnostic: LspDiagnostic): HTMLElement {
    const container = document.createElement("div");
    container.className = "diagnostic-tooltip";

    const message = document.createElement("div");
    message.className = "diagnostic-tooltip-message";
    message.textContent = diagnostic.message;
    container.append(message);

    const link = document.createElement("a");
    link.className = "diagnostic-tooltip-link";
    link.href = errorCodeUrl(diagnostic.code);
    link.target = "_blank";
    link.rel = "noreferrer";
    link.textContent = `${diagnostic.code} — more information`;
    container.append(link);

    return container;
}
