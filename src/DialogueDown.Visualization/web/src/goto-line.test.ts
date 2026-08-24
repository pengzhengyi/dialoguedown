import { describe, it, expect } from "vitest";
import { EditorState, EditorSelection } from "@codemirror/state";
import { resolve, guidanceFor } from "./goto-line";

/** A twenty-line document, with the cursor parked on line 10 unless a test says otherwise. */
function docAt(line = 10): EditorState {
    const state = EditorState.create({
        doc: Array.from({ length: 20 }, (_, i) => `line ${i + 1} text`).join("\n"),
    });
    return state.update({ selection: EditorSelection.cursor(state.doc.line(line).from) }).state;
}

const state = docAt();

describe("resolve", () => {
    it("takes a plain line number", () => {
        // A line with no column names its first character, not a zeroth one.
        expect(resolve(state, "3")).toMatchObject({ line: 3, column: 1, clampedLine: false });
    });

    it("takes a line and a column", () => {
        expect(resolve(state, "12:5")).toMatchObject({ line: 12, column: 5 });
    });

    it("takes a column alone, keeping the line the cursor is on", () => {
        expect(resolve(state, ":7")).toMatchObject({ line: 10, column: 7 });
    });

    it("accepts a colon on its own as a column still being typed", () => {
        // `line N text` is 11 characters, so a cursor can rest in columns 1 through 12.
        expect(resolve(state, "3:")).toMatchObject({
            line: 3,
            column: 1,
            lastColumn: 12,
            awaitingColumn: true,
        });
    });

    it("pulls a column past the end of the line back to it, and says it did", () => {
        expect(resolve(state, "3:999")).toMatchObject({ column: 12, clampedColumn: true });
    });

    it("moves relative to the cursor on a sign", () => {
        expect(resolve(state, "+5")).toMatchObject({ line: 15 });
        expect(resolve(state, "-4")).toMatchObject({ line: 6 });
    });

    it("reads a percentage as a position in the document", () => {
        expect(resolve(state, "50%")).toMatchObject({ line: 10 });
        expect(resolve(state, "100%")).toMatchObject({ line: 20 });
    });

    it("reads a signed percentage as a move by that much of the document", () => {
        // The cursor is on line 10 of 20 — half way — so +25% lands three quarters down.
        expect(resolve(state, "+25%")).toMatchObject({ line: 15 });
    });

    it("pulls a line outside the document back to the nearest one, and says it did", () => {
        expect(resolve(state, "999")).toMatchObject({ line: 20, clampedLine: true });
        expect(resolve(state, "-999")).toMatchObject({ line: 1, clampedLine: true });
    });

    it("resolves nothing from an empty or unreadable expression", () => {
        for (const value of ["", "   ", "abc", "12abc", "1.5", "+"]) {
            expect(resolve(state, value)).toBeNull();
        }
    });
});

describe("guidanceFor", () => {
    it("says what Enter will do", () => {
        expect(guidanceFor(state, "3")).toContain("Press Enter to go to line 3");
        expect(guidanceFor(state, "12:5")).toBe("Press Enter to go to line 12 at column 5.");
    });

    it("offers the column once a line is entered, and stops once one is given", () => {
        expect(guidanceFor(state, "3")).toContain("add :5 for a column");
        expect(guidanceFor(state, "3:8")).not.toContain("add :5");
    });

    it("names the column range as soon as the colon is typed", () => {
        expect(guidanceFor(state, "3:")).toBe("Type a column between 1 and 12 on line 3.");
    });

    it("says what an empty line offers rather than a range of one", () => {
        const blank = EditorState.create({ doc: "first\n\nthird" });

        expect(guidanceFor(blank, "2:")).toBe("Line 2 is empty — Enter goes to its start.");
    });

    it("says when a column ran past the end of its line", () => {
        expect(guidanceFor(state, "3:999")).toBe(
            "Press Enter to go to line 3 at column 12, the end of the line.",
        );
    });

    it("says when a line was pulled back inside the document", () => {
        expect(guidanceFor(state, "999")).toContain("nearest in the document");
    });

    it("teaches the expression when nothing is typed yet", () => {
        const message = guidanceFor(state, "");

        // The range a reader can use, and the forms they would never guess.
        expect(message).toContain("between 1 and 20");
        expect(message).toContain("12:5");
        expect(message).toContain("+10");
        expect(message).toContain("50%");
    });
});
