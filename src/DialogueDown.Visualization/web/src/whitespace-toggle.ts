/**
 * @file The status-line button and the Alt-W shortcut that show or hide whitespace in every
 * editor.
 */

import { codicon } from "./codicon";
import {
    isWhitespaceVisible,
    onWhitespaceVisibilityChange,
    setWhitespaceVisible,
} from "./show-whitespace";

/** The status-line toggle, and how to release its subscription. */
export interface WhitespaceToggle {
    /** The button to mount on the status line. */
    readonly element: HTMLButtonElement;
    /** Stop following the setting. */
    destroy(): void;
}

/**
 * The status-line button: a ¶ glyph that is pressed while whitespace is shown. Its name stays
 * "Show whitespace" and `aria-pressed` carries the state, as a toggle button's should. It follows
 * the setting however it changes, so the shortcut presses it too.
 */
export function createWhitespaceToggle(): WhitespaceToggle {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "whitespace-toggle";
    button.setAttribute("aria-label", "Show whitespace");
    button.title = "Show whitespace (Alt-W)";
    button.append(codicon("whitespace", "whitespace-icon"));

    const reflect = (visible: boolean): void => {
        button.setAttribute("aria-pressed", String(visible));
    };
    reflect(isWhitespaceVisible());
    button.addEventListener("click", () => setWhitespaceVisible(!isWhitespaceVisible()));

    return { element: button, destroy: onWhitespaceVisibilityChange(reflect) };
}

/**
 * Toggle whitespace on Alt-W from anywhere in the report, the editors included, like Save's
 * shortcut. The key is read from `event.code` because macOS reports Option-W as `∑`. Returns the
 * function that removes the shortcut.
 */
export function installWhitespaceShortcut(doc: Document = document): () => void {
    const onKeyDown = (event: KeyboardEvent): void => {
        if (event.defaultPrevented) return;
        if (!event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        if (event.code !== "KeyW") return;
        // Also keeps an editable editor from typing the character Alt-W produces.
        event.preventDefault();
        setWhitespaceVisible(!isWhitespaceVisible());
    };
    doc.addEventListener("keydown", onKeyDown);
    return () => doc.removeEventListener("keydown", onKeyDown);
}
