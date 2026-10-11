/** @file The header's View/Edit switch for a served session. */

import type { ServedMode } from "./model";

/** Feather Icons (MIT): an eye for View, a pencil for Edit. */
const icon = (paths: string): string =>
    `<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor"` +
    ` stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths}</svg>`;

const OPTIONS: ReadonlyArray<[ServedMode, string, string]> = [
    [
        "view",
        "View",
        icon(
            '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>',
        ),
    ],
    [
        "edit",
        "Edit",
        icon('<path d="M12 20h9"/><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z"/>'),
    ],
];

/** The header's View/Edit segmented control. */
export interface ModeToggle {
    /** The control element to mount. */
    readonly element: HTMLElement;
    /** Reflect the active mode (pressed state) on the control. */
    reflect(mode: ServedMode): void;
}

/**
 * A segmented control — `[ View | Edit ]` — that reports a requested mode on click. It
 * is purely presentational: it does not switch anything itself (the mode controller may
 * refuse, e.g. to confirm discarding edits), so the caller reflects the final state.
 */
export function createModeToggle(
    initial: ServedMode,
    onSelect: (mode: ServedMode) => void,
): ModeToggle {
    const group = document.createElement("div");
    group.className = "mode-toggle";
    group.setAttribute("role", "group");
    group.setAttribute("aria-label", "View or edit the document");

    const buttons = new Map<ServedMode, HTMLButtonElement>();
    for (const [mode, label, glyph] of OPTIONS) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "mode-toggle-option";
        button.dataset.mode = mode;
        button.innerHTML = `${glyph}<span>${label}</span>`;
        button.addEventListener("click", () => onSelect(mode));
        buttons.set(mode, button);
        group.append(button);
    }

    const reflect = (mode: ServedMode): void => {
        for (const [value, button] of buttons) {
            button.setAttribute("aria-pressed", String(value === mode));
        }
    };
    reflect(initial);

    return { element: group, reflect };
}
