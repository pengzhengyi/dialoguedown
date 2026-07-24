import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { decoratedRanges, semanticTokens, setEditorSemanticTokens } from "./semantic-tokens";
import type { LspRange, SemanticToken, TokenKind } from "./model";

/** A zero-based LSP range on a single line, from character `start` to `end`. */
function range(line: number, start: number, end: number): LspRange {
    return { start: { line, character: start }, end: { line, character: end } };
}

/** A semantic token of `kind` over a single line's `[start, end)` characters. */
function token(kind: TokenKind, line: number, start: number, end: number): SemanticToken {
    return { kind, range: range(line, start, end) };
}

describe("decoratedRanges", () => {
    it("resolves a token's LSP range to editor offsets and its class", () => {
        const state = EditorState.create({ doc: "Alice: Hi." });

        const ranges = decoratedRanges(state, [token("SpeakerName", 0, 0, 5)]);

        expect(ranges).toEqual([{ from: 0, to: 5, className: "dd-tok-speaker-name" }]);
    });

    it("maps each kind to its own class", () => {
        const state = EditorState.create({ doc: "x" });
        const kinds: TokenKind[] = [
            "SpeakerName",
            "SpeakerId",
            "Separator",
            "CustomTag",
            "ReservedTag",
            "JumpIndicator",
        ];

        const classes = kinds.map(
            (kind) => decoratedRanges(state, [token(kind, 0, 0, 1)])[0].className,
        );

        expect(classes).toEqual([
            "dd-tok-speaker-name",
            "dd-tok-speaker-id",
            "dd-tok-separator",
            "dd-tok-custom-tag",
            "dd-tok-reserved-tag",
            "dd-tok-jump",
        ]);
    });

    it("resolves a token that spans across lines", () => {
        const state = EditorState.create({ doc: "Alice: Hi.\nBob: Yo." });
        // Line 1 (zero-based) starts at offset 11; "Bob" is characters 0..3 there.
        const ranges = decoratedRanges(state, [token("SpeakerName", 1, 0, 3)]);

        expect(ranges).toEqual([{ from: 11, to: 14, className: "dd-tok-speaker-name" }]);
    });

    it("orders the disjoint speaker parts by start", () => {
        const state = EditorState.create({ doc: "Alice @alice #happy: Hi." });
        // Name, id, tag, separator: given out of order, they come back ordered by start.
        const ranges = decoratedRanges(state, [
            token("Separator", 0, 19, 20),
            token("SpeakerName", 0, 0, 5),
            token("CustomTag", 0, 13, 19),
            token("SpeakerId", 0, 6, 12),
        ]);

        expect(ranges.map((r) => r.className)).toEqual([
            "dd-tok-speaker-name",
            "dd-tok-speaker-id",
            "dd-tok-custom-tag",
            "dd-tok-separator",
        ]);
    });

    it("drops a zero-width token (a synthetic node has no text to color)", () => {
        const state = EditorState.create({ doc: "Alice: Hi." });

        expect(decoratedRanges(state, [token("SpeakerName", 0, 3, 3)])).toEqual([]);
    });

    it("clamps a stale range past the end of the shrunken buffer", () => {
        const state = EditorState.create({ doc: "Hi." });

        // A token on a line that no longer exists clamps to the document end; both ends land
        // on the end offset, so it collapses to zero width and is dropped rather than throwing.
        expect(decoratedRanges(state, [token("SpeakerName", 9, 0, 5)])).toEqual([]);
    });
});

describe("semanticTokens extension", () => {
    /** Mount an editor with the highlighting extension over `doc`. */
    function mount(doc: string): EditorView {
        const parent = document.createElement("div");
        document.body.appendChild(parent);
        return new EditorView({
            parent,
            state: EditorState.create({ doc, extensions: [semanticTokens()] }),
        });
    }

    /** The decorated `[from, to)` ranges the editor currently paints, in order. */
    function decorations(view: EditorView): Array<{ from: number; to: number }> {
        const out: Array<{ from: number; to: number }> = [];
        for (const source of view.state.facet(EditorView.decorations)) {
            const ranges = typeof source === "function" ? source(view) : source;
            ranges.between(0, view.state.doc.length, (from, to) => {
                out.push({ from, to });
            });
        }
        return out;
    }

    it("paints nothing until tokens are pushed", () => {
        const view = mount("Alice: Hi.");
        expect(decorations(view)).toEqual([]);
        view.destroy();
    });

    it("paints the pushed tokens' ranges", () => {
        const view = mount("Alice: Hi.");

        setEditorSemanticTokens(view, [token("SpeakerName", 0, 0, 7)]);

        expect(decorations(view)).toEqual([{ from: 0, to: 7 }]);
        view.destroy();
    });

    it("replaces the tokens on the next push (an empty list clears them)", () => {
        const view = mount("Alice: Hi.");
        setEditorSemanticTokens(view, [token("SpeakerName", 0, 0, 7)]);

        setEditorSemanticTokens(view, []);

        expect(decorations(view)).toEqual([]);
        view.destroy();
    });

    it("maps the decorations through edits so they track until the next compile", () => {
        const view = mount("Alice: Hi.");
        setEditorSemanticTokens(view, [token("SpeakerName", 0, 0, 7)]);

        // Insert two characters before the token; its range shifts right by two.
        view.dispatch({ changes: { from: 0, insert: "xx" } });

        expect(decorations(view)).toEqual([{ from: 2, to: 9 }]);
        view.destroy();
    });
});
