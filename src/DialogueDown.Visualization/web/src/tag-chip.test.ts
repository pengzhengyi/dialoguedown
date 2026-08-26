import { describe, it, expect } from "vitest";
import { renderTag, renderTags, tagHue, tagLabel } from "./tag-chip";

describe("tagLabel", () => {
    it("writes a tag as a script writes it", () => {
        expect(tagLabel({ name: "wise", reserved: false })).toBe("#wise");
        expect(tagLabel({ name: "role", value: "guide", reserved: false })).toBe("#role=guide");
        // A reserved name DialogueDown owns takes the double hash.
        expect(tagLabel({ name: "default", reserved: true })).toBe("##default");
    });
});

describe("tagHue", () => {
    it("gives one name the same hue every time", () => {
        // The whole point of hashing the name rather than counting tags: a reader who learns
        // `#wise` by its dot sees the same dot in every table and every tab, across reloads.
        expect(tagHue("wise")).toBe(tagHue("wise"));
    });

    it("keys on the name, so a keyed tag shares its hue with its siblings", () => {
        expect(tagHue("role")).toBe(tagHue("role"));
    });

    it("tells different names apart", () => {
        const hues = new Set(["wise", "role", "mood", "voice", "flag", "tone"].map(tagHue));

        expect(hues.size).toBeGreaterThan(1);
    });
});

describe("renderTag", () => {
    it("carries the text to copy, so a writer can lift it into a script", () => {
        const chip = renderTag({ name: "role", value: "guide", reserved: false });

        expect(chip.dataset.copy).toBe("#role=guide");
        expect(chip.textContent).toBe("#role=guide");
    });

    it("gives a writer's own tag an identity dot and a reserved name none", () => {
        // A reserved name is one of a closed set, so its violet already identifies it; only a
        // custom tag needs the dot to tell it from the next one.
        const custom = renderTag({ name: "wise", reserved: false });
        const reserved = renderTag({ name: "default", reserved: true });

        expect(custom.classList.contains("dd-tag-custom")).toBe(true);
        expect(custom.querySelector(".dd-tag-dot")).not.toBeNull();
        expect(reserved.classList.contains("dd-tag-reserved")).toBe(true);
        expect(reserved.querySelector(".dd-tag-dot")).toBeNull();
    });

    it("paints the dot with the name's hue", () => {
        const chip = renderTag({ name: "wise", reserved: false });
        const dot = chip.querySelector<HTMLElement>(".dd-tag-dot");

        expect(dot?.style.getPropertyValue("--dd-tag-hue")).toBe(tagHue("wise"));
    });
});

describe("renderTags", () => {
    it("draws one capsule per tag", () => {
        const wrap = renderTags([
            { name: "wise", reserved: false },
            { name: "default", reserved: true },
        ]);

        expect(wrap.querySelectorAll(".dd-tag")).toHaveLength(2);
    });

    it("draws nothing when a speaker carries no tags", () => {
        expect(renderTags([]).querySelectorAll(".dd-tag")).toHaveLength(0);
    });
});
