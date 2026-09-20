import type { Span, TagView, TokenKind } from "./model";
import { TOKEN_CLASS } from "./semantic-tokens";
import { renderTag } from "./tag-chip";

/**
 * The compiler's dialogue constructs, drawn in the rendered Preview the way the Source editor already
 * draws them beside it.
 *
 * Nothing here parses the script. Every mark comes from a token the compiler projected, matched to the
 * exact text it was written as, so the Preview cannot disagree with the compiler about what a
 * construct is. The marks wear the editor's own classes (`dd-tok-*`) except for a tag, which wears the
 * capsule the rest of the report shows it in.
 */

/** The token kinds the Preview marks. The kinds left out are plain on purpose. */
export const PREVIEW_CONSTRUCT_KINDS: readonly TokenKind[] = [
    "SpeakerName",
    "SpeakerId",
    "CustomTag",
    "ReservedTag",
    "JumpIndicator",
    "ReservedAnchor",
    "Query",
    "Condition",
    "StaticWeight",
    "DynamicWeight",
    "Command",
];

/** A construct the compiler projected, positioned in the document but not yet read back out of it. */
export interface PositionedConstruct {
    /** Which construct it is, which decides the mark's shape and what it explains. */
    kind: TokenKind;
    /** Where it is written, for the one mark that can reveal it in the editor. */
    span: Span;
}

/** One construct, resolved against the document being previewed. */
export interface PreviewConstruct extends PositionedConstruct {
    /** The text exactly as the document writes it. */
    text: string;
}

/**
 * What an ask-me mark says when the reader points at it. A tag carries no tip: it copies instead, the
 * way every other tag capsule in the report does. The kinds with no entry are the ones not marked at
 * all — a separator's colon is a colon wherever it appears, the control keyword is already the subject
 * of its own region, and the ignored Markdown is what the Preview deliberately renders plain.
 */
const CONSTRUCT_TIP: Partial<Record<TokenKind, string>> = {
    SpeakerName: "Who speaks this line.",
    SpeakerId: "A speaker id — the stable name for this speaker.",
    Command: "A command — the host is asked to perform this.",
    Query: "A query — a value only the running game can supply.",
    Condition: "A condition — this plays only when the query reads true.",
    StaticWeight: "A weight — this option's share of the draw.",
    DynamicWeight: "A weight — this option's share of the draw.",
    ReservedAnchor: "A reserved target — DialogueDown owns this anchor.",
    JumpIndicator: "A jump — click to reveal it in the source.",
};

/** Elements that are already spoken for: a link is navigable, a fence is not prose, a capsule is a tag. */
const SPOKEN_FOR =
    "a, pre, .dd-tag, [data-construct], .dd-preview-ignored, .dd-preview-ignored-region, .dd-preview-ignored-region-inline";

interface ResolvedMark {
    construct: PreviewConstruct;
    /** The text to find in the preview and to draw the mark with. */
    text: string;
    tag: TagView | null;
    tip: string | null;
}

/**
 * The kinds the compiler tokenizes *inside their backticks*. The token covers the whole code span,
 * while the rendered `<code>` holds only what is between the backticks, so the mark matches the
 * bare text.
 */
const CODE_SPAN_KINDS: readonly TokenKind[] = [
    "Command",
    "Query",
    "Condition",
    "StaticWeight",
    "DynamicWeight",
];

/** The text a mark looks for: the span's content for a code span, the text as written otherwise. */
function markText(construct: PreviewConstruct): string {
    const text = construct.text;
    const backticked = text.startsWith("`") && text.endsWith("`") && text.length > 1;
    return backticked && CODE_SPAN_KINDS.includes(construct.kind) ? text.slice(1, -1) : text;
}

/**
 * Mark every construct occurrence in `root`.
 *
 * A construct the compiler found is matched wherever the document repeats it, because the same text
 * in the same script is the same construct. The exceptions are a speaker's name, which also appears in
 * prose and is marked only where it opens a line's prefix, and a code span, where the mark needs the
 * whole span to be the construct rather than a piece of a longer expression.
 */
export function annotatePreviewConstructs(
    root: HTMLElement,
    constructs: readonly PreviewConstruct[],
): void {
    const marks = resolveMarks(constructs);
    if (marks.length === 0) return;

    for (const node of textNodes(root)) {
        if (isSpokenFor(node)) continue;
        replaceWithMarks(node, marks);
    }
}

