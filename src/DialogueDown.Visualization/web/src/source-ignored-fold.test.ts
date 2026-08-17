import { EditorState } from "@codemirror/state";
import { describe, expect, it } from "vitest";
import {
    ignoredRegionsOf,
    type IgnoredRegion,
    setIgnoredSpans,
    toggleIgnoredRegion,
    setEveryIgnoredRegionFolded,
    foldedIgnoredRegions,
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

/** The offsets of a line's text, so a fixture reads as lines rather than as arithmetic. */
function lineSpan(doc: string, first: number, last = first) {
    const state = EditorState.create({ doc });
    return { start: state.doc.line(first + 1).from, end: state.doc.line(last + 1).to };
}

function inlineSpan(doc: string, needle: string) {
    const start = doc.indexOf(needle);
    return { start, end: start + needle.length };
}

const TABLE = lineSpan(DOC, 2, 4);
const AUTOLINK = inlineSpan(DOC, "<https://example.com>");
const RULE = lineSpan(DOC, 8);

function stateWith(spans = [TABLE, AUTOLINK, RULE]): EditorState {
    const state = EditorState.create({ doc: DOC, extensions: [sourceIgnoredFold()] });
    return state.update({ effects: setIgnoredSpans.of(spans) }).state;
}

function keysOf(regions: readonly IgnoredRegion[]): string[] {
    return regions.map((region) => region.key);
}

describe("ignoredRegionsOf", () => {
    it("tells a block region from one inside a line", () => {
        const regions = ignoredRegionsOf(stateWith());

        expect(regions.map((region) => region.inline)).toEqual([false, true, false]);
    });

    it("counts the lines a block region covers", () => {
        const regions = ignoredRegionsOf(stateWith());

        expect(regions[0].summary).toBe("Ignored · 3 lines");
        expect(regions[2].summary).toBe("Ignored · 1 line");
    });

    it("names a region by its content, so a region that only moves keeps its name", () => {
        const before = ignoredRegionsOf(stateWith());

        // Type a line above everything: every region shifts down, none of them changes.
        const moved = EditorState.create({
            doc: `Bob: a new line.\n${DOC}`,
            extensions: [sourceIgnoredFold()],
        }).update({
            effects: setIgnoredSpans.of([
                lineSpan(`Bob: a new line.\n${DOC}`, 3, 5),
                inlineSpan(`Bob: a new line.\n${DOC}`, "<https://example.com>"),
                lineSpan(`Bob: a new line.\n${DOC}`, 9),
            ]),
        }).state;

        expect(keysOf(ignoredRegionsOf(moved))).toEqual(keysOf(before));
    });

    it("drops a span the document no longer contains", () => {
        const state = stateWith([{ start: 9999, end: 10_005 }]);

        expect(ignoredRegionsOf(state)).toEqual([]);
    });
});

describe("folding one region", () => {
    it("starts with every region open", () => {
        expect(foldedIgnoredRegions(stateWith()).size).toBe(0);
    });

    it("folds and opens the region a reader names", () => {
        const state = stateWith();
        const [table] = ignoredRegionsOf(state);

        const folded = state.update({ effects: toggleIgnoredRegion.of(table.key) }).state;
        expect([...foldedIgnoredRegions(folded)]).toEqual([table.key]);

        const opened = folded.update({ effects: toggleIgnoredRegion.of(table.key) }).state;
        expect(foldedIgnoredRegions(opened).size).toBe(0);
    });

    it("keeps a region's choice while the writer types elsewhere", () => {
        const state = stateWith();
        const [table] = ignoredRegionsOf(state);
        const folded = state.update({ effects: toggleIgnoredRegion.of(table.key) }).state;

        const typed = folded.update({ changes: { from: 0, insert: "x" } }).state;

        expect([...foldedIgnoredRegions(typed)]).toEqual([table.key]);
    });

    it("opens a folded region rather than letting an edit reach its hidden text", () => {
        const state = stateWith();
        const [table] = ignoredRegionsOf(state);
        const folded = state.update({ effects: toggleIgnoredRegion.of(table.key) }).state;

        const edited = folded.update({ changes: { from: TABLE.start + 2, insert: "!" } }).state;

        expect(foldedIgnoredRegions(edited).size).toBe(0);
    });
});

describe("folding every region", () => {
    it("folds them all, and opens them all", () => {
        const state = stateWith();
        const every = keysOf(ignoredRegionsOf(state));

        const shut = state.update({ effects: setEveryIgnoredRegionFolded.of(true) }).state;
        expect([...foldedIgnoredRegions(shut)].sort()).toEqual([...every].sort());

        const open = shut.update({ effects: setEveryIgnoredRegionFolded.of(false) }).state;
        expect(foldedIgnoredRegions(open).size).toBe(0);
    });

    it("discards one region's choice rather than merging with it", () => {
        const state = stateWith();
        const [table] = ignoredRegionsOf(state);
        const mixed = state.update({ effects: toggleIgnoredRegion.of(table.key) }).state;

        const open = mixed.update({ effects: setEveryIgnoredRegionFolded.of(false) }).state;

        expect(foldedIgnoredRegions(open).size).toBe(0);
    });
});
