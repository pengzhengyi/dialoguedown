import { marked, Marked } from "marked";
import { gfmHeadingId } from "marked-gfm-heading-id";
import type { DisplayNode } from "./model";

/** Longest inline label/attribute drawn on a node before it is ellipsised. */
export const MAX_INLINE_TEXT = 30;

/** Escape a value for safe insertion into HTML. */
export function escapeHtml(value: string): string {
    return value.replace(
        /[&<>"']/g,
        (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[ch]!,
    );
}

/** Shorten a string to a maximum length with an ellipsis. */
export function ellipsize(text: string, max: number): string {
    return text.length > max ? text.slice(0, max - 1) + "…" : text;
}

/** A node's type name without any parenthetical detail ("Heading (H2)" -> "Heading"). */
export function baseLabel(label: string): string {
    return label.replace(/\s*\(.*\)\s*$/, "");
}

/** HTML for a node's hover tooltip: its label and full (untruncated) attributes. */
export function tooltipHtml(node: DisplayNode): string {
    const parts = [`<strong>${escapeHtml(node.label)}</strong>`];
    for (const attr of node.attributes) {
        parts.push(`<div>${escapeHtml(attr.name)}: ${escapeHtml(attr.value)}</div>`);
    }
    return parts.join("");
}

interface FrontMatterSplit {
    frontMatter: string | null;
    body: string;
}

/**
 * Split a leading YAML front matter block off a source string. marked has no
 * notion of front matter and would render `title:` + `---` as a heading, so we
 * peel it off and show it as metadata instead.
 */
export function splitFrontMatter(source: string): FrontMatterSplit {
    const match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/.exec(source);
    if (match) {
        return { frontMatter: match[1], body: source.slice(match[0].length) };
    }
    return { frontMatter: null, body: source };
}

/**
 * A dedicated marked instance that adds GitHub-style heading ids, so anchor
 * links in the whole-document preview (`[text](#slug)`) resolve to their
 * headings. Kept separate from the default instance so node-snippet previews
 * (fragments) stay id-free and cannot collide with the document's ids.
 */
const documentMarked = new Marked();
documentMarked.use(gfmHeadingId());

/**
 * Render Markdown to HTML, handling a leading YAML front matter block.
 *
 * Uses CommonMark line-break semantics (`breaks: false`) to match VSCode's
 * Markdown preview: a single newline is a soft break that continues the line,
 * while an explicit hard break (two trailing spaces or a trailing backslash)
 * becomes a `<br>`.
 */
export function renderMarkdown(source: string): string {
    return renderFrontMatterAnd(
        source,
        (body) => marked.parse(body, { async: false, breaks: false }) as string,
    );
}

/**
 * Like {@link renderMarkdown}, but adds GitHub-style heading ids so in-document
 * anchor links work. Use for the whole-document Source preview.
 */
export function renderDocument(source: string): string {
    return renderFrontMatterAnd(source, (body) =>
        decorateJumpIndicators(
            documentMarked.parse(body, { async: false, breaks: false }) as string,
        ),
    );
}

// The Source preview is presentation, not another parser: marked has already proved the
// following HTML is a link. Wrap only an escaped `=>` immediately before that rendered link,
// leaving prose arrows, code spans, and the Source editor's underlying characters untouched.
function decorateJumpIndicators(html: string): string {
    return html.replace(/=&gt;(?=\s*<a\b)/g, '<span class="jump-ligature">=&gt;</span>');
}

function renderFrontMatterAnd(source: string, parseBody: (body: string) => string): string {
    const { frontMatter, body } = splitFrontMatter(source);
    const head = frontMatter
        ? `<p class="frontmatter-label">Front matter</p><pre class="frontmatter"><code>${escapeHtml(frontMatter)}</code></pre>`
        : "";
    return head + parseBody(body);
}
