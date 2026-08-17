import { describe, expect, it } from "vitest";
import {
    FOLD_COMMAND_GLYPHS,
    foldGlyphCharacter,
    foldGlyphName,
    foldControlIcon,
    foldGutterMarker,
} from "./fold-glyph";

describe("foldGlyphName", () => {
    it("points a chevron down over an open item and right over a shut one", () => {
        expect(foldGlyphName(true)).toBe("chevron-down");
        expect(foldGlyphName(false)).toBe("chevron-right");
    });
});

describe("foldGlyphCharacter", () => {
    it("gives the same glyph as a character, for surfaces that draw text rather than HTML", () => {
        expect(foldGlyphCharacter(true)).toBe("\ueab4");
        expect(foldGlyphCharacter(false)).toBe("\ueab6");
    });
});

describe("foldControlIcon", () => {
    it("renders the shared chevron, hidden from assistive technology", () => {
        const icon = foldControlIcon(true, "example-icon");

        expect(icon.classList.contains("codicon-chevron-down")).toBe(true);
        expect(icon.classList.contains("example-icon")).toBe(true);
        expect(icon.getAttribute("aria-hidden")).toBe("true");
    });

    it("switches to the collapsed chevron", () => {
        expect(
            foldControlIcon(false, "example-icon").classList.contains("codicon-chevron-right"),
        ).toBe(true);
    });
});

describe("foldGutterMarker", () => {
    it("gives a CodeMirror gutter the shared chevron and keeps the library's titles", () => {
        const open = foldGutterMarker(true);
        const shut = foldGutterMarker(false);

        expect(open.classList.contains("codicon-chevron-down")).toBe(true);
        expect(open.title).toBe("Fold line");
        expect(shut.classList.contains("codicon-chevron-right")).toBe(true);
        expect(shut.title).toBe("Unfold line");
    });
});

describe("FOLD_COMMAND_GLYPHS", () => {
    it("names the pair every surface uses for its all-commands", () => {
        expect(FOLD_COMMAND_GLYPHS).toEqual({
            expandAll: "expand-all",
            collapseAll: "collapse-all",
        });
    });
});
