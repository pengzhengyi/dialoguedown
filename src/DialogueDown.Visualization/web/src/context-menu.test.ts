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

describe("openContextMenu — submenu", () => {
    afterEach(() => {
        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    });

    it("renders a submenu parent as a popup item with a chevron, children hidden until opened", () => {
        openContextMenu(contextEvent(), [
            {
                icon: "go-to-file",
                label: "Jump to",
                submenu: [{ icon: "list-tree", label: "Dialogue AST", run: () => {} }],
            },
        ]);

        const parent = document.querySelector<HTMLElement>(".context-menu-item");
        expect(parent?.getAttribute("aria-haspopup")).toBe("menu");
        expect(parent?.querySelector(".context-menu-chevron")).not.toBeNull();
        // The child menu is not in the DOM until the parent is opened.
        expect(document.querySelectorAll(".context-menu")).toHaveLength(1);
    });

    it("opens the child menu on click and lists the submenu items", () => {
        openContextMenu(contextEvent(), [
            {
                icon: "go-to-file",
                label: "Jump to",
                submenu: [
                    { icon: "list-tree", label: "Dialogue AST", run: () => {} },
                    { icon: "list-tree", label: "Semantic Model", run: () => {} },
                ],
            },
        ]);

        document.querySelector<HTMLButtonElement>(".context-menu-item")!.click();

        expect(document.querySelectorAll(".context-menu")).toHaveLength(2);
        const labels = [...document.querySelectorAll(".context-submenu .context-menu-label")].map(
            (e) => e.textContent,
        );
        expect(labels).toEqual(["Dialogue AST", "Semantic Model"]);
    });

    it("opens the child menu on ArrowRight", () => {
        openContextMenu(contextEvent(), [
            {
                icon: "go-to-file",
                label: "Jump to",
                submenu: [{ icon: "list-tree", label: "Dialogue AST", run: () => {} }],
            },
        ]);

        document.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));

        expect(document.querySelector(".context-submenu")).not.toBeNull();
    });

    it("runs a child action and dismisses the whole menu", () => {
        const run = vi.fn();
        openContextMenu(contextEvent(), [
            {
                icon: "go-to-file",
                label: "Jump to",
                submenu: [{ icon: "list-tree", label: "Dialogue AST", run }],
            },
        ]);
        document.querySelector<HTMLButtonElement>(".context-menu-item")!.click();

        document.querySelector<HTMLButtonElement>(".context-submenu .context-menu-item")!.click();

        expect(run).toHaveBeenCalledOnce();
        expect(document.querySelector(".context-menu")).toBeNull();
    });

    it("closes the child on ArrowLeft, keeping the root menu open", () => {
        openContextMenu(contextEvent(), [
            {
                icon: "go-to-file",
                label: "Jump to",
                submenu: [{ icon: "list-tree", label: "Dialogue AST", run: () => {} }],
            },
        ]);
        document.querySelector<HTMLButtonElement>(".context-menu-item")!.click();
        expect(document.querySelector(".context-submenu")).not.toBeNull();

        document.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowLeft" }));

        expect(document.querySelector(".context-submenu")).toBeNull();
        expect(document.querySelector(".context-menu")).not.toBeNull();
    });
});
