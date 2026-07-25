import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { markdown } from "@codemirror/lang-markdown";
import { activeHeadingSlug } from "./heading-slug-hints";

/** An EditorState parsed as Markdown, with the main cursor at `offset`. */
function stateAt(doc: string, offset: number): EditorState {
    return EditorState.create({ doc, selection: { anchor: offset }, extensions: [markdown()] });
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
