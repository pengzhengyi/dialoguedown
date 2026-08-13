import { marked, Marked, type Token, type Tokens } from "marked";
import { gfmHeadingId } from "marked-gfm-heading-id";
import type { DisplayNode, Span } from "./model";

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
    parser.use(gfmHeadingId(), {
        extensions: [
            decoratedToken("table", ignoredSource, (token, render) =>
                ignoredRegion(token, render.table(token as Tokens.Table)),
            ),
            decoratedToken("code", ignoredSource, (token, render) =>
                ignoredRegion(token, render.code(token as Tokens.Code)),
            ),
            decoratedToken("hr", ignoredSource, (token, render) =>
                ignoredRegion(token, render.hr(token as Tokens.Hr)),
            ),
            decoratedToken("html", ignoredSource, (token, render) =>
                ignoredRegion(token, render.html(token as Tokens.HTML)),
            ),
            decoratedToken("link", ignoredSource, (token, render) =>
                ignoredRegion(token, render.link(token as Tokens.Link)),
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

function ignoredRegion(token: Token, html: string): string {
    const content = addClassToFirstElement(html, "dd-preview-ignored");
    const inline =
        token.type === "link" || (token.type === "html" && !(token as Tokens.HTML).block);
    const tag = inline ? "span" : "div";
    const inlineClass = inline ? " dd-preview-ignored-region-inline" : "";
    return `<${tag} class="dd-preview-ignored-region${inlineClass}" title="Ignored — not included in dialogue">${content}</${tag}>`;
}

// Marked removes list/blockquote indentation before handing a nested token to a renderer, while
// the compiler's span slices the original source. Ignoring leading indentation per line makes
// those two views of the same construct compare equal without re-implementing Markdown parsing.
function normalizeMarkdown(value: string): string {
    return value
        .trim()
        .split(/\r?\n/)
        .map((line) => line.trimStart())
        .join("\n");
}

function addClassToFirstElement(html: string, className: string): string {
    return html.replace(/^<([a-z][\w:-]*)([^>]*)>/i, (_whole, tag: string, attributes: string) =>
        attributes.includes("class=")
            ? `<${tag}${attributes.replace(/class="([^"]*)"/, `class="$1 ${className}"`)}>`
            : `<${tag} class="${className}"${attributes}>`,
    );
}

// The Source preview is presentation, not another parser: marked has already proved the
// following HTML is a link. Wrap only an escaped `=>` immediately before that rendered link,
// leaving prose arrows, code spans, and the Source editor's underlying characters untouched.
const jumpLigatureHtml = '<span class="jump-ligature">=&gt;</span>';

function decorateJumpIndicators(html: string): string {
    return html.replace(/=&gt;(?=[ \t]*<a\b)/g, jumpLigatureHtml);
}

function renderFrontMatterAnd(source: string, parseBody: (body: string) => string): string {
    const { frontMatter, body } = splitFrontMatter(source);
    const head = frontMatter
        ? `<p class="frontmatter-label">Front matter</p><pre class="frontmatter"><code>${escapeHtml(frontMatter)}</code></pre>`
        : "";
    return head + parseBody(body);
}
