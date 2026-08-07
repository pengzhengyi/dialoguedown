import { createMaximizeButton } from "./maximize-button";
import { createZenButton } from "./zen-button";

/** The focus-mode controls installed in the tab row, returned so the caller can hide them. */
export interface FocusControls {
    /** The tab-bar maximize button, hidden when there is nothing to maximize. */
    readonly maximize: HTMLElement;
    /** The tab-bar Zen button, hidden alongside it. */
    readonly zen: HTMLElement;
}

/**
 * Install the app-level focus-mode controls. Both modes are page-level actions (they hide
 * the app chrome so the active tab fills the window), so each gets one control at the right
 * end of the tab row rather than a copy tucked into every tab's toolbar.
 *
 * They are placed in `actions`, a flex sibling of the tab nav rather than an overlay on it,
 * so they stay pinned while the tabs scroll horizontally underneath on a narrow window.
 *
 * A second "exit" chip is placed in `contentRoot` but pinned to the viewport corner and
 * revealed by CSS only while a focus mode is active — the header, and therefore both tab-bar
 * buttons, is hidden then, so the reader still needs a visible way out besides `Escape`,
 * `f`, and `z`. It leaves whichever mode is on.
 *
 * Returns both tab-bar buttons so the caller can hide them when there is nothing to
 * maximize (the empty state has no tabs).
 */
export function installMaximizeControls(
    actions: HTMLElement,
    contentRoot: HTMLElement,
    onToggle: () => void,
    onToggleZen: () => void,
): FocusControls {
    const zen = createZenButton(onToggleZen);
    zen.classList.add("tabbar-zen");
    actions.appendChild(zen);

    const bar = createMaximizeButton(onToggle);
    bar.classList.add("tabbar-maximize");
    actions.appendChild(bar);

    const exit = createMaximizeButton(onToggle);
    exit.classList.add("maximize-exit");
    contentRoot.appendChild(exit);

    return { maximize: bar, zen };
}
