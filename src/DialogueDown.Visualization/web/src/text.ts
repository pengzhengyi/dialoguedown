import { Marked, type MarkedExtension, type Token, type Tokens } from "marked";
import { gfmHeadingId } from "marked-gfm-heading-id";
import { createRegionKeys, type RegionKey } from "./region-key";
import { foldGlyphName } from "./fold-glyph";
import type { DisplayNode, Span } from "./model";
import { MERMAID_PLACEHOLDER_ATTRIBUTE, MERMAID_PLACEHOLDER_TOKEN } from "./mermaid-placeholder";

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

const mermaidCodeBlocks: MarkedExtension = {
    renderer: {
        code(token) {
            if (!isMermaidFence(token.lang)) return false;
            return (
                `<div class="mermaid-diagram" ${MERMAID_PLACEHOLDER_ATTRIBUTE}="${MERMAID_PLACEHOLDER_TOKEN}" data-preview-block="pre">` +
                `<pre class="mermaid-source"><code>${escapeHtml(token.text)}</code></pre>` +
                "</div>\n"
            );
        },
    },
};

// A DialogueDown jump target is a bracketed Markdown link. Mark it while the Markdown token still
// distinguishes `[label](target)` from angle/bare autolinks and raw HTML anchors; the rendered
// HTML alone reduces all of them to `<a>`.
const markdownLinks: MarkedExtension = {
    extensions: [
        {
            name: "link",
            renderer(token) {
                const link = token as Tokens.Link;
                if (!link.raw.startsWith("[")) return false;
                return addClassToFirstElement(this.parser.renderer.link(link), "dd-markdown-link");
            },
        },
    ],
};

/** A snippet parser with Mermaid placeholders but no document-level heading ids. */
const fragmentMarked = new Marked();
fragmentMarked.use(mermaidCodeBlocks, markdownLinks);

/**
 * A dedicated marked instance that adds GitHub-style heading ids, so anchor
 * links in the whole-document preview (`[text](#slug)`) resolve to their
 * headings. Kept separate from the fragment instance so node-snippet previews
 * stay id-free and cannot collide with the document's ids.
 */
const documentMarked = new Marked();
documentMarked.use(gfmHeadingId(), mermaidCodeBlocks, markdownLinks);

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
        (body) => fragmentMarked.parse(body, { async: false, breaks: false }) as string,
    );
}

/**
 * Render a node snippet. Once a stage has Dialogue semantics, linked jump syntax is known and
 * may receive the preview-only ligature even when the selected node is a parent Line/Document.
 */
export function renderNodePreview(source: string, label: string, recognizeJumps = false): string {
    const html = renderMarkdown(source);
    if (!recognizeJumps) return html;
    if (label === "Jump indicator") {
        return html.replace(/=&gt;/, jumpLigatureHtml);
    }
    return decorateJumpIndicators(html);
}

/**
 * Like {@link renderMarkdown}, but adds GitHub-style heading ids so in-document
 * anchor links work. Use for the whole-document Source preview.
 */
export interface PreviewSemantics {
    ignored: readonly Span[];
    controlKeywords: readonly Span[];
}

const EMPTY_PREVIEW_SEMANTICS: PreviewSemantics = {
    ignored: [],
    controlKeywords: [],
};

export function renderDocument(
    source: string,
    semantics: PreviewSemantics = EMPTY_PREVIEW_SEMANTICS,
): string {
    return renderFrontMatterAnd(source, (body) =>
        decorateJumpIndicators(
            documentParser(source, semantics).parse(body, {
                async: false,
                breaks: false,
            }) as string,
        ),
    );
}

/**
 * A document parser that marks only the Markdown the compiler says it ignored. The source
 * snippets, not Markdown kinds, drive this: when project configuration changes a kind from
 * Ignore to Keep, its semantic token disappears and the same preview renders at full strength.
 */
