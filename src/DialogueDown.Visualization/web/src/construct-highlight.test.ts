import { describe, expect, it } from "vitest";
import { annotatePreviewConstructs, type PreviewConstruct } from "./construct-highlight";
import type { Span, TokenKind } from "./model";

/** A construct as the compiler projected it, over `text` written at `[start, end)`. */
function construct(kind: TokenKind, text: string, start = 0): PreviewConstruct {
    const span: Span = { start, end: start + text.length };
    return { kind, text, span };
}

function preview(html: string, constructs: readonly PreviewConstruct[]): HTMLElement {
    const root = document.createElement("div");
    root.className = "source-preview preview";
    root.innerHTML = html;
    annotatePreviewConstructs(root, constructs);
    return root;
}

describe("annotatePreviewConstructs", () => {
    it("renders a custom tag as the shared capsule, dot and copy affordance included", () => {
        const root = preview("<p>Alice #happy is here.</p>", [construct("CustomTag", "#happy", 6)]);

        const chip = root.querySelector(".dd-tag");

        expect(chip?.classList.contains("dd-tag-custom")).toBe(true);
        expect(chip?.textContent).toBe("#happy");
        expect(chip?.getAttribute("data-copy")).toBe("#happy");
        expect(chip?.querySelector(".dd-tag-dot")?.getAttribute("style")).toContain("--dd-tag-hue");
        expect(root.querySelector("p")?.textContent).toBe("Alice #happy is here.");
    });

    it("renders a reserved tag as the reserved capsule, which needs no dot", () => {
        const root = preview("<p>Narrator ##default: Hi.</p>", [
            construct("ReservedTag", "##default", 9),
        ]);

        const chip = root.querySelector(".dd-tag");

        expect(chip?.classList.contains("dd-tag-reserved")).toBe(true);
        expect(chip?.textContent).toBe("##default");
        expect(chip?.querySelector(".dd-tag-dot")).toBeNull();
    });

    it("reads a tag's value into the capsule it labels", () => {
        const root = preview("<p>Bob #mood=happy: Hi.</p>", [
            construct("CustomTag", "#mood=happy", 4),
        ]);

        expect(root.querySelector(".dd-tag")?.textContent).toBe("#mood=happy");
        expect(root.querySelector(".dd-tag")?.getAttribute("data-copy")).toBe("#mood=happy");
    });

    it.each([
        ["SpeakerName", "Guide", "Guide", "dd-tok-speaker-name"],
        ["SpeakerId", "@guide", "@guide", "dd-tok-speaker-id"],
        ["Command", '`playSound("wind")`', 'playSound("wind")', "dd-tok-command"],
        ["Query", '`"playerName"`', '"playerName"', "dd-tok-query"],
        ["Condition", "`Alice.HasMap?`", "Alice.HasMap?", "dd-tok-condition"],
        ["StaticWeight", "`70%`", "70%", "dd-tok-static-weight"],
        ["DynamicWeight", "`Bob.Affection?`", "Bob.Affection?", "dd-tok-dynamic-weight"],
        ["ReservedAnchor", "#END", "#END", "dd-tok-reserved-anchor"],
    ] as const)("tints a %s with the editor's own class", (kind, written, shown, className) => {
        const root = preview(`<p><code>${shown}</code></p>`, [construct(kind, written, 1)]);

        const mark = root.querySelector(`.${className}`);

        // The compiler's token covers the backticks; the mark carries the code's own text.
        expect(mark?.textContent).toBe(shown);
        expect(mark?.closest("code")).not.toBeNull();
    });

    it("marks a speaker's name only where it opens a line's prefix", () => {
        const root = preview("<p>Bob asked Alice yesterday.</p><p>Alice: Hi.</p>", [
            construct("SpeakerName", "Alice"),
        ]);

        expect(root.querySelectorAll(".dd-tok-speaker-name")).toHaveLength(1);
        expect(root.querySelector(".dd-tok-speaker-name")?.textContent).toBe("Alice");
    });

    it("explains what each ask-me mark is on hover", () => {
        const root = preview('<p><code>("fade in")</code></p>', [
            construct("Command", '`("fade in")`', 1),
        ]);

        expect(root.querySelector(".dd-tok-command")?.getAttribute("data-tip")).toBe(
            "A command — the host is asked to perform this.",
        );
    });

    it("marks the jump indicator and lets it reveal its own line", () => {
        const root = preview('<p>=> <a href="#the-market">The market</a></p>', [
            construct("JumpIndicator", "=>", 0),
        ]);

        const jump = root.querySelector(".dd-tok-jump");

        expect(jump?.textContent).toBe("=>");
        expect(jump?.getAttribute("data-span")).toBe("0:2");
    });

    it("marks a whole code span, but not one that merely contains the construct", () => {
        const root = preview(
            '<p><code>("fade in")</code> and <code>keep ("fade in") here</code></p>',
            [construct("Command", '("fade in")', 1)],
        );

        expect(root.querySelectorAll(".dd-tok-command")).toHaveLength(1);
        expect(root.querySelector(".dd-tok-command")?.textContent).toBe('("fade in")');
    });

    it("leaves the ignored Markdown, the front matter, and link text plain", () => {
        const root = preview(
            '<pre class="frontmatter"><code>title: #happy</code></pre>' +
                '<div class="dd-preview-ignored-region"><p>#happy</p></div>' +
                '<p><a href="#happy">#happy</a></p>',
            [construct("CustomTag", "#happy")],
        );

        expect(root.querySelectorAll(".dd-tag")).toHaveLength(0);
    });

    it("prefers the longest construct when two share a prefix", () => {
        const root = preview("<p>Narrator ##default: Hi.</p>", [
            construct("CustomTag", "#default"),
            construct("ReservedTag", "##default"),
        ]);

        expect(root.querySelector(".dd-tag")?.textContent).toBe("##default");
        expect(root.querySelectorAll(".dd-tag")).toHaveLength(1);
    });

    it("does not mark part of a longer word or an already marked word", () => {
        const root = preview("<p>a#happy and #happiness and #happy</p>", [
            construct("CustomTag", "#happy"),
        ]);

        expect(root.querySelectorAll(".dd-tag")).toHaveLength(1);
        expect(root.querySelector("p")?.textContent).toBe("a#happy and #happiness and #happy");
    });

    it("marks each construct in a speaker prefix once", () => {
        const root = preview("<p>Guide @guide #wise: Welcome.</p>", [
            construct("SpeakerName", "Guide"),
            construct("SpeakerId", "@guide", 6),
            construct("CustomTag", "#wise", 13),
        ]);

        expect(root.querySelector(".dd-tok-speaker-name")?.textContent).toBe("Guide");
        expect(root.querySelector(".dd-tok-speaker-id")?.textContent).toBe("@guide");
        expect(root.querySelector(".dd-tag")?.textContent).toBe("#wise");
        expect(root.querySelector("p")?.textContent).toBe("Guide @guide #wise: Welcome.");
    });

    it("does nothing on a second pass over the same preview", () => {
        const root = preview("<p>Alice #happy: Hi.</p>", [construct("CustomTag", "#happy", 6)]);

        annotatePreviewConstructs(root, [construct("CustomTag", "#happy", 6)]);

        expect(root.querySelectorAll(".dd-tag")).toHaveLength(1);
    });

    it("ignores the kinds the preview deliberately leaves plain", () => {
        const root = preview("<p>Alice: Hi.</p>", [construct("Separator", ":", 5)]);

        expect(root.querySelector(".dd-tok-separator")).toBeNull();
        expect(root.textContent).toBe("Alice: Hi.");
    });

    it("is a no-op with no constructs to mark", () => {
        const root = preview("<p>Alice: Hi.</p>", []);

        expect(root.innerHTML).toBe("<p>Alice: Hi.</p>");
    });
});
