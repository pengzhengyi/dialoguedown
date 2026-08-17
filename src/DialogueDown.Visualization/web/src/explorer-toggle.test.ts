import { describe, it, expect, vi } from "vitest";
import { createExplorerToggle, EXPLORER_PANEL_NAME } from "./explorer-toggle";
import { initCollapsiblePanel } from "./collapse-toggle";

describe("createExplorerToggle", () => {
    it("wears the tab row's shape: a glyph beside the word it is named for", () => {
        const button = createExplorerToggle(vi.fn());

        expect(button.tagName).toBe("BUTTON");
        expect(button.type).toBe("button");
        expect(button.classList.contains("tabbar-explorer")).toBe(true);
        expect(button.getAttribute("aria-controls")).toBe("explorer");
        // The row's own icon family, at the size and weight the Config tab's gear uses.
        expect(button.querySelector("svg.tab-icon")?.getAttribute("stroke-width")).toBe("2");
        expect(button.querySelector(".tabbar-explorer-label")?.textContent).toBe(
            EXPLORER_PANEL_NAME,
        );
    });

    it("runs the toggle handler on click", () => {
        const onToggle = vi.fn();

        createExplorerToggle(onToggle).click();

        expect(onToggle).toHaveBeenCalledOnce();
    });

    it("says whether the Explorer is showing, and is named by the word it shows", () => {
        // A control whose visible word is missing from its accessible name is a WCAG failure,
        // so the two are built from one constant rather than written twice.
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

        panel.button.click();

        expect(panel.button.getAttribute("aria-expanded")).toBe("false");
        expect(panel.button.getAttribute("aria-label")).toContain(EXPLORER_PANEL_NAME);
        expect(container.classList.contains("explorer-collapsed")).toBe(true);
    });
});
