import { describe, it, expect, vi, afterEach } from "vitest";

import { openContextMenu } from "./context-menu";

function contextEvent(): MouseEvent {
    return new MouseEvent("contextmenu", { clientX: 12, clientY: 12, bubbles: true });
}

describe("openContextMenu", () => {
    afterEach(() => {
        // Dismiss any menu left open so the module's single-menu state does not leak between tests.
        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    });

    it("renders each item with its codicon and label", () => {
        openContextMenu(contextEvent(), [
            { icon: "bold", label: "Bold", run: () => {} },
            { icon: "quote", label: "Quote", run: () => {} },
        ]);

        const items = document.querySelectorAll(".context-menu .context-menu-item");
        expect(items).toHaveLength(2);
        expect(items[0].textContent).toContain("Bold");
        expect(items[0].querySelector(".codicon-bold")).not.toBeNull();
    });

    it("runs an item's action and dismisses on click", () => {
        const run = vi.fn();
        openContextMenu(contextEvent(), [{ icon: "bold", label: "Bold", run }]);

        (document.querySelector(".context-menu-item") as HTMLButtonElement).click();

        expect(run).toHaveBeenCalledOnce();
        expect(document.querySelector(".context-menu")).toBeNull();
    });

    it("dismisses on Escape", () => {
        openContextMenu(contextEvent(), [{ icon: "bold", label: "Bold", run: () => {} }]);

        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));

        expect(document.querySelector(".context-menu")).toBeNull();
    });

    it("dismisses on a click elsewhere", () => {
        openContextMenu(contextEvent(), [{ icon: "bold", label: "Bold", run: () => {} }]);

        document.dispatchEvent(new MouseEvent("pointerdown", { bubbles: true }));

        expect(document.querySelector(".context-menu")).toBeNull();
    });

    it("keeps only one menu open at a time", () => {
        openContextMenu(contextEvent(), [{ icon: "bold", label: "Bold", run: () => {} }]);
        openContextMenu(contextEvent(), [{ icon: "quote", label: "Quote", run: () => {} }]);

        expect(document.querySelectorAll(".context-menu")).toHaveLength(1);
        expect(document.querySelector(".context-menu-item")?.textContent).toContain("Quote");
    });
});
