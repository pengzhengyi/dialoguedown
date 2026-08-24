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
        expect(resolve(state, "3")).toMatchObject({ line: 3, column: 0, clamped: false });
    });

    it("takes a line and a column", () => {
        expect(resolve(state, "12:5")).toMatchObject({ line: 12, column: 5 });
    });

    it("takes a column alone, keeping the line the cursor is on", () => {
        expect(resolve(state, ":7")).toMatchObject({ line: 10, column: 7 });
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
        expect(resolve(state, "999")).toMatchObject({ line: 20, clamped: true });
        expect(resolve(state, "-999")).toMatchObject({ line: 1, clamped: true });
    });

    it("resolves nothing from an empty or unreadable expression", () => {
        for (const value of ["", "   ", "abc", "12abc", "1.5", "+"]) {
            expect(resolve(state, value)).toBeNull();
        }
    });
});

describe("guidanceFor", () => {
    it("says what Enter will do", () => {
        expect(guidanceFor(state, "3")).toBe("Press Enter to go to line 3.");
        expect(guidanceFor(state, "12:5")).toBe("Press Enter to go to line 12 at column 5.");
    });

    it("says when a line was pulled back inside the document", () => {
        expect(guidanceFor(state, "999")).toContain("nearest line");
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
