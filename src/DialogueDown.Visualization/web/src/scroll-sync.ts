/**
 * Keep the Source tab's editor and its rendered preview scrolled together, VS Code-style.
 *
 * The mapping is block-anchored: top-level Markdown blocks in the editor are paired with
 * the matching rendered elements in the preview, and scrolling interpolates linearly
 * between those anchors. When the block structures differ, headings are matched by their
 * GitHub-style slug instead; with no matches it degrades to a straight proportional map.
 */

import type { EditorView } from "@codemirror/view";
import { syntaxTree } from "@codemirror/language";
import GithubSlugger from "github-slugger";
import { splitFrontMatter } from "./text";

/** Lezer-Markdown names both ATX (`## x`) and Setext (underlined) headings by level. */
const HEADING_NODE = /^(?:ATXHeading|SetextHeading)([1-6])$/;

interface ScrollAnchor {
    key: string;
    top: number;
}

interface AnchorTops {
    from: number[];
    to: number[];
}

/**
 * How long the pane that started a scroll keeps ownership after its last scroll event.
 * It only needs to outlast the echo scroll our own write triggers on the other pane (a
 * frame or two); a short window keeps switching which pane you drive feeling immediate.
 */
const DRIVER_HOLD_MS = 100;

/**
 * Map a scroll offset from one scrollable axis to another through paired anchor offsets.
 *
 * `fromAnchors[i]` (a pixel offset on the driving axis) corresponds to `toAnchors[i]`
 * (a pixel offset on the following axis) — here, the top of the i-th heading in each
 * pane. The result is a piecewise-linear interpolation through the breakpoints `0 → 0`,
 * each `fromAnchors[i] → toAnchors[i]`, and `fromMax → toMax`. Anchors that are not
 * strictly increasing on both axes (or that fall outside `(0, max)`) are dropped, so a
 * stray or duplicated heading cannot invert the map; extra anchors on either side are
 * ignored by pairing on the shorter list.
 */
export function mapScroll(
    from: number,
    fromAnchors: readonly number[],
    toAnchors: readonly number[],
    fromMax: number,
    toMax: number,
): number {
    if (fromMax <= 0 || toMax <= 0) return 0;
    const clamped = Math.max(0, Math.min(fromMax, from));

    // Build a monotonic breakpoint ladder, starting from the shared content top.
    const breaks: Array<{ from: number; to: number }> = [{ from: 0, to: 0 }];
    const pairs = Math.min(fromAnchors.length, toAnchors.length);
    for (let i = 0; i < pairs; i++) {
        const f = fromAnchors[i];
        const t = toAnchors[i];
        const last = breaks[breaks.length - 1];
        if (f > last.from && f < fromMax && t > last.to && t < toMax) {
            breaks.push({ from: f, to: t });
        }
    }
    breaks.push({ from: fromMax, to: toMax });

    for (let i = 0; i < breaks.length - 1; i++) {
        const lo = breaks[i];
        const hi = breaks[i + 1];
        if (clamped <= hi.from) {
            const span = hi.from - lo.from;
            const fraction = span <= 0 ? 0 : (clamped - lo.from) / span;
            return lo.to + fraction * (hi.to - lo.to);
        }
    }
    return toMax;
}

/** Pair keyed anchors in driving-axis order, dropping anything absent from the other pane. */
export function matchAnchorTops(
    fromAnchors: readonly ScrollAnchor[],
    toAnchors: readonly ScrollAnchor[],
): AnchorTops {
    const targetByKey = new Map(toAnchors.map((anchor) => [anchor.key, anchor.top]));
    const from: number[] = [];
    const to: number[] = [];
    for (const anchor of fromAnchors) {
        const target = targetByKey.get(anchor.key);
        if (target !== undefined) {
            from.push(anchor.top);
            to.push(target);
        }
    }
    return { from, to };
}

