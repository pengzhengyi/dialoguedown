import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { codeFolding, foldable, foldedRanges } from "@codemirror/language";
import { describe, expect, it } from "vitest";
import {
    foldEveryIgnoredRegion,
    hasFoldableIgnoredRegions,
    ignoredRegionsOf,
    setIgnoredSpans,
    sourceIgnoredFold,
} from "./source-ignored-fold";

const DOC = [
    "# Market", // 0
    "", // 1
    "| Item | Cost |", // 2
    "| --- | --- |", // 3
    "| Rope | 5 |", // 4
    "", // 5
    "Alice: see <https://example.com> now.", // 6
    "", // 7
    "---", // 8
    "",
].join("\n");

function lineSpan(first: number, last = first) {
    const state = EditorState.create({ doc: DOC });
    return { start: state.doc.line(first + 1).from, end: state.doc.line(last + 1).to };
}

const TABLE = lineSpan(2, 4);
const AUTOLINK = (() => {
    const start = DOC.indexOf("<https://example.com>");
    return { start, end: start + "<https://example.com>".length };
})();
const RULE = lineSpan(8);

function stateWith(spans = [TABLE, AUTOLINK, RULE]): EditorState {
    const state = EditorState.create({
        doc: DOC,
        extensions: [codeFolding(), sourceIgnoredFold()],
    });
    return state.update({ effects: setIgnoredSpans.of(spans) }).state;
}

function viewWith(spans?: readonly { start: number; end: number }[]): EditorView {
    return new EditorView({ state: stateWith(spans as never) });
}

function foldedCount(state: EditorState): number {
    let count = 0;
    foldedRanges(state).between(0, state.doc.length, () => {
        count += 1;
    });
    return count;
}

describe("ignoredRegionsOf", () => {
    it("can fold a run of whole lines, but not a span inside one", () => {
        expect(ignoredRegionsOf(stateWith()).map((region) => region.foldable)).toEqual([
            true,
            false,
            false,
        ]);
    });

    it("does not offer to fold a single line, which has nothing to hide", () => {
        expect(ignoredRegionsOf(stateWith([RULE]))[0].foldable).toBe(false);
    });

    it("follows its region as the document above it grows", () => {
        const state = stateWith();
        const typed = state.update({ changes: { from: 0, insert: "Bob: hello.\n" } }).state;

        const [table] = ignoredRegionsOf(typed);
        expect(typed.doc.sliceString(table.from, table.to)).toBe(DOC.slice(TABLE.start, TABLE.end));
    });

    it("marks every run that owns its lines, whether or not it can fold", () => {
        expect(ignoredRegionsOf(stateWith()).map((region) => region.ownsItsLines)).toEqual([
            true,
            false,
            true,
        ]);
    });

    it("drops a span the document no longer contains", () => {
        expect(ignoredRegionsOf(stateWith([{ start: 9999, end: 10_005 }]))).toEqual([]);
    });
});

describe("the editor's own gutter", () => {
    it("offers to fold the line an ignored run starts on", () => {
        const state = stateWith();

        expect(foldable(state, state.doc.line(3).from, state.doc.line(3).to)).toEqual({
            from: state.doc.line(3).to,
            to: TABLE.end,
        });
    });

    it("offers nothing on a line no ignored run starts on", () => {
        const state = stateWith();

        expect(foldable(state, state.doc.line(1).from, state.doc.line(1).to)).toBeNull();
    });
});

describe("folding every ignored region", () => {
    it("folds them through the editor's own folding, and opens them again", () => {
        const view = viewWith();

        expect(foldEveryIgnoredRegion(view, true)).toBe(true);
        expect(foldedCount(view.state)).toBe(1);

        expect(foldEveryIgnoredRegion(view, false)).toBe(true);
        expect(foldedCount(view.state)).toBe(0);
        view.destroy();
    });

    it("reports nothing to do when no run can fold", () => {
        const view = viewWith([AUTOLINK, RULE]);

        expect(hasFoldableIgnoredRegions(view.state)).toBe(false);
        expect(foldEveryIgnoredRegion(view, true)).toBe(false);
        view.destroy();
    });

    it("leaves an already folded run alone rather than folding it twice", () => {
        const view = viewWith();
        foldEveryIgnoredRegion(view, true);

        expect(foldEveryIgnoredRegion(view, true)).toBe(false);
        expect(foldedCount(view.state)).toBe(1);
        view.destroy();
    });
});
