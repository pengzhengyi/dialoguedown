/**
 * The Explorer's own control: a Files button pinned at the leading edge of the tab bar.
 *
 * It is a **glyph alone**, sized and spaced like the Zen and maximize buttons at the row's other
 * end, because the tab bar's width belongs to the stages. A word here would spend that width on
 * a control that is not a stage, and the file glyph already carries the meaning — it is the same
 * mark an editor puts on its own file panel. It never takes the row's underline, though: that
 * mark means "the stage you are on", and the Explorer is not a stage.
 *
 * It is a **disclosure**, not a mode — a button that shows and hides a named region — so it
 * carries `aria-expanded` (set by the panel it drives) and names the region with
 * `aria-controls`. Having no visible word, it leans on the name and tooltip the panel gives it,
 * which is why {@link EXPLORER_PANEL_NAME} is what the panel is registered under.
 *
 * It is pinned rather than placed among the stage tabs on purpose: the stage row scrolls on a
 * narrow window, and a control that scrolls out of reach is worse than no control at all.
 */

/** Feather Icons (MIT) `file-text`, matching the Config tab's gear in family, size, and weight. */
const FILES_ICON =
    '<svg class="tab-icon" viewBox="0 0 24 24" width="14" height="14" fill="none"' +
    ' stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"' +
    ' aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />' +
    '<polyline points="14 2 14 8 20 8" /><line x1="16" y1="13" x2="8" y2="13" />' +
    '<line x1="16" y1="17" x2="8" y2="17" /><polyline points="10 9 9 9 8 9" /></svg>';

/**
 * The name the panel is announced and captioned by.
 *
 * The control shows no word, so this is the only name it has: the panel builds both the tooltip
 * and the accessible name from it, which keeps what a pointer reveals and what a screen reader
 * announces the same phrase rather than two that can drift.
 */
export const EXPLORER_PANEL_NAME = "Files";

/** The region the toggle shows and hides. */
const EXPLORER_REGION_ID = "explorer";

export function createExplorerToggle(toggle: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "tabbar-explorer";
    button.setAttribute("aria-controls", EXPLORER_REGION_ID);
    button.innerHTML = FILES_ICON;
    button.addEventListener("click", toggle);
    return button;
}
