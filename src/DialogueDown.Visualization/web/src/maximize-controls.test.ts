import { describe, it, expect, vi } from "vitest";
import { installMaximizeControls } from "./maximize-controls";

function scratch() {
    const header = document.createElement("header");
    const content = document.createElement("main");
    return { header, content };
}

describe("installMaximizeControls", () => {
    it("adds one tab-bar button and one exit chip, each a maximize button", () => {
        const { header, content } = scratch();
        installMaximizeControls(header, content, vi.fn(), vi.fn());

        const bar = header.querySelector<HTMLButtonElement>(".tabbar-maximize.maximize-button");
        const exit = content.querySelector<HTMLButtonElement>(".maximize-exit.maximize-button");
        expect(bar).not.toBeNull();
        expect(exit).not.toBeNull();
    });

    it("toggles fullscreen from either control", () => {
        const { header, content } = scratch();
        const onToggle = vi.fn();
        installMaximizeControls(header, content, onToggle, vi.fn());

        header.querySelector<HTMLButtonElement>(".tabbar-maximize")!.click();
        content.querySelector<HTMLButtonElement>(".maximize-exit")!.click();
        expect(onToggle).toHaveBeenCalledTimes(2);
    });

    it("adds a Zen button beside the maximize one, wired to its own action", () => {
        const { header, content } = scratch();
        const onToggle = vi.fn();
        const onToggleZen = vi.fn();
        installMaximizeControls(header, content, onToggle, onToggleZen);

        const zen = header.querySelector<HTMLButtonElement>(".tabbar-zen.zen-button")!;
        expect(zen).not.toBeNull();
        expect(zen.getAttribute("aria-label")).toBe("Zen mode");
        expect(zen.title).toContain("z");
        // It carries the concentric-circles glyph VS Code shows beside its own Zen Mode
        // command — not the maximize arrows, and not the Centered Layout glyph.
        expect(zen.querySelector(".codicon-target")).not.toBeNull();
        expect(zen.querySelector(".codicon-layout-centered")).toBeNull();

        zen.click();
        expect(onToggleZen).toHaveBeenCalledTimes(1);
        expect(onToggle).not.toHaveBeenCalled();
    });

    it("returns both tab-bar controls so the empty state can hide them", () => {
        const { header, content } = scratch();
        const controls = installMaximizeControls(header, content, vi.fn(), vi.fn());

        expect(controls.maximize).toBe(header.querySelector(".tabbar-maximize"));
        expect(controls.zen).toBe(header.querySelector(".tabbar-zen"));
    });

    it("orders Zen before maximize, so the pair reads left to right", () => {
        const { header, content } = scratch();
        installMaximizeControls(header, content, vi.fn(), vi.fn());

        const buttons = [...header.querySelectorAll("button")].map((b) => b.className);
        expect(buttons[0]).toContain("tabbar-zen");
        expect(buttons[1]).toContain("tabbar-maximize");
    });
});
