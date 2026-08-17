import {
    EditorState,
    StateEffect,
    StateField,
    RangeSet,
    type Extension,
    type Range,
    type Transaction,
} from "@codemirror/state";
import { Decoration, EditorView, WidgetType, type DecorationSet } from "@codemirror/view";
import { foldGlyphName } from "./fold-glyph";
import { createRegionKeys } from "./region-key";
import type { Span } from "./model";

/**
 * Folding the Markdown the compiler ignored, in the Source editor.
 *
 * The editor already folds *line ranges* — a heading's section, a table, a fenced block — which is
 * an editing convenience and stays. This adds a second unit alongside it: the **ignored region**,
 * the same thing the Preview folds, so a writer can put aside the content that never becomes
 * dialogue while still editing the text that does.
 *
 * The two units are told apart by where their control sits. A line range folds from the gutter,
 * which is CodeMirror's own affordance for a range of lines; an ignored region folds from a
 * control on the region itself.
 *
 * Source keeps its **own** fold state. The Preview may hide the same region without this editor
 * following, because Source is the editable truth: a reading choice must never hide the text a
 * writer needs to change.
 */

/** One region of ignored Markdown, as the editor draws it. */
export interface IgnoredRegion {
    /** Content-derived name, so a choice follows the region rather than the position. */
    readonly key: string;
    readonly from: number;
    readonly to: number;
    /** A span inside a line cannot fold to a summary row; it collapses to a chip in place. */
    readonly inline: boolean;
    /** What a folded block says in place of its content. */
    readonly summary: string;
}

/** Replace the compiler-projected ignored spans the editor knows about. */
export const setIgnoredSpans = StateEffect.define<readonly Span[]>();

/** Fold the named region if it is open, open it if it is folded. */
export const toggleIgnoredRegion = StateEffect.define<string>();

/**
 * Fold every ignored region, or open every one. A command over the whole editor, so it replaces
 * the folded set outright rather than merging with the choices already made.
 */
export const setEveryIgnoredRegionFolded = StateEffect.define<boolean>();

interface IgnoredFoldState {
    readonly spans: readonly Span[];
    readonly folded: ReadonlySet<string>;
}

const ignoredFoldState = StateField.define<IgnoredFoldState>({
    create: () => ({ spans: [], folded: new Set() }),
    update(value, transaction) {
        let { spans, folded } = value;

        if (transaction.docChanged) {
            spans = spans
                .map((span) => ({
                    start: transaction.changes.mapPos(span.start, 1),
                    end: transaction.changes.mapPos(span.end, -1),
                }))
                .filter((span) => span.end > span.start);
            folded = openRegionsAnEditReached(value, transaction);
        }

        for (const effect of transaction.effects) {
            if (effect.is(setIgnoredSpans)) spans = effect.value;
            else if (effect.is(setEveryIgnoredRegionFolded)) {
                folded = effect.value
                    ? new Set(regionsIn(transaction.state, spans).map((region) => region.key))
                    : new Set();
            } else if (effect.is(toggleIgnoredRegion)) {
                const next = new Set(folded);
                if (!next.delete(effect.value)) next.add(effect.value);
                folded = next;
            }
        }

        return spans === value.spans && folded === value.folded ? value : { spans, folded };
    },
});

/**
 * A folded region's text is hidden, so an edit that reaches it would change words the writer
 * cannot see. Opening the region instead keeps the editor honest: the change lands, and it lands
 * somewhere visible.
 */
function openRegionsAnEditReached(
    value: IgnoredFoldState,
    transaction: Transaction,
): ReadonlySet<string> {
    if (value.folded.size === 0) return value.folded;

    // The regions are read from the document as it was, because the change's offsets describe
    // that document rather than the one the edit produced.
    const before = regionsIn(transaction.startState, value.spans).filter((region) =>
        value.folded.has(region.key),
    );
    const touched = new Set<string>();
    transaction.changes.iterChangedRanges((fromA, toA) => {
        for (const region of before) {
            if (fromA <= region.to && toA >= region.from) touched.add(region.key);
        }
    });
    if (touched.size === 0) return value.folded;

    const next = new Set(value.folded);
    for (const key of touched) next.delete(key);
    return next;
}

/** The ignored regions the editor currently knows, in document order. */
export function ignoredRegionsOf(state: EditorState): readonly IgnoredRegion[] {
    return regionsIn(state, state.field(ignoredFoldState).spans);
}

/** The names of the regions the reader has folded. */
export function foldedIgnoredRegions(state: EditorState): ReadonlySet<string> {
    return state.field(ignoredFoldState).folded;
}

