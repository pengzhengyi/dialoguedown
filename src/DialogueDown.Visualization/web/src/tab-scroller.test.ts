import { describe, it, expect, vi, beforeEach } from "vitest";
import { createTabScroller } from "./tab-scroller";

/**
 * jsdom does no layout, so the scroll geometry a scroller reads is stubbed here. `scrollLeft`
 * stays a real writable property so the module's own writes are observable.
 */
function stubGeometry(el: HTMLElement, scrollWidth: number, clientWidth: number): void {
    Object.defineProperty(el, "scrollWidth", { value: scrollWidth, configurable: true });
    Object.defineProperty(el, "clientWidth", { value: clientWidth, configurable: true });
    el.scrollBy = vi.fn(({ left }: ScrollToOptions = {}) => {
        el.scrollLeft = Math.max(
            0,
            Math.min(scrollWidth - clientWidth, el.scrollLeft + (left ?? 0)),
        );
        el.dispatchEvent(new Event("scroll"));
    }) as unknown as HTMLElement["scrollBy"];
}

describe("createTabScroller", () => {
    let nav: HTMLElement;

    beforeEach(() => {
        nav = document.createElement("nav");
        document.body.appendChild(nav);
    });

    it("stays hidden while every tab already fits", () => {
        stubGeometry(nav, 300, 300);

        const scroller = createTabScroller(nav);

        // Arrows would be noise on a wide window where nothing is off-screen.
        expect(scroller.previous.hidden).toBe(true);
        expect(scroller.next.hidden).toBe(true);
    });

    it("appears when the row overflows, with the leading arrow spent at the start", () => {
        stubGeometry(nav, 900, 300);

        const scroller = createTabScroller(nav);

        expect(scroller.previous.hidden).toBe(false);
        expect(scroller.next.hidden).toBe(false);
        // Nothing is hidden to the left yet, so going back is unavailable but still shown —
        // a control that vanishes mid-interaction moves the ones beside it.
        expect(scroller.previous.disabled).toBe(true);
        expect(scroller.next.disabled).toBe(false);
    });

    it("advances by most of a row, keeping a tab of context", () => {
        stubGeometry(nav, 900, 300);
        const scroller = createTabScroller(nav);

        scroller.next.click();

        expect(nav.scrollBy).toHaveBeenCalledWith({ left: 240, behavior: "smooth" });
        expect(nav.scrollLeft).toBe(240);
    });

    it("spends the trailing arrow once the row is scrolled to its end", () => {
        stubGeometry(nav, 900, 300);
        const scroller = createTabScroller(nav);

        nav.scrollLeft = 600;
        nav.dispatchEvent(new Event("scroll"));

        expect(scroller.previous.disabled).toBe(false);
        expect(scroller.next.disabled).toBe(true);
    });

    it("ignores a press on a spent arrow", () => {
        stubGeometry(nav, 900, 300);
        const scroller = createTabScroller(nav);

        // Already at the start: the leading arrow is disabled, so it must not scroll.
        scroller.previous.click();

        expect(nav.scrollBy).not.toHaveBeenCalled();
    });

    it("goes back by the same step", () => {
        stubGeometry(nav, 900, 300);
        const scroller = createTabScroller(nav);
        nav.scrollLeft = 600;
        nav.dispatchEvent(new Event("scroll"));

        scroller.previous.click();

        expect(nav.scrollBy).toHaveBeenCalledWith({ left: -240, behavior: "smooth" });
        expect(nav.scrollLeft).toBe(360);
    });

    it("re-reads the geometry when the row is resized", () => {
        stubGeometry(nav, 300, 300);
        const scroller = createTabScroller(nav);
        expect(scroller.next.hidden).toBe(true);

        // The window narrowed, so tabs that fit a moment ago no longer do.
        stubGeometry(nav, 900, 300);
        scroller.refresh();

        expect(scroller.next.hidden).toBe(false);
    });
});
