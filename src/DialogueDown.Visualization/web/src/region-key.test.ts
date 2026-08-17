import { describe, expect, it } from "vitest";
import { createRegionKeys } from "./region-key";

describe("createRegionKeys", () => {
    it("names a region by what it contains, not where it sits", () => {
        const name = createRegionKeys();

        expect(name("| A |\n| - |")).toBe(createRegionKeys()("| A |\n| - |"));
    });

    it("gives different content different names", () => {
        const name = createRegionKeys();

        expect(name("| A |")).not.toBe(name("| B |"));
    });

    it("keeps identical siblings apart by the order they appear", () => {
        const name = createRegionKeys();

        const first = name("---");
        const second = name("---");

        expect(first).not.toBe(second);
        expect(second).toBe(`${first.slice(0, first.lastIndexOf(":"))}:1`);
    });

    it("counts occurrences per content, not across the document", () => {
        const name = createRegionKeys();

        const firstRule = name("---");
        name("| A |");
        const secondRule = name("---");

        expect(secondRule).toBe(`${firstRule.slice(0, firstRule.lastIndexOf(":"))}:1`);
    });

    it("starts counting again for each document", () => {
        expect(createRegionKeys()("---")).toBe(createRegionKeys()("---"));
    });
});
