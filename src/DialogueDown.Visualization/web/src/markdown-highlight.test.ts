import { type Tag, tags } from "@lezer/highlight";
import { describe, expect, it } from "vitest";
import { markdownHighlightStyle, yamlHighlightStyle } from "./source-view";

/** The Markdown layer under the compiler's tokens: a blockquote is not muted, and a comment is. */
describe("markdownHighlightStyle", () => {
    const styled = (tag: Tag) => markdownHighlightStyle.style([tag]);

    it("does not mute blockquotes", () => {
        // A marker-headed quote is a control block and any other quote is a transparent wrapper,
        // so every blockquote is live dialogue, colored by the compiler's own tokens.
        expect(styled(tags.quote)).toBeNull();
    });

    it("styles comments, which never reach the compiler", () => {
        // The compiler always leaves a comment out, so the editor's own parser can style it.
        expect(styled(tags.comment)).not.toBeNull();
    });

    it("still styles the Markdown a script is made of", () => {
        expect(styled(tags.heading)).not.toBeNull();
        expect(styled(tags.monospace)).not.toBeNull();
    });

    it("styles the front-matter delimiter as metadata", () => {
        expect(styled(tags.meta)).not.toBeNull();
    });
});

describe("yamlHighlightStyle", () => {
    const styled = (tag: Tag) => yamlHighlightStyle.style([tag]);

    it("uses the Config editor's metadata palette", () => {
        expect(styled(tags.definition(tags.propertyName))).not.toBeNull();
        expect(styled(tags.string)).not.toBeNull();
        expect(styled(tags.content)).not.toBeNull();
        expect(styled(tags.lineComment)).not.toBeNull();
        expect(styled(tags.bracket)).not.toBeNull();
        expect(styled(tags.squareBracket)).not.toBeNull();
    });
});
