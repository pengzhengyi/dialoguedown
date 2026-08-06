import { describe, it, expect, beforeEach, vi } from "vitest";
import { rememberActiveTab, rememberedActiveTab, revealActiveTab } from "./active-tab";

describe("active tab persistence", () => {
    beforeEach(() => window.sessionStorage.clear());

    it("returns null before any tab is remembered", () => {
        expect(rememberedActiveTab()).toBeNull();
    });

    it("round-trips the remembered tab title", () => {
        rememberActiveTab("Dialogue AST");
        expect(rememberedActiveTab()).toBe("Dialogue AST");
    });

    it("keeps only the most recently remembered tab", () => {
        rememberActiveTab("Source");
        rememberActiveTab("Semantic Model");
        expect(rememberedActiveTab()).toBe("Semantic Model");
    });

    it("never throws and reads back null when the store throws", () => {
        const throwing = {
            getItem: () => {
                throw new Error("blocked");
            },
            setItem: () => {
                throw new Error("blocked");
            },
        } as unknown as Storage;
        expect(() => rememberActiveTab("Source", throwing)).not.toThrow();
        expect(rememberedActiveTab(throwing)).toBeNull();
    });
});

describe("revealActiveTab", () => {
    /** A tab row holding `count` tabs, with `scrollIntoView` stubbed — jsdom has no layout. */
    function tabRow(count: number): { nav: HTMLElement; tabs: HTMLElement[] } {
        const nav = document.createElement("nav");
        const tabs = Array.from({ length: count }, () => {
            const tab = document.createElement("button");
            tab.className = "tab";
            tab.scrollIntoView = vi.fn();
            nav.appendChild(tab);
            return tab;
        });
        return { nav, tabs };
    }

    it("scrolls the active tab into view along the row only", () => {
        const { nav, tabs } = tabRow(3);
        tabs[2].classList.add("active");

        revealActiveTab(nav);

        // `inline` scrolls the horizontal strip; `block: "nearest"` keeps the page itself
        // still, so revealing a tab never scrolls the report out from under the reader.
        expect(tabs[2].scrollIntoView).toHaveBeenCalledWith({
            behavior: "smooth",
            inline: "nearest",
            block: "nearest",
        });
        expect(tabs[0].scrollIntoView).not.toHaveBeenCalled();
    });

    it("does nothing when no tab is active", () => {
        const { nav, tabs } = tabRow(2);

        revealActiveTab(nav);

        expect(tabs[0].scrollIntoView).not.toHaveBeenCalled();
        expect(tabs[1].scrollIntoView).not.toHaveBeenCalled();
    });

    it("survives an environment without scrollIntoView", () => {
        const { nav, tabs } = tabRow(1);
        tabs[0].classList.add("active");
        // jsdom omits it entirely; the report must still render there and in older engines.
        (tabs[0] as Partial<HTMLElement>).scrollIntoView = undefined;

        expect(() => revealActiveTab(nav)).not.toThrow();
    });
});
