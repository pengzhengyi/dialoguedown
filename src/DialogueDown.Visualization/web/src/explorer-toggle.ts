import { codicon } from "./codicon";

/**
 * The Explorer's own control: a Files button pinned at the leading edge of the tab bar.
 *
 * It is a **disclosure**, not a mode — a button that shows and hides a named region — so it
 * carries `aria-expanded` (set by the panel it drives) and names the region with
 * `aria-controls`. The engaged styling keys off that state rather than `aria-pressed`, which
 * would describe a button that stays pushed rather than a region that is open.
 *
 * It is pinned rather than placed among the stage tabs on purpose: the stage row scrolls on a
 * narrow window, and a control that scrolls out of reach is worse than no control at all.
 */
export function createExplorerToggle(toggle: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "tabbar-explorer";
    button.setAttribute("aria-controls", EXPLORER_REGION_ID);
    button.appendChild(codicon("files", "explorer-toggle-icon"));
    button.addEventListener("click", toggle);
    return button;
}

/** The region the toggle shows and hides. */
const EXPLORER_REGION_ID = "explorer";
