import { type Tag, tags } from "@lezer/highlight";
import { describe, expect, it } from "vitest";
import { markdownHighlightStyle } from "./source-view";

/**
 * The Markdown layer under the compiler's tokens. These assertions pin two decisions that are
 * easy to undo by reflex: a blockquote must not be muted, and a comment must be.
 */
describe("markdownHighlightStyle", () => {
    const styled = (tag: Tag) => markdownHighlightStyle.style([tag]);

    it("does not mute blockquotes", () => {
        // A marker-headed quote is a control block and any other quote is a transparent wrapper,
        // so every blockquote is live dialogue. Muting it grayed out the liveliest construct in
        // the language and overrode the compiler's own tokens inside it.
        expect(styled(tags.quote)).toBeNull();
    });

    it("styles comments, which never reach the compiler", () => {
        // A comment is always left out, unconditionally, so the editor's own parser can style it
        // — no projection needed for a fate that never varies.
        expect(styled(tags.comment)).not.toBeNull();
    });

    it("still styles the Markdown a script is made of", () => {
        expect(styled(tags.heading)).not.toBeNull();
        expect(styled(tags.monospace)).not.toBeNull();
    });
});
