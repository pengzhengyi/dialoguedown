import { describe, it, expect, vi, beforeEach } from "vitest";
import { createCollapseToggle, initCollapsiblePanel } from "./collapse-toggle";

/** A throwaway in-memory Storage so persistence is testable without touching the DOM one. */
function memoryStorage(): Storage {
    const map = new Map<string, string>();
    return {
        get length() {
            return map.size;
        },
        clear: () => map.clear(),
        getItem: (k) => map.get(k) ?? null,
        key: (i) => [...map.keys()][i] ?? null,
        removeItem: (k) => map.delete(k),
        setItem: (k, v) => void map.set(k, v),
    };
}

describe("createCollapseToggle", () => {
    it("renders a button carrying both the collapse and expand icons", () => {
        const button = createCollapseToggle(vi.fn());
        expect(button.tagName).toBe("BUTTON");
        expect(button.classList.contains("collapse-toggle")).toBe(true);
        expect(button.querySelector(".icon-collapse")).not.toBeNull();
        expect(button.querySelector(".icon-expand")).not.toBeNull();
    });

    it("runs the toggle handler on click", () => {
        const onToggle = vi.fn();
        createCollapseToggle(onToggle).click();
        expect(onToggle).toHaveBeenCalledOnce();
    });

    it("draws left-panel glyphs for a left-side panel, right-panel glyphs by default", () => {
        // The divider sits at x=9 for a left panel (panel-left) and x=15 for a right one
        // (panel-right), so each side's chevrons point the way the panel moves without a CSS mirror.
        expect(createCollapseToggle(vi.fn(), "left").innerHTML).toContain("M9 3v18");
        expect(createCollapseToggle(vi.fn(), "left").innerHTML).not.toContain("M15 3v18");
        expect(createCollapseToggle(vi.fn()).innerHTML).toContain("M15 3v18");
    });

    it("swallows mousedown so a divider drag never starts from the toggle", () => {
        const button = createCollapseToggle(vi.fn());
        const event = new MouseEvent("mousedown", { bubbles: true, cancelable: true });
        const spy = vi.spyOn(event, "stopPropagation");
        button.dispatchEvent(event);
        expect(spy).toHaveBeenCalled();
    });
});

describe("initCollapsiblePanel", () => {
    let container: HTMLElement;

    beforeEach(() => {
        container = document.createElement("div");
    });

    function setup(storage = memoryStorage()) {
        const panel = initCollapsiblePanel({
            container,
            collapsedClass: "is-collapsed",
            storageKey: "dd-test",
            name: "inspector",
            storage,
        });
        return { panel, storage };
    }

    it("starts expanded and toggles the collapsed class", () => {
        const { panel } = setup();
        expect(panel.isCollapsed()).toBe(false);
        expect(container.classList.contains("is-collapsed")).toBe(false);

        panel.toggle();
        expect(panel.isCollapsed()).toBe(true);
        expect(container.classList.contains("is-collapsed")).toBe(true);

        panel.toggle();
        expect(container.classList.contains("is-collapsed")).toBe(false);
    });

    it("reflects the state on the button (label + aria-expanded)", () => {
        const { panel } = setup();
        expect(panel.button.getAttribute("aria-expanded")).toBe("true");
        expect(panel.button.getAttribute("aria-label")).toBe("Hide inspector");

        panel.toggle();
        expect(panel.button.getAttribute("aria-expanded")).toBe("false");
        expect(panel.button.getAttribute("aria-label")).toBe("Show inspector");
    });

    it("persists the choice either way, so a default can mean hidden", () => {
        // A marker whose absence meant "shown" cannot express "shown on purpose", which a panel
        // that starts hidden needs: the two have to be told apart.
        const { panel, storage } = setup();
        panel.toggle();
        expect(storage.getItem("dd-test")).toBe("1");
        panel.toggle();
        expect(storage.getItem("dd-test")).toBe("0");
    });

    it("starts hidden when asked to and nothing was ever chosen", () => {
        const panel = initCollapsiblePanel({
            container,
            collapsedClass: "is-collapsed",
            storageKey: "dd-test",
            name: "explorer",
            storage: memoryStorage(),
            startCollapsed: true,
        });

        expect(panel.isCollapsed()).toBe(true);
        expect(container.classList.contains("is-collapsed")).toBe(true);
    });

    it("lets a deliberate show outrank a hidden default", () => {
        const storage = memoryStorage();
        storage.setItem("dd-test", "0");

        const panel = initCollapsiblePanel({
            container,
            collapsedClass: "is-collapsed",
            storageKey: "dd-test",
            name: "explorer",
            storage,
            startCollapsed: true,
        });

        expect(panel.isCollapsed()).toBe(false);
    });

    it("honors a choice remembered before the state was written both ways", () => {
        const storage = memoryStorage();
        storage.setItem("dd-test", "1");

        const { panel } = setup(storage);

        expect(panel.isCollapsed()).toBe(true);
    });

    it("drives a control the caller supplies, instead of the divider handle", () => {
        const own = document.createElement("button");
        const panel = initCollapsiblePanel({
            container,
            collapsedClass: "is-collapsed",
            storageKey: "dd-test",
            name: "explorer",
            storage: memoryStorage(),
            createButton: (toggle) => {
                own.addEventListener("click", toggle);
                return own;
            },
        });

        expect(panel.button).toBe(own);
        expect(own.getAttribute("aria-expanded")).toBe("true");

        own.click();

        expect(panel.isCollapsed()).toBe(true);
        expect(own.getAttribute("aria-expanded")).toBe("false");
    });

    it("restores a remembered collapsed state on init", () => {
        const storage = memoryStorage();
        storage.setItem("dd-test", "1");
        const { panel } = setup(storage);
        expect(panel.isCollapsed()).toBe(true);
        expect(container.classList.contains("is-collapsed")).toBe(true);
        expect(panel.button.getAttribute("aria-label")).toBe("Show inspector");
    });

    it("survives a throwing storage without breaking the toggle", () => {
        const throwing: Storage = {
            length: 0,
            clear: () => {},
            getItem: () => {
                throw new Error("blocked");
            },
            key: () => null,
            removeItem: () => {
                throw new Error("blocked");
            },
            setItem: () => {
                throw new Error("blocked");
            },
        };
        const { panel } = setup(throwing);
        expect(panel.isCollapsed()).toBe(false);
        expect(() => panel.toggle()).not.toThrow();
        expect(panel.isCollapsed()).toBe(true);
    });
});