/** Strip ATX markers from a heading line; a Setext node starts on its plain-text line. */
function headingText(lineText: string): string {
    return lineText
        .replace(/^\s*#{1,6}\s+/, "")
        .replace(/\s+#+\s*$/, "")
        .trim();
}

/** Source offset where the Markdown body begins, after optional YAML front matter. */
function bodyStart(view: EditorView): number {
    const source = view.state.doc.toString();
    return source.length - splitFrontMatter(source).body.length;
}

/** Content-relative tops of real source headings, keyed by their unique preview id. */
function editorHeadingAnchors(view: EditorView): ScrollAnchor[] {
    const anchors: ScrollAnchor[] = [];
    const firstBodyOffset = bodyStart(view);
    const slugger = new GithubSlugger();
    syntaxTree(view.state).iterate({
        enter: (node) => {
            if (!HEADING_NODE.test(node.name) || node.from < firstBodyOffset) return;
            const line = view.state.doc.lineAt(node.from);
            const slug = slugger.slug(headingText(line.text));
            if (slug)
                anchors.push({ key: `heading:${slug}`, top: view.lineBlockAt(node.from).top });
        },
    });
    return anchors;
}

/** Content-relative tops of rendered headings, keyed by their GitHub-style id. */
function previewHeadingAnchors(preview: HTMLElement): ScrollAnchor[] {
    const base = preview.getBoundingClientRect().top - preview.scrollTop;
    return [...preview.querySelectorAll("h1, h2, h3, h4, h5, h6")]
        .filter((heading) => heading.id !== "")
        .map((heading) => ({
            key: `heading:${heading.id}`,
            top: heading.getBoundingClientRect().top - base,
        }));
}

function editorBlockKind(name: string): string | null {
    const heading = HEADING_NODE.exec(name);
    if (heading) return `h${heading[1]}`;
    switch (name) {
        case "Paragraph":
            return "p";
        case "BulletList":
            return "ul";
        case "OrderedList":
            return "ol";
        case "Blockquote":
            return "blockquote";
        case "HorizontalRule":
            return "hr";
        case "FencedCode":
        case "CodeBlock":
            return "pre";
        default:
            return null;
    }
}

/** Direct body blocks from CodeMirror. An unsupported block makes dense pairing unsafe. */
function editorBlockAnchors(view: EditorView): ScrollAnchor[] | null {
    const firstBodyOffset = bodyStart(view);
    const cursor = syntaxTree(view.state).cursor();
    const anchors: ScrollAnchor[] = [];
    if (!cursor.firstChild()) return anchors;
    do {
        if (cursor.from < firstBodyOffset) continue;
        const kind = editorBlockKind(cursor.name);
        if (kind === null) return null;
        anchors.push({
            key: `block:${anchors.length}:${kind}`,
            top: view.lineBlockAt(cursor.from).top,
        });
    } while (cursor.nextSibling());
    return anchors;
}

/** Direct rendered body blocks, excluding the two front-matter display elements. */
function previewBlockAnchors(preview: HTMLElement): ScrollAnchor[] {
    const base = preview.getBoundingClientRect().top - preview.scrollTop;
    return [...preview.children]
        .filter((element) => !element.matches(".frontmatter-label, .frontmatter"))
        .map((element, index) => ({
            key: `block:${index}:${element.tagName.toLowerCase()}`,
            top: element.getBoundingClientRect().top - base,
        }));
}

/** Prefer dense block anchors when the source and rendered block sequences agree exactly. */
function scrollAnchorTops(view: EditorView, preview: HTMLElement): AnchorTops {
    const editorBlocks = editorBlockAnchors(view);
    const previewBlocks = previewBlockAnchors(preview);
    if (
        editorBlocks !== null &&
        editorBlocks.length === previewBlocks.length &&
        editorBlocks.every((anchor, index) => anchor.key === previewBlocks[index].key)
    ) {
        return matchAnchorTops(editorBlocks, previewBlocks);
    }
    return matchAnchorTops(editorHeadingAnchors(view), previewHeadingAnchors(preview));
}

/**
 * Bind the editor and its preview so scrolling either one scrolls the other to the
 * matching block or heading (see {@link mapScroll}). Whichever pane the user scrolls owns the sync
 * for a short window ({@link DRIVER_HOLD_MS}), so the scroll our own write echoes back on
 * the other pane cannot start a feedback loop. Writes are coalesced to one per frame.
 * Returns a disposer that detaches the listeners.
 */
export function initScrollSync(view: EditorView, preview: HTMLElement): () => void {
    const editor = view.scrollDOM;
    let owner: "editor" | "preview" | null = null;
    let releaseTimer = 0;
    let frame = 0;

    const maxScroll = (element: { scrollHeight: number; clientHeight: number }): number =>
        element.scrollHeight - element.clientHeight;

    const follow = (from: HTMLElement, fromTops: number[], to: HTMLElement, toTops: number[]) => {
        to.scrollTo({
            top: mapScroll(from.scrollTop, fromTops, toTops, maxScroll(from), maxScroll(to)),
            // "instant" (not "auto") so the follow never inherits the preview's CSS
            // `scroll-behavior: smooth`; a smooth animation would fire scroll events past the
            // ownership window and let the follower drive back — a feedback loop.
            behavior: "instant",
        });
    };

    const onScroll = (who: "editor" | "preview") => () => {
        if (owner && owner !== who) return; // the other pane owns the sync right now
        owner = who;
        clearTimeout(releaseTimer);
        releaseTimer = window.setTimeout(() => (owner = null), DRIVER_HOLD_MS);
        if (frame) return; // one write per frame is enough
        frame = requestAnimationFrame(() => {
            frame = 0;
            const anchors = scrollAnchorTops(view, preview);
            if (who === "editor") follow(editor, anchors.from, preview, anchors.to);
            else follow(preview, anchors.to, editor, anchors.from);
        });
    };

    const onEditorScroll = onScroll("editor");
    const onPreviewScroll = onScroll("preview");
    editor.addEventListener("scroll", onEditorScroll, { passive: true });
    preview.addEventListener("scroll", onPreviewScroll, { passive: true });

    return () => {
        editor.removeEventListener("scroll", onEditorScroll);
        preview.removeEventListener("scroll", onPreviewScroll);
        clearTimeout(releaseTimer);
        if (frame) cancelAnimationFrame(frame);
    };
}
