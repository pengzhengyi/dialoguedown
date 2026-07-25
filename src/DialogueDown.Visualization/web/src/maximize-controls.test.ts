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
        installMaximizeControls(header, content, vi.fn());

        const bar = header.querySelector<HTMLButtonElement>(".tabbar-maximize.maximize-button");
        const exit = content.querySelector<HTMLButtonElement>(".maximize-exit.maximize-button");
        expect(bar).not.toBeNull();
        expect(exit).not.toBeNull();
    });

    it("toggles fullscreen from either control", () => {
        const { header, content } = scratch();
        const onToggle = vi.fn();
        installMaximizeControls(header, content, onToggle);

        header.querySelector<HTMLButtonElement>(".tabbar-maximize")!.click();
        content.querySelector<HTMLButtonElement>(".maximize-exit")!.click();
        expect(onToggle).toHaveBeenCalledTimes(2);
    });
});
