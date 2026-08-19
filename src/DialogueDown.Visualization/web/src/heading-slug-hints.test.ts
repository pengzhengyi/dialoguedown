import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { markdown } from "@codemirror/lang-markdown";
import { ensureSyntaxTree } from "@codemirror/language";
import { activeHeadingSlug, needsRebuild } from "./heading-slug-hints";

/** An EditorState parsed as Markdown, with the main cursor at `offset`. */
function stateAt(doc: string, offset: number): EditorState {
    return EditorState.create({ doc, selection: { anchor: offset }, extensions: [markdown()] });
}

/**
 * A state whose heading sits past CodeMirror's bounded first parse, so its syntax tree is
 * genuinely incomplete — the same shape a small document takes when the 20ms parse budget
 * runs out under load.
 */
function stateWithUnparsedHeading(): { state: EditorState; docLength: number } {
    const filler = Array.from({ length: 4000 }, (_, i) => `Alice: line ${i}.`).join("\n\n");
    const doc = `${filler}\n\n# Far Heading\n`;
    return { state: stateAt(doc, doc.indexOf("# Far Heading") + 2), docLength: doc.length };
}

describe("activeHeadingSlug", () => {
    const doc = "# The Market\n\nMerchant: Hi\n\n# The Dark Forest\n";

    it("returns the slug of the heading on the cursor's line", () => {
        expect(activeHeadingSlug(stateAt(doc, doc.indexOf("The Market")))).toBe("the-market");
        expect(activeHeadingSlug(stateAt(doc, doc.indexOf("The Dark Forest")))).toBe(
            "the-dark-forest",
        );
    });

    it("returns null when the cursor is not on a heading line", () => {
        expect(activeHeadingSlug(stateAt(doc, doc.indexOf("Merchant")))).toBeNull();
    });

    it("suffixes a duplicate heading in step with the preview's github-slugger", () => {
        const dup = "# Scene\n\ntext\n\n# Scene\n";
        expect(activeHeadingSlug(stateAt(dup, dup.indexOf("# Scene") + 2))).toBe("scene");
        expect(activeHeadingSlug(stateAt(dup, dup.lastIndexOf("# Scene") + 2))).toBe("scene-1");
    });

    it("returns null for a heading whose text slugs to nothing", () => {
        expect(activeHeadingSlug(stateAt("# !!!\n", 3))).toBeNull();
    });

    it("ignores a '#' inside a fenced code block (a real heading node, not a regex match)", () => {
        const code = "```\n# not a heading\n```\n";
        expect(activeHeadingSlug(stateAt(code, code.indexOf("# not") + 2))).toBeNull();
    });
});

describe("needsRebuild", () => {
    it("rebuilds on an edit and on a caret move", () => {
        const state = stateAt("# Scene\n", 2);
        expect(needsRebuild(state.update({ changes: { from: 7, insert: "!" } }))).toBe(true);
        expect(needsRebuild(state.update({ selection: { anchor: 0 } }))).toBe(true);
    });

    it("leaves the hint alone for a transaction that changes nothing it reads", () => {
        expect(needsRebuild(stateAt("# Scene\n", 2).update({}))).toBe(false);
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