function regionsIn(state: EditorState, spans: readonly Span[]): IgnoredRegion[] {
    const length = state.doc.length;
    const name = createRegionKeys();
    return spans
        .filter((span) => span.start >= 0 && span.end <= length && span.end > span.start)
        .sort((a, b) => a.start - b.start)
        .map((span) => {
            const first = state.doc.lineAt(span.start);
            const last = state.doc.lineAt(span.end);
            const inline = span.start > first.from || span.end < last.to;
            const lines = last.number - first.number + 1;
            return {
                key: name(state.doc.sliceString(span.start, span.end)),
                from: span.start,
                to: span.end,
                inline,
                summary: `Ignored · ${lines} ${lines === 1 ? "line" : "lines"}`,
            };
        });
}

/** The control that folds one region, and the placeholder a folded region leaves behind. */
class IgnoredRegionWidget extends WidgetType {
    constructor(
        private readonly region: IgnoredRegion,
        private readonly folded: boolean,
    ) {
        super();
    }

    override eq(other: IgnoredRegionWidget): boolean {
        return other.region.key === this.region.key && other.folded === this.folded;
    }

    toDOM(view: EditorView): HTMLElement {
        const host = document.createElement("span");
        host.className = this.folded
            ? "dd-source-ignored dd-source-ignored-folded"
            : "dd-source-ignored";
        host.dataset.ignoredKey = this.region.key;

        const control = document.createElement("button");
        control.type = "button";
        control.className = "dd-source-ignored-toggle";
        const label = `${this.folded ? "Show" : "Hide"} ignored Markdown`;
        control.setAttribute("aria-expanded", String(!this.folded));
        control.setAttribute("aria-label", label);
        control.title = label;
        const glyph = document.createElement("span");
        glyph.className = `codicon codicon-${foldGlyphName(!this.folded)}`;
        glyph.setAttribute("aria-hidden", "true");
        control.append(glyph);
        control.addEventListener("mousedown", (event) => event.preventDefault());
        control.addEventListener("click", () => {
            view.dispatch({ effects: toggleIgnoredRegion.of(this.region.key) });
        });
        host.append(control);

        if (this.folded && !this.region.inline) {
            const summary = document.createElement("span");
            summary.className = "dd-source-ignored-summary";
            summary.textContent = this.region.summary;
            host.append(summary);
        }

        const status = document.createElement("span");
        status.className = "dd-source-ignored-status codicon codicon-circle-slash";
        status.setAttribute("aria-hidden", "true");
        host.append(status);
        return host;
    }

    override ignoreEvent(): boolean {
        return false;
    }
}

function decorationsFor(state: EditorState): DecorationSet {
    const folded = foldedIgnoredRegions(state);
    const ranges: Range<Decoration>[] = [];
    for (const region of ignoredRegionsOf(state)) {
        const shut = folded.has(region.key);
        if (shut) {
            ranges.push(
                Decoration.replace({
                    widget: new IgnoredRegionWidget(region, true),
                    block: !region.inline,
                }).range(region.from, region.to),
            );
            continue;
        }
        // Open: the control trails the region's first line, where it cannot shift the text it
        // belongs to. Putting it before the region would push a table's first row out of line
        // with the rows beneath it.
        const anchor = state.doc.lineAt(region.from).to;
        ranges.push(
            Decoration.widget({
                widget: new IgnoredRegionWidget(region, false),
                side: 1,
            }).range(anchor),
        );
    }
    return RangeSet.of(ranges, true);
}

const ignoredDecorations = StateField.define<DecorationSet>({
    create: (state) => decorationsFor(state),
    update: (value, transaction) =>
        transaction.docChanged || transaction.effects.length > 0
            ? decorationsFor(transaction.state)
            : value,
    provide: (field) => EditorView.decorations.from(field),
});

/** Whether the document has any ignored region for a command to act on. */
export function hasIgnoredRegions(state: EditorState): boolean {
    return state.field(ignoredFoldState).spans.length > 0;
}

/**
 * Fold every ignored region, or open every one — a command over the whole editor, so it discards
 * the choices made region by region rather than merging with them.
 */
export function foldEveryIgnoredRegion(view: EditorView, folded: boolean): boolean {
    if (!hasIgnoredRegions(view.state)) return false;
    view.dispatch({ effects: setEveryIgnoredRegionFolded.of(folded) });
    return true;
}

/**
 * Fold the compiler's ignored Markdown in the Source editor, alongside the editor's own
 * line-range folding.
 */
export function sourceIgnoredFold(): Extension {
    return [
        ignoredFoldState,
        ignoredDecorations,
        // A folded region is one object as far as the cursor is concerned, so arrow keys step over
        // it rather than into text the reader cannot see.
        EditorView.atomicRanges.of((view) => view.state.field(ignoredDecorations)),
    ];
}
