import { describe, expect, it } from "vitest";
import { sourceLanguage } from "./source-language";

function treeOf(source: string): string {
    return sourceLanguage.language.parser.parse(source).toString();
}

describe("sourceLanguage", () => {
    it("parses canonical leading front matter as YAML and the body as Markdown", () => {
        const tree = treeOf("---\ntitle: Scene 1\ntags: [intro]\n---\n# Arrival\n");

        expect(tree).toContain("Frontmatter(");
        expect(tree).toContain("Stream(Document(BlockMapping(");
        expect(tree).toContain("Pair");
        expect(tree).toContain("Document(ATXHeading1");
    });

    it("supports CRLF canonical delimiters", () => {
        expect(treeOf("---\r\ntitle: Scene 1\r\n---\r\nBody")).toContain("Frontmatter(");
    });

    it.each([
        ["no front matter", "# Scene\n---\nBody"],
        ["whitespace after a delimiter", "--- \ntitle: Scene\n---\nBody"],
    ])("leaves %s outside the front-matter region", (_, source) => {
        expect(treeOf(source)).not.toContain("Frontmatter(");
    });

    it("recovers an unterminated opener as editable YAML through the end of the document", () => {
        const tree = treeOf("---\ntitle: Scene");

        expect(tree).toContain("Frontmatter(");
        expect(tree).toContain("BlockMapping");
        expect(tree).toContain("⚠");
    });

    it("does not treat a YAML document-end marker as the canonical closing fence", () => {
        const tree = treeOf("---\ntitle: Scene\n...\nBody");

        expect(tree).toContain("Frontmatter(");
        expect(tree).toContain("DocEnd");
        expect(tree).toContain("⚠");
    });
});
