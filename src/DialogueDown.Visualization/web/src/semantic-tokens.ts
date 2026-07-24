import { type EditorState, type Extension, StateEffect, StateField } from "@codemirror/state";
import { Decoration, type DecorationSet, EditorView } from "@codemirror/view";
import { positionToOffset } from "./lsp-position";
import type { SemanticToken, TokenKind } from "./model";

/**
 * The CSS class each token kind decorates its range with. A future kind is added here and
 * styled in `styles.css`; nothing else in the editor changes.
 */
const TOKEN_CLASS: Record<TokenKind, string> = {
    SpeakerName: "dd-tok-speaker-name",
    SpeakerId: "dd-tok-speaker-id",
    Separator: "dd-tok-separator",
    CustomTag: "dd-tok-custom-tag",
    ReservedTag: "dd-tok-reserved-tag",
    JumpIndicator: "dd-tok-jump",
};

/** One token resolved to editor offsets and its decoration class. */
export interface DecoratedRange {
    from: number;
    to: number;
    className: string;
}

/**
 * Resolve each token's LSP range to editor offsets and its class, dropping zero-width tokens
 * (a synthetic node has no text to color). The compiler's tokens are disjoint, so they only
 * need ordering by start for CodeMirror's sorted decoration set. Exported for testing.
 */
export function decoratedRanges(
    state: EditorState,
    tokens: readonly SemanticToken[],
): DecoratedRange[] {
    return tokens
        .map((token) => ({
            from: positionToOffset(state, token.range.start),
            to: positionToOffset(state, token.range.end),
            className: TOKEN_CLASS[token.kind],
        }))
        .filter((range) => range.to > range.from && range.className != null)
        .sort((a, b) => a.from - b.from);
}

/** Build the decoration set for `tokens` against the current document. */
function buildDecorations(state: EditorState, tokens: readonly SemanticToken[]): DecorationSet {
    const marks = decoratedRanges(state, tokens).map((range) =>
        Decoration.mark({ class: range.className }).range(range.from, range.to),
    );
    return Decoration.set(marks, true);
}

/** Replace the editor's semantic-token decorations (a recompile pushed a new set). */
const setSemanticTokensEffect = StateEffect.define<readonly SemanticToken[]>();

/**
 * The decoration field: it maps its ranges through document changes so the coloring tracks
 * edits until the next compile, and rebuilds when {@link setEditorSemanticTokens} pushes a
 * fresh set. It holds no source of its own — the tokens come from the .NET compiler.
 */
const semanticTokenField = StateField.define<DecorationSet>({
    create: () => Decoration.none,
    update(decorations, transaction) {
        let mapped = decorations.map(transaction.changes);
        for (const effect of transaction.effects) {
            if (effect.is(setSemanticTokensEffect)) {
                mapped = buildDecorations(transaction.state, effect.value);
            }
        }
        return mapped;
    },
    provide: (field) => EditorView.decorations.from(field),
});

/**
 * The always-on highlighting extension: an empty decoration field until the first
 * {@link setEditorSemanticTokens} pushes the compiler's tokens. No lexer runs in the browser
 * — the tokens are projected by the .NET compiler and carried in the report payload.
 */
export function semanticTokens(): Extension {
    return semanticTokenField;
}

/**
 * Push the report's semantic tokens into the editor, resolving each LSP range to offsets
 * against the current buffer. An empty list clears the highlighting. Called on load, on a
 * View-mode hot-reload, and after each Edit-mode save.
 */
export function setEditorSemanticTokens(view: EditorView, tokens: readonly SemanticToken[]): void {
    view.dispatch({ effects: setSemanticTokensEffect.of(tokens) });
}