/** The marks to draw, longest text first so a tag's `##` form wins over its `#` prefix. */
function resolveMarks(constructs: readonly PreviewConstruct[]): ResolvedMark[] {
    return constructs
        .filter(
            (construct) =>
                construct.text.length > 0 && PREVIEW_CONSTRUCT_KINDS.includes(construct.kind),
        )
        .map((construct) => ({
            construct,
            text: markText(construct),
            tag: isTag(construct.kind) ? tagOf(construct) : null,
            tip: CONSTRUCT_TIP[construct.kind] ?? null,
        }))
        .sort((left, right) => right.text.length - left.text.length);
}

function isTag(kind: TokenKind): boolean {
    return kind === "CustomTag" || kind === "ReservedTag";
}

/** The capsule's data, read back out of the text the compiler tokenized. */
function tagOf(construct: PreviewConstruct): TagView {
    const reserved = construct.kind === "ReservedTag";
    const body = construct.text.slice(reserved ? 2 : 1);
    const equals = body.indexOf("=");
    if (equals < 0) return { name: body, reserved };
    return { name: body.slice(0, equals), value: body.slice(equals + 1), reserved };
}

function textNodes(root: HTMLElement): Text[] {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const nodes: Text[] = [];
    while (walker.nextNode()) nodes.push(walker.currentNode as Text);
    return nodes;
}

function isSpokenFor(node: Text): boolean {
    const element = node.parentElement;
    return element === null || element.closest(SPOKEN_FOR) !== null;
}

/**
 * Replace one text node with the same text, its constructs wrapped in their marks.
 *
 * A code span is not prose: its whole text must be the construct, so `("fade in")` is a command while
 * the same words inside a longer expression are just words.
 */
function replaceWithMarks(node: Text, marks: readonly ResolvedMark[]): void {
    const text = node.nodeValue ?? "";
    const code = node.parentElement?.closest("code") != null;
    const hits: { at: number; mark: ResolvedMark }[] = [];

    for (let at = 0; at < text.length;) {
        const mark = code ? marks.find((mark) => text === mark.text) : markAt(text, at, marks);
        if (mark === undefined) {
            at += 1;
            continue;
        }
        hits.push({ at, mark });
        at += mark.text.length;
    }
    if (hits.length === 0) return;

    const fragment = document.createDocumentFragment();
    let cursor = 0;
    for (const hit of hits) {
        if (hit.at > cursor)
            fragment.appendChild(document.createTextNode(text.slice(cursor, hit.at)));
        fragment.appendChild(markElement(hit.mark));
        cursor = hit.at + hit.mark.text.length;
    }
    if (cursor < text.length) fragment.appendChild(document.createTextNode(text.slice(cursor)));
    node.replaceWith(fragment);
}

function markAt(
    text: string,
    at: number,
    marks: readonly ResolvedMark[],
): ResolvedMark | undefined {
    return marks.find(
        (mark) => text.startsWith(mark.text, at) && readsAsConstruct(text, at, mark),
    );
}

/**
 * Whether the text at `at` reads as the construct rather than as part of a longer word or prose.
 *
 * A tag or a speaker id stands alone: `#happy` is not the `#happy` inside `#happyish`, and one glued
 * to the word before it belongs to that word. A jump indicator is a line's arrow, not an arrow in a
 * sentence. A speaker's name is marked only where it opens a prefix — the occurrence a `:` or an
 * `@id` follows — because the same name appears in prose without being anybody's line.
 */
function readsAsConstruct(text: string, at: number, mark: ResolvedMark): boolean {
    const before = at === 0 ? "" : text[at - 1]!;
    const after = text.slice(at + mark.text.length);

    if (mark.text.startsWith("#") || mark.text.startsWith("@")) {
        return !isWordChar(before) && !/^[\w-]/.test(after);
    }
    if (mark.construct.kind === "JumpIndicator") {
        return (before === "" || /\s/.test(before)) && (after === "" || /^\s/.test(after));
    }
    if (mark.construct.kind === "SpeakerName") {
        return !isWordChar(before) && /^\s*[:@#]/.test(after);
    }
    return !isWordChar(before) && !isWordChar(after[0]);
}

function isWordChar(value: string | undefined): boolean {
    return value !== undefined && /[\w@#-]/.test(value);
}

function markElement(mark: ResolvedMark): HTMLElement {
    const element = mark.tag === null ? constructSpan(mark) : renderTag(mark.tag);
    element.dataset.construct = "";
    if (mark.tip !== null) element.dataset.tip = mark.tip;
    if (mark.construct.kind === "JumpIndicator") {
        element.dataset.span = `${mark.construct.span.start}:${mark.construct.span.end}`;
    }
    return element;
}

function constructSpan(mark: ResolvedMark): HTMLElement {
    const element = document.createElement("span");
    element.className = TOKEN_CLASS[mark.construct.kind];
    element.textContent = mark.text;
    return element;
}
