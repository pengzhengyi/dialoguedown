// @vitest-environment node

import { describe, expect, it } from "vitest";
import { clipToWidth, ELLIPSIS, type MeasureText } from "./clip-text";

/** A measurer where every character is one unit wide — width is length. */
const monospace: MeasureText = (text) => text.length;

/** A measurer where `W` is wide and `i` is narrow, as a real font's are. */
const proportional: MeasureText = (text) =>
    [...text].reduce((width, glyph) => width + (glyph === "W" ? 3 : glyph === "i" ? 1 : 2), 0);

describe("clipToWidth", () => {
    it("leaves text that already fits alone", () => {
        expect(clipToWidth("Alice: Hi.", 100, monospace)).toBe("Alice: Hi.");
    });

    it("leaves text that exactly fills its budget alone", () => {
        expect(clipToWidth("abcde", 5, monospace)).toBe("abcde");
    });

    it("cuts text that does not fit, and says so", () => {
        const clipped = clipToWidth("abcdefghij", 5, monospace);

        expect(clipped.endsWith(ELLIPSIS)).toBe(true);
        expect(monospace(clipped)).toBeLessThanOrEqual(5);
    });

    it("keeps as much as the budget allows", () => {
        // Budget 5, ellipsis costs 1, so four characters can stay.
        expect(clipToWidth("abcdefghij", 5, monospace)).toBe("abcd" + ELLIPSIS);
    });

    it("measures width rather than counting characters", () => {
        // The same length, clipped differently, because the glyphs are not the same width.
        const wide = clipToWidth("WWWWWWWW", 9, proportional);
        const narrow = clipToWidth("iiiiiiii", 9, proportional);

        expect(wide.length).toBeLessThan(narrow.length);
        expect(proportional(wide)).toBeLessThanOrEqual(9);
        expect(proportional(narrow)).toBeLessThanOrEqual(9);
    });

    it("never returns more than the budget, whatever the text", () => {
        const budget = 12;
        for (const text of ["WWWWWWWWWWWW", "iiiiiiiiiiiiiiiiiiii", "Wi".repeat(30), "abc"]) {
            expect(proportional(clipToWidth(text, budget, proportional))).toBeLessThanOrEqual(
                budget,
            );
        }
    });

    it("gives up rather than lie when not even the mark fits", () => {
        expect(clipToWidth("abcdef", 0, monospace)).toBe("");
    });

    it("does not leave a space stranded before the mark", () => {
        expect(clipToWidth("ab cdef", 4, monospace)).toBe("ab" + ELLIPSIS);
    });

    it("handles empty text", () => {
        expect(clipToWidth("", 10, monospace)).toBe("");
    });
});
