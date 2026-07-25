import { createMaximizeButton } from "./maximize-button";

/**
 * Install the single, app-level maximize control. The whole-window maximize mode is one
 * page-level action (it hides the app chrome so the active tab fills the window), so it gets
 * one control rather than a copy tucked into each tab's own toolbar.
 *
 * A primary button sits at the right end of the tab-nav row (`header`). A second "exit" chip
 * is placed in `contentRoot` but pinned to the viewport corner and revealed by CSS only while
 * maximized — the header, and therefore the tab-bar button, is hidden then, so the reader
 * still needs a visible way out besides `Escape` / `f`. Both toggle the same mode.
 */
export function installMaximizeControls(
    header: HTMLElement,
    contentRoot: HTMLElement,
    onToggle: () => void,
): void {
    const bar = createMaximizeButton(onToggle);
    bar.classList.add("tabbar-maximize");
    header.appendChild(bar);

    const exit = createMaximizeButton(onToggle);
    exit.classList.add("maximize-exit");
    contentRoot.appendChild(exit);
}
