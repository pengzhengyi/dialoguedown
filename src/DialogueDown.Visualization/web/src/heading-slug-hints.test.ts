import { describe, it, expect, vi } from "vitest";
import { EditorState } from "@codemirror/state";
import { markdown } from "@codemirror/lang-markdown";
import { ensureSyntaxTree, syntaxTree } from "@codemirror/language";
import { activeHeadingSlug, needsRebuild } from "./heading-slug-hints";

/** An EditorState parsed as Markdown, with the main cursor at `offset`, and nothing more done. */
function stateAt(doc: string, offset: number): EditorState {
    return EditorState.create({ doc, selection: { anchor: offset }, extensions: [markdown()] });
}

/**
 * The same state, with a tree that covers the whole document.
 *
 * `stateAt` alone is not enough to assert a slug against: the editor's first parse is bounded (20ms
 * of wall clock, counted with `Date.now()`, over at most the first 3000 characters), so a fresh
 * state's tree can stop mid-document, and which heading it reaches then depends on whether
 * anything stalled the parse. A case that means "this heading's slug is X" has to read a parse that
 * finished; only {@link stateWithUnparsedHeading} wants one that did not.
 */
function parsedStateAt(doc: string, offset: number): EditorState {
    const state = stateAt(doc, offset);
    ensureSyntaxTree(state, doc.length, 10_000);
    return state.update({}).state;
}

/**
 * A state whose heading sits past CodeMirror's bounded first parse, so its syntax tree is
 * genuinely incomplete — the same shape a small document takes when the 20ms parse budget
 * runs out under load.
 */
function stateWithUnparsedHeading(): { state: EditorState; docLength: number } {
    const filler = Array.from({ length: 4000 }, (_, i) => `Alice: line ${i}.`).join("\n\n");
    const doc = `${filler}\n\n# Far Heading\n`;
    // `stateAt`, not `parsedStateAt`: this case is about the tree *not* covering the document.
    return { state: stateAt(doc, doc.indexOf("# Far Heading") + 2), docLength: doc.length };
}

describe("activeHeadingSlug", () => {
    const doc = "# The Market\n\nMerchant: Hi\n\n# The Dark Forest\n";

    it("returns the slug of the heading on the cursor's line", () => {
        expect(activeHeadingSlug(parsedStateAt(doc, doc.indexOf("The Market")))).toBe("the-market");
        expect(activeHeadingSlug(parsedStateAt(doc, doc.indexOf("The Dark Forest")))).toBe(
            "the-dark-forest",
        );
    });

    // The regression the fix pins: a state whose first parse a stall cut short holds no slug for a
    // heading it never reached — the shape the full run failed on, where the file alone passed.
    it("reads the slug from a parse that finished, not from a stalled first parse", () => {
        let clock = 1_000_000;
        const spy = vi.spyOn(Date, "now").mockImplementation(() => (clock += 25));
        const stalled = stateAt(doc, doc.indexOf("The Dark Forest"));
        spy.mockRestore();

        // The stall is real: the tree stopped at the first heading, which is why this assertion
        // exists at all.
        expect(syntaxTree(stalled).length).toBeLessThan(doc.indexOf("The Dark Forest"));
        expect(activeHeadingSlug(stalled)).toBeNull();
        expect(activeHeadingSlug(parsedStateAt(doc, doc.indexOf("The Dark Forest")))).toBe(
            "the-dark-forest",
        );
    });

    it("returns null when the cursor is not on a heading line", () => {
        expect(activeHeadingSlug(parsedStateAt(doc, doc.indexOf("Merchant")))).toBeNull();
    });

    it("suffixes a duplicate heading in step with the preview's github-slugger", () => {
        const dup = "# Scene\n\ntext\n\n# Scene\n";
        expect(activeHeadingSlug(parsedStateAt(dup, dup.indexOf("# Scene") + 2))).toBe("scene");
        expect(activeHeadingSlug(parsedStateAt(dup, dup.lastIndexOf("# Scene") + 2))).toBe(
            "scene-1",
        );
    });

    it("returns null for a heading whose text slugs to nothing", () => {
        expect(activeHeadingSlug(parsedStateAt("# !!!\n", 3))).toBeNull();
    });

    it("ignores a '#' inside a fenced code block (a real heading node, not a regex match)", () => {
        const code = "```\n# not a heading\n```\n";
        expect(activeHeadingSlug(parsedStateAt(code, code.indexOf("# not") + 2))).toBeNull();
    });
});

describe("needsRebuild", () => {
    it("rebuilds on an edit and on a caret move", () => {
        const state = parsedStateAt("# Scene\n", 2);
        expect(needsRebuild(state.update({ changes: { from: 7, insert: "!" } }))).toBe(true);
        expect(needsRebuild(state.update({ selection: { anchor: 0 } }))).toBe(true);
    });

    it("leaves the hint alone for a transaction that changes nothing it reads", () => {
        expect(needsRebuild(parsedStateAt("# Scene\n", 2).update({}))).toBe(false);
    });

    it("rebuilds when a background parse finishes, though the transaction looks inert", () => {
        // CodeMirror's first parse is bounded (3000 chars, 20ms), so a heading can be missing
        // from the tree when the editor opens — under load this hits even a small document.
        const { state, docLength } = stateWithUnparsedHeading();
        expect(activeHeadingSlug(state)).toBeNull();

        // The parse worker advances the tree, then publishes it with a transaction that
        // changes neither the document nor the selection.
        ensureSyntaxTree(state, docLength, 10_000);
        const published = state.update({});
        expect(published.docChanged).toBe(false);
        expect(published.selection).toBeUndefined();

        expect(needsRebuild(published)).toBe(true);
        expect(activeHeadingSlug(published.state)).toBe("far-heading");
    });
});