function documentParser(source: string, semantics: PreviewSemantics): Marked {
    if (semantics.ignored.length === 0 && semantics.controlKeywords.length === 0) {
        return documentMarked;
    }

    const ignoredSource = new Set(
        semantics.ignored
            .filter((span) => span.end > span.start)
            .map((span) => normalizeMarkdown(source.slice(span.start, span.end))),
    );
    const controlKeywordSource = new Set(
        semantics.controlKeywords
            .filter((span) => span.end > span.start)
            .map((span) => normalizeMarkdown(source.slice(span.start, span.end))),
    );
    const parser = new Marked();
    const regionKey = createRegionKeys();
    parser.use(gfmHeadingId(), mermaidCodeBlocks, markdownLinks, {
        extensions: [
            decoratedToken("table", ignoredSource, (token, render) =>
                ignoredRegion(token, render.table(token as Tokens.Table), regionKey),
            ),
            decoratedToken("code", ignoredSource, (token, render) =>
                ignoredRegion(token, render.code(token as Tokens.Code), regionKey),
            ),
            decoratedToken("hr", ignoredSource, (token, render) =>
                ignoredRegion(token, render.hr(token as Tokens.Hr), regionKey),
            ),
            decoratedToken("html", ignoredSource, (token, render) =>
                ignoredRegion(token, render.html(token as Tokens.HTML), regionKey),
            ),
            decoratedToken("link", ignoredSource, (token, render) =>
                ignoredRegion(token, render.link(token as Tokens.Link), regionKey),
            ),
            decoratedToken("codespan", controlKeywordSource, (token, render) =>
                addClassToFirstElement(
                    render.codespan(token as Tokens.Codespan),
                    "dd-preview-control-keyword",
                ),
            ),
        ],
    });
    return parser;
}

function isMermaidFence(language: string | undefined): boolean {
    return language?.trim().split(/\s+/, 1)[0].toLowerCase() === "mermaid";
}

type DefaultRenderer = Marked["defaults"]["renderer"];

/**
 * Wrap one core Marked renderer, decorating it only when the token's exact source is among the
 * compiler-projected spans for that semantic kind.
 */
function decoratedToken(
    name: string,
    decoratedSource: ReadonlySet<string>,
    decorate: (token: Token, renderer: NonNullable<DefaultRenderer>) => string,
) {
    return {
        name,
        renderer(this: { parser: { renderer: NonNullable<DefaultRenderer> } }, token: Token) {
            return decoratedSource.has(normalizeMarkdown(token.raw))
                ? decorate(token, this.parser.renderer)
                : false;
        },
    };
}

function ignoredRegion(token: Token, html: string, regionKey: RegionKey): string {
    // Ignored HTML must be shown as source, not executed as markup. Markdig reports opening and
    // closing inline tags separately; wrapping their rendered HTML would create unbalanced DOM.
    const content =
        token.type === "html"
            ? `<code class="dd-preview-ignored dd-preview-ignored-source">${escapeHtml(token.raw)}</code>`
            : addClassToFirstElement(html, "dd-preview-ignored");
    const inline =
        token.type === "link" || (token.type === "html" && !(token as Tokens.HTML).block);
    const tag = inline ? "span" : "div";
    const inlineClass = inline ? " dd-preview-ignored-region-inline" : "";
    const kind = ignoredKind(token);
    const lineCount = sourceLineCount(token.raw);
    const summary = `${kind} · ${lineCount} ${lineCount === 1 ? "line" : "lines"}`;
    const source = token.raw.trim();
    const title = inline
        ? `Ignored ${kind.toLowerCase()}: ${source}`
        : "Ignored — not included in dialogue";
    const sourceBlock = token.type === "code" ? ' data-preview-block="pre"' : "";
    // A chevron performs the action and a static mark states what the region is, which is the
    // rule the whole report follows -- inline as well, so a reader never meets the same glyph
    // meaning two different things depending on where it sits.
    //
    // The accessible name and pressed state depend on the current view, so the Preview controller
    // owns them; the renderer only emits the structure.
    const toggle =
        `<button type="button" class="dd-ignored-region-toggle">` +
        `<span class="codicon codicon-${foldGlyphName(true)} dd-ignored-region-toggle-icon" aria-hidden="true"></span></button>` +
        `<span class="dd-ignored-region-status codicon codicon-circle-slash" aria-hidden="true"></span>`;
    return `<${tag} class="dd-preview-ignored-region${inlineClass}" data-ignored-kind="${escapeHtml(kind)}" data-ignored-summary="${escapeHtml(summary)}" data-ignored-key="${escapeHtml(regionKey(`${kind}:${source}`))}" title="${escapeHtml(title)}"${sourceBlock}>${toggle}${content}</${tag}>`;
}

