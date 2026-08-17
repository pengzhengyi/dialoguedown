import { describe, it, expect, vi } from "vitest";
import { createExplorerToggle, EXPLORER_PANEL_NAME } from "./explorer-toggle";
import { initCollapsiblePanel } from "./collapse-toggle";

describe("createExplorerToggle", () => {
    it("is a glyph alone, in the row's own icon family", () => {
        const button = createExplorerToggle(vi.fn());

        expect(button.tagName).toBe("BUTTON");
        expect(button.type).toBe("button");
        expect(button.classList.contains("tabbar-explorer")).toBe(true);
        expect(button.getAttribute("aria-controls")).toBe("explorer");
        // The row's own icon family, at the size and weight the Config tab's gear uses.
        expect(button.querySelector("svg.tab-icon")?.getAttribute("stroke-width")).toBe("2");
        // No word: the tab row's width belongs to the stages, and the glyph carries the meaning.
        expect(button.textContent?.trim()).toBe("");
    });

    it("runs the toggle handler on click", () => {
        const onToggle = vi.fn();

        createExplorerToggle(onToggle).click();

        expect(onToggle).toHaveBeenCalledOnce();
    });

    it("says whether the Explorer is showing, and names itself for pointer and screen reader", () => {
        // Showing no word, the tooltip and the accessible name are the only way to learn what
        // this glyph opens — so both are load-bearing, and both come from one constant.
        const container = document.createElement("div");
        const panel = initCollapsiblePanel({
            container,
            collapsedClass: "explorer-collapsed",
            storageKey: "dd-explorer-collapsed",
            name: EXPLORER_PANEL_NAME,
            storage: undefined,
            createButton: createExplorerToggle,
        });

        expect(panel.button.getAttribute("aria-expanded")).toBe("true");
        expect(panel.button.getAttribute("aria-label")).toContain(EXPLORER_PANEL_NAME);
        expect(panel.button.title).toContain(EXPLORER_PANEL_NAME);

        panel.button.click();

        expect(panel.button.getAttribute("aria-expanded")).toBe("false");
        expect(panel.button.getAttribute("aria-label")).toContain(EXPLORER_PANEL_NAME);
        expect(panel.button.title).toContain(EXPLORER_PANEL_NAME);
        expect(container.classList.contains("explorer-collapsed")).toBe(true);
    });
});
