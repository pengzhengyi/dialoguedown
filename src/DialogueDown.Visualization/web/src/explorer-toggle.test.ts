import { describe, it, expect, vi } from "vitest";
import { createExplorerToggle } from "./explorer-toggle";
import { initCollapsiblePanel } from "./collapse-toggle";

describe("createExplorerToggle", () => {
    it("renders a Files button that names the region it shows", () => {
        const button = createExplorerToggle(vi.fn());

        expect(button.tagName).toBe("BUTTON");
        expect(button.type).toBe("button");
        expect(button.classList.contains("tabbar-explorer")).toBe(true);
        expect(button.getAttribute("aria-controls")).toBe("explorer");
        expect(button.querySelector(".codicon-files")).not.toBeNull();
    });

    it("runs the toggle handler on click", () => {
        const onToggle = vi.fn();

        createExplorerToggle(onToggle).click();

        expect(onToggle).toHaveBeenCalledOnce();
    });

    it("says whether the Explorer is showing, once it drives one", () => {
        // The button states the panel's condition rather than only offering an action, which is
        // what lets it read as engaged while the tree is open.
        const container = document.createElement("div");
        const panel = initCollapsiblePanel({
            container,
            collapsedClass: "explorer-collapsed",
            storageKey: "dd-explorer-collapsed",
            name: "explorer",
            storage: undefined,
            createButton: createExplorerToggle,
        });

        expect(panel.button.getAttribute("aria-expanded")).toBe("true");
        expect(panel.button.getAttribute("aria-label")).toBe("Hide explorer");

        panel.button.click();

        expect(panel.button.getAttribute("aria-expanded")).toBe("false");
        expect(panel.button.getAttribute("aria-label")).toBe("Show explorer");
        expect(container.classList.contains("explorer-collapsed")).toBe(true);
    });
});
