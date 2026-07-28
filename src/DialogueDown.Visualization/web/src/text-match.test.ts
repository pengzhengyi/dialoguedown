import { describe, it, expect } from "vitest";
import { findMatches, hasMatch, type MatchOptions } from "./text-match";

const opts = (over: Partial<MatchOptions> = {}): MatchOptions => ({
    caseSensitive: false,
    wholeWord: false,
    ...over,
});

describe("findMatches", () => {
    it("finds a case-insensitive occurrence by default", () => {
        expect(findMatches("The Crossroads", "cro", opts())).toEqual([{ start: 4, end: 7 }]);
    });

    it("finds every non-overlapping occurrence", () => {
        expect(findMatches("banana", "an", opts())).toEqual([
            { start: 1, end: 3 },
            { start: 3, end: 5 },
        ]);
    });

    it("returns nothing for an empty query", () => {
        expect(findMatches("anything", "", opts())).toEqual([]);
    });
});

describe("hasMatch", () => {
    it("respects Match Case", () => {
        expect(hasMatch("The Crossroads", "cro", opts({ caseSensitive: true }))).toBe(false);
        expect(hasMatch("The Crossroads", "Cro", opts({ caseSensitive: true }))).toBe(true);
    });

    it("matches only whole words with Match Whole Word", () => {
        expect(hasMatch("supermarket", "market", opts({ wholeWord: true }))).toBe(false);
        expect(hasMatch("the-market", "market", opts({ wholeWord: true }))).toBe(true); // hyphen bounds
        expect(hasMatch("The Market", "market", opts({ wholeWord: true }))).toBe(true);
    });

    it("combines Match Case and Match Whole Word", () => {
        const both = opts({ caseSensitive: true, wholeWord: true });
        expect(hasMatch("The Market", "Market", both)).toBe(true);
        expect(hasMatch("The Market", "market", both)).toBe(false);
    });
});
