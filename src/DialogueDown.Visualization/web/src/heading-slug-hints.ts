import { type EditorState, type Extension, StateField } from "@codemirror/state";
import { Decoration, type DecorationSet, EditorView, WidgetType } from "@codemirror/view";
import { syntaxTree } from "@codemirror/language";
import GithubSlugger from "github-slugger";
import { copyToClipboard } from "./path-display";
import { showToast } from "./toast";

/** Lezer-Markdown names both ATX (`## x`) and Setext (underlined) headings by level. */
const HEADING_NODE = /^(?:ATXHeading|SetextHeading)[1-6]$/;

/** Strip the ATX markers from a heading line, leaving its text (a Setext line has none). */
function headingText(lineText: string): string {
    return lineText
        .replace(/^\s*#{1,6}\s+/, "")
        .replace(/\s+#+\s*$/, "")
        .trim();
}

/**
 * The GitHub-style slug of the heading on the active (main-cursor) line, or null when the cursor
 * is not on a heading line or that heading has no sluggable text. Every heading is slugged in
 * document order with one {@link GithubSlugger}, so a duplicate heading suffixes (`-1`, `-2`)
 * exactly as the preview's `gfmHeadingId` does, and a `#` inside a fenced code block is not a
 * heading node so it is skipped. Exported for testing.
 */
export function activeHeadingSlug(state: EditorState): string | null {
    const activeLine = state.doc.lineAt(state.selection.main.head).number;
    const slugger = new GithubSlugger();
    let result: string | null = null;
    syntaxTree(state).iterate({
        enter: (node) => {
            if (!HEADING_NODE.test(node.name)) return;
            const line = state.doc.lineAt(node.from);
            const slug = slugger.slug(headingText(line.text));
            if (line.number === activeLine) result = slug || null;
        },
    });
    return result;
}

/** The inline `#slug` chip rendered after the active heading line; clicking copies `#slug`. */
class SlugHintWidget extends WidgetType {
    constructor(private readonly slug: string) {
        super();
    }

    eq(other: SlugHintWidget): boolean {
        return other.slug === this.slug;
    }

    toDOM(): HTMLElement {
        const anchor = `#${this.slug}`;
        // A zero-size inline wrapper holds the chip as an absolutely-positioned overlay, so the
        // hint adds no line height and no wrap width. That matters because the editor/preview
        // scroll-sync aligns on heading line tops — a layout-affecting widget on the active
        // heading line would shift every heading below it and desync the panes.
        const wrap = document.createElement("span");
        wrap.className = "dd-slug-hint-wrap";
        const button = document.createElement("button");
        button.type = "button";
        button.className = "dd-slug-hint";
        button.textContent = anchor;
        button.title = `Copy ${anchor}`;
        button.setAttribute("aria-label", `Copy anchor ${anchor}`);
        // A press on the chip must not move the caret off the heading line (which would hide it).
        button.addEventListener("mousedown", (event) => event.preventDefault());
        button.addEventListener("click", (event) => {
            event.preventDefault();
            void copyToClipboard(anchor).then(() => showToast(`Copied ${anchor}`));
        });
        wrap.appendChild(button);
        return wrap;
    }

    ignoreEvent(): boolean {
        return true;
    }
}

/** One widget at the end of the active heading line, or nothing when the cursor is elsewhere. */
function buildHint(state: EditorState): DecorationSet {
    const slug = activeHeadingSlug(state);
    if (slug === null) return Decoration.none;
    const line = state.doc.lineAt(state.selection.main.head);
    return Decoration.set([
        Decoration.widget({ widget: new SlugHintWidget(slug), side: 1 }).range(line.to),
    ]);
}

/**
 * The active-line slug-hint field: it rebuilds on document *and* selection changes so the chip
 * follows the caret onto and off heading lines. It holds no source of its own — slugs are derived
 * from the buffer with `github-slugger`, the same algorithm the preview and compiler use.
 */
const slugHintField = StateField.define<DecorationSet>({
    create: (state) => buildHint(state),
    update(decorations, transaction) {
        if (transaction.docChanged || transaction.selection) return buildHint(transaction.state);
        return decorations;
    },
    provide: (field) => EditorView.decorations.from(field),
});

/** The editor extension: reveal a copyable `#slug` chip on the active heading line. */
export function headingSlugHints(): Extension {
    return slugHintField;
}
