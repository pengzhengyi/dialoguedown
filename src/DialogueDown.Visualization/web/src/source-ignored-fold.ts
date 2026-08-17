import { EditorState, StateEffect, StateField, type Extension } from "@codemirror/state";
import { Decoration, EditorView, WidgetType, type DecorationSet } from "@codemirror/view";
import { foldEffect, foldService, foldedRanges, unfoldEffect } from "@codemirror/language";
import type { Span } from "./model";

/**
 * The Markdown the compiler ignored, in the Source editor.
 *
 * Folding here is the **editor's own** fold, from the gutter chevron a code editor already offers.
 * Hiding a run of lines is what a writer wants from an editor and what the gutter already means,
 * so an ignored region simply becomes foldable — it does not grow a second control that would give
 * one table two ways to close.
 *
 * What this adds beyond that is a quiet cue: a mark saying *this run never becomes dialogue*,
 * annotation rather than button, in the manner of an editor's inline hints.
 *
 * The Preview folds the same regions from its own state. Neither pane drives the other, because
 * Source is the editable truth: a reading choice must not hide the text a writer needs to change.
 */

/** One run of ignored Markdown, as the editor sees it. */
export interface IgnoredRegion {
    readonly from: number;
    readonly to: number;
    /** A run that occupies its lines entirely, rather than a span inside a line of dialogue. */
    readonly ownsItsLines: boolean;
    /** Whether the editor's gutter can fold this run away. */
    readonly foldable: boolean;
}

/** Replace the compiler-projected ignored spans the editor knows about. */
export const setIgnoredSpans = StateEffect.define<readonly Span[]>();

const ignoredSpansField = StateField.define<readonly Span[]>({
    create: () => [],
    update(spans, transaction) {
        let next = spans;
        if (transaction.docChanged) {
            next = next
                .map((span) => ({
                    start: transaction.changes.mapPos(span.start, 1),
                    end: transaction.changes.mapPos(span.end, -1),
                }))
                .filter((span) => span.end > span.start);
        }
        for (const effect of transaction.effects) {
            if (effect.is(setIgnoredSpans)) next = effect.value;
        }
        return next;
    },
});

/** The ignored regions the editor currently knows, in document order. */
export function ignoredRegionsOf(state: EditorState): readonly IgnoredRegion[] {
    const length = state.doc.length;
    return state
        .field(ignoredSpansField)
        .filter((span) => span.start >= 0 && span.end <= length && span.end > span.start)
        .map((span) => {
            const first = state.doc.lineAt(span.start);
            const last = state.doc.lineAt(span.end);
            const ownsItsLines = span.start === first.from && span.end === last.to;
            return {
                from: span.start,
                to: span.end,
                ownsItsLines,
                // Folding hides lines beneath the one the gutter chevron sits on, so a run of a
                // single line has nothing to hide even though it owns that line.
                foldable: ownsItsLines && last.number > first.number,
            };
        })
        .sort((a, b) => a.from - b.from);
}

/** The fold range an ignored region offers, in the shape the editor's gutter expects. */
function ignoredFoldRange(
    state: EditorState,
    lineStart: number,
): { from: number; to: number } | null {
    const line = state.doc.lineAt(lineStart);
    for (const region of ignoredRegionsOf(state)) {
        if (!region.foldable) continue;
        if (state.doc.lineAt(region.from).number === line.number) {
            return { from: line.to, to: region.to };
        }
    }
    return null;
}

/** A quiet mark saying the run it trails never becomes dialogue. It is annotation, not a control. */
class IgnoredCueWidget extends WidgetType {
    override eq(): boolean {
        return true;
    }

    toDOM(): HTMLElement {
        const cue = document.createElement("span");
        cue.className = "dd-source-ignored-cue codicon codicon-circle-slash";
        cue.title = "Ignored — not included in dialogue";
        cue.setAttribute("aria-hidden", "true");
        return cue;
    }

    override ignoreEvent(): boolean {
        return true;
    }
}

const CUE = Decoration.widget({ widget: new IgnoredCueWidget(), side: 1 });

function cuesFor(state: EditorState): DecorationSet {
    // The cue trails the region's first line, where it cannot shift the text it describes: a mark
    // before a table's first row would push that row out of line with the rows beneath it.
    return Decoration.set(
        ignoredRegionsOf(state)
            .filter((region) => region.ownsItsLines)
            .map((region) => CUE.range(state.doc.lineAt(region.from).to)),
        true,
    );
}

const ignoredCues = StateField.define<DecorationSet>({
    create: (state) => cuesFor(state),
    update: (value, transaction) =>
        transaction.docChanged || transaction.effects.length > 0
            ? cuesFor(transaction.state)
            : value,
    provide: (field) => EditorView.decorations.from(field),
});

/** Whether the document has an ignored region a fold command could act on. */
export function hasFoldableIgnoredRegions(state: EditorState): boolean {
    return ignoredRegionsOf(state).some((region) => region.foldable);
}

/**
 * Fold every ignored region, or open every one, through the editor's own folding — so the result
 * is indistinguishable from having pressed each gutter chevron in turn.
 */
export function foldEveryIgnoredRegion(view: EditorView, folded: boolean): boolean {
    const ranges = ignoredRegionsOf(view.state)
        .filter((region) => region.foldable)
        .map((region) => ({ from: view.state.doc.lineAt(region.from).to, to: region.to }));
    if (ranges.length === 0) return false;

    const already = foldedRanges(view.state);
    const effects = ranges
        .filter((range) => {
            let open = true;
            already.between(range.from, range.to, (from, to) => {
                if (from === range.from && to === range.to) open = false;
            });
            return folded === open;
        })
        .map((range) => (folded ? foldEffect : unfoldEffect).of(range));
    if (effects.length === 0) return false;

    view.dispatch({ effects });
    return true;
}

/**
 * Make the compiler's ignored Markdown foldable from the editor's own gutter, and mark it as
 * content that never becomes dialogue.
 */
export function sourceIgnoredFold(): Extension {
    return [ignoredSpansField, ignoredCues, foldService.of(ignoredFoldRange)];
}
