import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * The report folds things on several surfaces, and each one arrived separately. This test keeps
 * them speaking one visual language: a surface that grows a fold control must take the chevron
 * from the shared module rather than naming a glyph itself, which is how four different
 * renderings of "fold this" appeared in the first place.
 *
 * Stylesheets are exempt: a CSS `::before` cannot call a helper, so the legend group's disclosure
 * names the codepoint directly. The rule that matters is that no *behavior* picks its own glyph.
 *
 * A submenu marker is exempt too. It points at a menu that opens beside it rather than at content
 * that folds away, so it is a different idea that happens to share a shape.
 */
const SHARED_GLYPH_MODULE = "fold-glyph.ts";
const NOT_A_FOLD_CONTROL = ["context-menu.ts"];
const CHEVRON_SPELLINGS = ["chevron-down", "chevron-right", "\\ueab4", "\\ueab6"];

function sourceFiles(): string[] {
    const here = join(import.meta.dirname);
    return readdirSync(here)
        .filter((name) => name.endsWith(".ts"))
        .filter((name) => !name.endsWith(".test.ts"))
        .filter((name) => name !== SHARED_GLYPH_MODULE)
        .filter((name) => !NOT_A_FOLD_CONTROL.includes(name));
}

describe("the report's fold language", () => {
    it("names the fold chevron in one module only", () => {
        const offenders = sourceFiles().filter((name) => {
            const source = readFileSync(join(import.meta.dirname, name), "utf8");
            return CHEVRON_SPELLINGS.some((spelling) => source.includes(spelling));
        });

        expect(offenders).toEqual([]);
    });

    it("keeps the shared module as the one place the chevron is spelled", () => {
        const shared = readFileSync(join(import.meta.dirname, SHARED_GLYPH_MODULE), "utf8");

        for (const spelling of CHEVRON_SPELLINGS) {
            expect(shared).toContain(spelling);
        }
    });
});
