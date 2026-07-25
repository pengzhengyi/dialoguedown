import { describe, it, expect, vi } from "vitest";
import { createZoomControls, ZOOM_STEP, type ZoomHandlers } from "./zoom-controls";

function setup() {
    const handlers: ZoomHandlers = {
        onZoomIn: vi.fn(),
        onZoomOut: vi.fn(),
        onSetZoom: vi.fn(),
        onRevert: vi.fn(),
    };
    const controls = createZoomControls(handlers);
    const buttons = controls.element.querySelectorAll<HTMLButtonElement>("button");
    const input = controls.element.querySelector<HTMLInputElement>(".zoom-input")!;
    return { handlers, controls, buttons, input };
}

describe("createZoomControls", () => {
    it("renders − / editable % / + / revert", () => {
        const { buttons, input } = setup();
        expect(buttons.length).toBe(3);
        expect([...buttons].map((b) => b.textContent)).toEqual(["\u2212", "+", "\u21BA"]);
        expect(input.type).toBe("number");
        expect(input.value).toBe("100");
    });

    it("gives every control an accessible label", () => {
        const { buttons, input } = setup();
        for (const button of buttons) {
            expect(button.getAttribute("aria-label")).toBeTruthy();
        }
        expect(input.getAttribute("aria-label")).toBe("Zoom percent");
    });

    it("wires the step and revert buttons to their handlers", () => {
        const { handlers, buttons } = setup();
        const [minus, plus, revert] = buttons;
        minus.click();
        plus.click();
        revert.click();
        expect(handlers.onZoomOut).toHaveBeenCalledOnce();
        expect(handlers.onZoomIn).toHaveBeenCalledOnce();
        expect(handlers.onRevert).toHaveBeenCalledOnce();
    });

    it("commits a typed percentage on change and on Enter", () => {
        const { handlers, input } = setup();
        input.value = "150";
        input.dispatchEvent(new Event("change"));
        expect(handlers.onSetZoom).toHaveBeenCalledWith(150);

        input.value = "42";
        input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter" }));
        expect(handlers.onSetZoom).toHaveBeenCalledWith(42);
    });

    it("ignores a non-positive or empty percentage", () => {
        const { handlers, input } = setup();
        input.value = "";
        input.dispatchEvent(new Event("change"));
        input.value = "0";
        input.dispatchEvent(new Event("change"));
        expect(handlers.onSetZoom).not.toHaveBeenCalled();
    });

    it("reflects the current scale as a rounded percentage, unless being edited", () => {
        const { controls, input } = setup();
        controls.setRatio(0.85);
        expect(input.value).toBe("85");
        controls.setRatio(1.234);
        expect(input.value).toBe("123");

        // While the reader is typing (input focused), setRatio does not overwrite it.
        document.body.appendChild(controls.element);
        input.focus();
        input.value = "77";
        controls.setRatio(2);
        expect(input.value).toBe("77");
        controls.element.remove();
    });

    it("exposes a sensible zoom step", () => {
        expect(ZOOM_STEP).toBeGreaterThan(1);
    });
});