function ignoredKind(token: Token): string {
    switch (token.type) {
        case "table":
            return "Table";
        case "code":
            return "Code block";
        case "hr":
            return "Divider";
        case "html":
            return "Raw HTML";
        case "link":
            return "Autolink";
        default:
            return "Markdown";
    }
}

function sourceLineCount(raw: string): number {
    return raw.replace(/\r?\n$/, "").split(/\r?\n/).length;
}

// Marked removes list/blockquote indentation before handing a nested token to a renderer, while
// the compiler's span slices the original source. Ignoring leading indentation per line makes
// those two views of the same construct compare equal without re-implementing Markdown parsing.
function normalizeMarkdown(value: string): string {
    const lines = value.trim().split(/\r?\n/);
    const continuationDepths = lines
        .slice(1)
        .filter((line) => line.trim().length > 0)
        .map(leadingQuoteDepth);
    const quoteDepth = continuationDepths.length === 0 ? 0 : Math.min(...continuationDepths);
    return lines
        .map((line, index) => stripQuoteDepth(line, index === 0 ? 0 : quoteDepth))
        .join("\n");
}

function leadingQuoteDepth(line: string): number {
    let rest = line.trimStart();
    let depth = 0;
    while (rest.startsWith(">")) {
        depth += 1;
        rest = rest.slice(1);
        if (rest.startsWith(" ")) rest = rest.slice(1);
    }
    return depth;
}

function stripQuoteDepth(line: string, depth: number): string {
    let rest = line.trimStart();
    for (let level = 0; level < depth && rest.startsWith(">"); level += 1) {
        rest = rest.slice(1);
        if (rest.startsWith(" ")) rest = rest.slice(1);
    }
    return rest.trimStart();
}

function addClassToFirstElement(html: string, className: string): string {
    return html.replace(/^<([a-z][\w:-]*)([^>]*)>/i, (_whole, tag: string, attributes: string) =>
        /(?:^|\s)class="/.test(attributes)
            ? `<${tag}${attributes.replace(/(\sclass=")([^"]*)"/, `$1$2 ${className}"`)}>`
            : `<${tag} class="${className}"${attributes}>`,
    );
}

// The Source preview is presentation, not another parser: marked has already proved the
// following HTML is a link. Group only an escaped `=>` immediately before that rendered link,
// leaving prose arrows, code spans, and the Source editor's underlying characters untouched.
// The group is an atomic inline box: it moves to the next line together, while a long anchor may
// still wrap inside it.
const jumpLigatureHtml = '<span class="jump-ligature">=&gt;</span>';

function decorateJumpIndicators(html: string): string {
    return html.replace(
        /=&gt;[ \t]*(<a\b(?=[^>]*\bclass="[^"]*\bdd-markdown-link\b)[^>]*>[\s\S]*?<\/a>)/g,
        `<span class="jump-target">${jumpLigatureHtml}$1</span>`,
    );
}

function renderFrontMatterAnd(source: string, parseBody: (body: string) => string): string {
    const { frontMatter, body } = splitFrontMatter(source);
    const head = frontMatter
        ? `<p class="frontmatter-label">Front matter</p><pre class="frontmatter"><code>${escapeHtml(frontMatter)}</code></pre>`
        : "";
    return head + parseBody(body);
}
