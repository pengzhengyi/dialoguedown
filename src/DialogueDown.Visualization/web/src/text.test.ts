// @vitest-environment node

import { describe, it, expect } from "vitest";
import {
    MAX_INLINE_TEXT,
    escapeHtml,
    ellipsize,
    baseLabel,
    tooltipHtml,
    splitFrontMatter,
    renderMarkdown,
    renderDocument,
} from "./text";
import type { DisplayNode } from "./model";

describe("escapeHtml", () => {
    it("escapes the five HTML-significant characters", () => {
        expect(escapeHtml(`&<>"'`)).toBe("&amp;&lt;&gt;&quot;&#39;");
    });

    it("leaves ordinary text untouched", () => {
        expect(escapeHtml("Alice: hello")).toBe("Alice: hello");
    });

    it("escapes every occurrence, not just the first", () => {
        expect(escapeHtml("<<")).toBe("&lt;&lt;");
    });
});

describe("ellipsize", () => {
    it("returns the text unchanged when at or below the maximum", () => {
        expect(ellipsize("hello", 5)).toBe("hello");
        expect(ellipsize("hi", 5)).toBe("hi");
    });

    it("truncates and appends an ellipsis when over the maximum", () => {
        const result = ellipsize("abcdefgh", 5);
        expect(result).toBe("abcd…");
        expect(result).toHaveLength(5);
    });

    it("respects the MAX_INLINE_TEXT budget", () => {
        const long = "x".repeat(MAX_INLINE_TEXT + 10);
        expect(ellipsize(long, MAX_INLINE_TEXT)).toHaveLength(MAX_INLINE_TEXT);
    });
});

describe("baseLabel", () => {
    it("strips a trailing parenthetical", () => {
        expect(baseLabel("Heading (H2)")).toBe("Heading");
    });

    it("returns labels without a parenthetical unchanged", () => {
        expect(baseLabel("Text")).toBe("Text");
    });

    it("only strips a parenthetical at the end", () => {
        expect(baseLabel("List (ordered) item")).toBe("List (ordered) item");
    });
});

describe("tooltipHtml", () => {
    it("renders a bold label followed by a div per attribute", () => {
        const node: DisplayNode = {
            id: "n1",
            label: "Heading (H2)",
            attributes: [
                { name: "level", value: "2" },
                { name: "text", value: "Scene" },
            ],
        };
        expect(tooltipHtml(node)).toBe(
            "<strong>Heading (H2)</strong><div>level: 2</div><div>text: Scene</div>",
        );
    });

    it("escapes the label and attribute values", () => {
        const node: DisplayNode = {
            id: "n1",
            label: "<b>",
            attributes: [{ name: "raw", value: "a<b>c" }],
        };
        expect(tooltipHtml(node)).toBe("<strong>&lt;b&gt;</strong><div>raw: a&lt;b&gt;c</div>");
    });

    it("renders just the label when there are no attributes", () => {
        const node: DisplayNode = { id: "n1", label: "Document", attributes: [] };
        expect(tooltipHtml(node)).toBe("<strong>Document</strong>");
    });
});

describe("splitFrontMatter", () => {
    it("peels a leading YAML front matter block off the body", () => {
        const source = "---\ntitle: Scene\n---\n# Heading\n";
        expect(splitFrontMatter(source)).toEqual({
            frontMatter: "title: Scene",
            body: "# Heading\n",
        });
    });

    it("handles CRLF line endings", () => {
        const source = "---\r\ntitle: Scene\r\n---\r\nbody";
        expect(splitFrontMatter(source)).toEqual({ frontMatter: "title: Scene", body: "body" });
    });

    it("returns null front matter when the source does not start with a fence", () => {
        const source = "# Heading\n---\nnot front matter\n";
        expect(splitFrontMatter(source)).toEqual({ frontMatter: null, body: source });
    });
});

describe("renderMarkdown", () => {
    it("renders ordinary Markdown to HTML", () => {
        expect(renderMarkdown("# Scene")).toContain("<h1>Scene</h1>");
    });

    it("shows front matter as a labeled block rather than a heading", () => {
        const html = renderMarkdown("---\ntitle: Scene\n---\nBody text");
        expect(html).toContain('<p class="frontmatter-label">Front matter</p>');
        expect(html).toContain('<pre class="frontmatter"><code>title: Scene</code></pre>');
        expect(html).toContain("Body text");
        expect(html).not.toContain("<h1>");
    });

    it("escapes front matter content", () => {
        const html = renderMarkdown("---\nnote: a<b>\n---\n");
        expect(html).toContain("a&lt;b&gt;");
    });
});

describe("renderDocument", () => {
    it("adds GitHub-style heading ids so in-document anchor links resolve", () => {
        const html = renderDocument("## Discuss Bob's photo");
        expect(html).toContain('id="discuss-bobs-photo"');
    });

    it("handles front matter like renderMarkdown, but with heading ids", () => {
        const html = renderDocument("---\ntitle: X\n---\n# Scene");
        expect(html).toContain('<p class="frontmatter-label">Front matter</p>');
        expect(html).toContain('id="scene"');
    });

    it("leaves renderMarkdown id-free, so snippet previews cannot collide", () => {
        expect(renderMarkdown("## Heading")).not.toContain("id=");
    });

    it("joins single newlines as soft breaks, matching CommonMark and VSCode", () => {
        expect(renderDocument("Alice: hi.\n=> [Go](#go)")).not.toContain("<br>");
        expect(renderMarkdown("first line\nsecond line")).not.toContain("<br>");
    });

    it("honors explicit hard breaks (two trailing spaces or a trailing backslash)", () => {
        expect(renderDocument("first line  \nsecond line")).toContain("<br>");
        expect(renderMarkdown("first line\\\nsecond line")).toContain("<br>");
    });
});
