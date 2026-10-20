import { describe, it, expect, afterEach } from "vitest";
import { isWhitespaceVisible, setWhitespaceVisible } from "./show-whitespace";
import { createWhitespaceToggle, installWhitespaceShortcut } from "./whitespace-toggle";

const cleanups: Array<() => void> = [];

afterEach(() => {
    for (const cleanup of cleanups.splice(0)) cleanup();
    setWhitespaceVisible(false);
    localStorage.removeItem("dd-show-whitespace");
    document.body.replaceChildren();
});

/** The status-line toggle, mounted and released after the test. */
function toggle(): HTMLButtonElement {
    const { element, destroy } = createWhitespaceToggle();
    cleanups.push(destroy);
    document.body.append(element);
    return element;
}

/** The Alt-W shortcut on `document`, released after the test. */
function shortcut(): void {
    cleanups.push(installWhitespaceShortcut(document));
}

/** Press a key on `target` the way a browser reports it, returning whether a handler took it. */
function press(target: EventTarget, init: KeyboardEventInit): boolean {
    const event = new KeyboardEvent("keydown", { bubbles: true, cancelable: true, ...init });
    target.dispatchEvent(event);
    return event.defaultPrevented;
}

describe("createWhitespaceToggle", () => {
    it("is a labeled toggle button that names its shortcut", () => {
        const button = toggle();

        expect(button.type).toBe("button");
        expect(button.getAttribute("aria-label")).toBe("Show whitespace");
        expect(button.title).toBe("Show whitespace (Alt-W)");
        expect(button.querySelector(".codicon-whitespace")).not.toBeNull();
    });

    it("starts unpressed while whitespace is hidden", () => {
        expect(toggle().getAttribute("aria-pressed")).toBe("false");
    });

    it("starts pressed when whitespace is already shown", () => {
        setWhitespaceVisible(true);

        expect(toggle().getAttribute("aria-pressed")).toBe("true");
    });

    it("shows whitespace when pressed, and hides it when pressed again", () => {
        const button = toggle();

        button.click();
        expect(isWhitespaceVisible()).toBe(true);
        expect(button.getAttribute("aria-pressed")).toBe("true");

        button.click();
        expect(isWhitespaceVisible()).toBe(false);
        expect(button.getAttribute("aria-pressed")).toBe("false");
    });

    it("follows a change made elsewhere, such as the shortcut", () => {
        const button = toggle();

        setWhitespaceVisible(true);

        expect(button.getAttribute("aria-pressed")).toBe("true");
    });

    it("stops following the setting once destroyed", () => {
        const { element, destroy } = createWhitespaceToggle();
        destroy();

        setWhitespaceVisible(true);

        expect(element.getAttribute("aria-pressed")).toBe("false");
    });
});

describe("installWhitespaceShortcut", () => {
    it("toggles whitespace on Alt-W", () => {
        shortcut();

        const taken = press(document.body, { key: "w", code: "KeyW", altKey: true });

        expect(taken).toBe(true);
        expect(isWhitespaceVisible()).toBe(true);
    });

    it("reads the physical key, since macOS reports Option-W as ∑", () => {
        shortcut();

        press(document.body, { key: "∑", code: "KeyW", altKey: true });

        expect(isWhitespaceVisible()).toBe(true);
    });

    it("works from inside an editor, where the reader is typing", () => {
        shortcut();
        const editor = document.createElement("div");
        editor.contentEditable = "true";
        document.body.append(editor);

        press(editor, { key: "w", code: "KeyW", altKey: true });

        expect(isWhitespaceVisible()).toBe(true);
    });

    it("leaves W with any other modifier alone", () => {
        shortcut();

        const taken = [
            press(document.body, { key: "w", code: "KeyW" }),
            press(document.body, { key: "w", code: "KeyW", altKey: true, ctrlKey: true }),
            press(document.body, { key: "w", code: "KeyW", altKey: true, metaKey: true }),
            press(document.body, { key: "W", code: "KeyW", altKey: true, shiftKey: true }),
        ];

        expect(taken).toEqual([false, false, false, false]);
        expect(isWhitespaceVisible()).toBe(false);
    });

    it("yields to a handler that already took the key", () => {
        shortcut();
        document.body.addEventListener("keydown", (event) => event.preventDefault(), {
            once: true,
        });

        press(document.body, { key: "w", code: "KeyW", altKey: true });

        expect(isWhitespaceVisible()).toBe(false);
    });

    it("stops listening once released", () => {
        installWhitespaceShortcut(document)();

        press(document.body, { key: "w", code: "KeyW", altKey: true });

        expect(isWhitespaceVisible()).toBe(false);
    });
});
