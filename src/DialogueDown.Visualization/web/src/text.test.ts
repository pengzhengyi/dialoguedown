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
    renderNodePreview,
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

describe("renderNodePreview", () => {
    it("wraps the bare marker for a Dialogue AST jump indicator", () => {
        expect(renderNodePreview("=>", "Jump indicator", true)).toContain(
            '<span class="jump-ligature">=&gt;</span>',
        );
    });

    it("wraps only the marker in an assembled downstream jump", () => {
        const html = renderNodePreview("=> [Go](#go)", "Jump", true);

        expect(html).toContain('<span class="jump-ligature">=&gt;</span> <a href="#go">Go</a>');
    });

    it("decorates recognized jump syntax in a parent node's preview", () => {
        expect(renderNodePreview("=> [Go](#go)", "Line", true)).toContain("jump-ligature");
    });

    it("does not cross a soft line break to decorate a dangling indicator", () => {
        expect(renderNodePreview("=>\n[Go](#go)", "Line", true)).not.toContain("jump-ligature");
    });

    it("does not promote the same source before Dialogue semantics exist", () => {
        expect(renderNodePreview("=> [Go](#go)", "Text")).not.toContain("jump-ligature");
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

    it("wraps a jump indicator before a rendered link for preview ligatures", () => {
        const html = renderDocument("=> [Go](#go)");

        expect(html).toContain('<span class="jump-ligature">=&gt;</span> <a href="#go">Go</a>');
    });

    it("does not wrap arrows in prose, inline code, or snippet previews", () => {
        expect(renderDocument("Alice: A => B")).not.toContain("jump-ligature");
        expect(renderDocument("Alice: `=> [Go](#go)`")).not.toContain("jump-ligature");
        expect(renderDocument("=>\n[Go](#go)")).not.toContain("jump-ligature");
        expect(renderMarkdown("=> [Go](#go)")).not.toContain("jump-ligature");
    });

    it.each([
        ["table", "| A | B |\n| - | - |\n| x | y |", "<table", "Table · 3 lines"],
        ["code block", "```mermaid\ngraph TD\n```", "<pre", "Code block · 3 lines"],
        ["divider", "---", "<hr", "Divider · 1 line"],
    ])("marks an ignored %s in the rendered preview", (_kind, source, element, summary) => {
        const html = renderDocument(source, {
            ignored: [{ start: 0, end: source.length }],
            controlKeywords: [],
        });

        expect(html).toContain('class="dd-preview-ignored-region"');
        expect(html).toContain(`data-ignored-summary="${summary}"`);
        expect(html).toContain(`${element} class="dd-preview-ignored"`);
    });

    it("leaves the same Markdown at full strength when the policy keeps it", () => {
        const source = "| A | B |\n| - | - |\n| x | y |";

        expect(renderDocument(source)).not.toContain("dd-preview-ignored");
    });

    it("marks an autolink when a configured policy ignores it", () => {
        const source = "<https://example.com>";
        const html = renderDocument(source, {
            ignored: [{ start: 0, end: source.length }],
            controlKeywords: [],
        });

        expect(html).toContain('<a class="dd-preview-ignored"');
        expect(html).toContain('title="Ignored autolink: &lt;https://example.com&gt;"');
    });

    it("does not mistake class= inside an autolink URL for an HTML class attribute", () => {
        const source = "<https://example.com/?class=route>";

        const html = renderDocument(source, {
            ignored: [{ start: 0, end: source.length }],
            controlKeywords: [],
        });

        expect(html).toContain('<a class="dd-preview-ignored"');
        expect(html).toContain('href="https://example.com/?class=route"');
    });

    it("shows ignored inline HTML as escaped source without unbalancing the Preview DOM", () => {
        const source = "Before <span>inside</span> after";
        const opening = source.indexOf("<span>");
        const closing = source.indexOf("</span>");

        const html = renderDocument(source, {
            ignored: [
                { start: opening, end: opening + "<span>".length },
                { start: closing, end: closing + "</span>".length },
            ],
            controlKeywords: [],
        });

        expect(html.match(/data-ignored-kind="Raw HTML"/g)).toHaveLength(2);
        expect(html).toContain("&lt;span&gt;");
        expect(html).toContain("&lt;/span&gt;");
        expect(html).not.toContain("<span>inside</span>");
    });

    it("marks a compiler-projected control keyword for region annotation", () => {
        const source = "> `if` `Ready?`\n>\n> Alice: Go.";
        const start = source.indexOf("`if`");

        const html = renderDocument(source, {
            ignored: [],
            controlKeywords: [{ start, end: start + "`if`".length }],
        });

        expect(html).toContain('<code class="dd-preview-control-keyword">if</code>');
    });

    it("matches an ignored table nested inside a blockquote", () => {
        const source = "> | A | B |\n> | - | - |\n> | x | y |";
        const start = source.indexOf("|");

        const html = renderDocument(source, {
            ignored: [{ start, end: source.length }],
            controlKeywords: [],
        });

        expect(html).toContain('data-ignored-summary="Table · 3 lines"');
    });

    it("preserves a literal quote marker inside an ignored blockquoted code block", () => {
        const source = "> ```text\n> > literal quote\n> ```";
        const start = source.indexOf("```");

        const html = renderDocument(source, {
            ignored: [{ start, end: source.length }],
            controlKeywords: [],
        });

        expect(html).toContain('data-ignored-kind="Code block"');
        expect(html).toContain("&gt; literal quote");
    });
});
