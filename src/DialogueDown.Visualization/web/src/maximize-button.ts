/** @file The button that enters and leaves full screen. */

/**
 * Feather Icons (MIT): `maximize-2` (arrows pointing out) to enter full screen and
 * `minimize-2` (arrows pointing in) to leave it — the same outward/inward zoom arrows
 * readers expect from a media player. Both are rendered into the one button; CSS reveals
 * whichever matches the current state (see the root `.maximized` class in styles.css).
 */
const icon = (variant: string, paths: string): string =>
    `<svg class="maximize-icon ${variant}" viewBox="0 0 24 24" width="14" height="14" ` +
    `fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" ` +
    `stroke-linejoin="round" aria-hidden="true">${paths}</svg>`;

const EXPAND = icon(
    "icon-expand",
    '<polyline points="15 3 21 3 21 9"/><polyline points="9 21 3 21 3 15"/>' +
        '<line x1="21" y1="3" x2="14" y2="10"/><line x1="3" y1="21" x2="10" y2="14"/>',
);

const COMPRESS = icon(
    "icon-compress",
    '<polyline points="4 14 10 14 10 20"/><polyline points="20 10 14 10 14 4"/>' +
        '<line x1="14" y1="10" x2="21" y2="3"/><line x1="3" y1="21" x2="10" y2="14"/>',
);

/**
 * A full-screen toggle button carrying both the enter (expand) and leave (compress)
 * arrow icons. The icons are swapped by CSS from the root `.maximized` class rather than
 * per-button state, so a button built while already maximized (a hot-reloaded graph tab)
 * still shows the correct glyph. Clicking it runs `onToggle`.
 */
export function createMaximizeButton(onToggle: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "maximize-button";
    button.title = "Full screen (f)";
    button.setAttribute("aria-label", "Full screen");
    button.innerHTML = EXPAND + COMPRESS;
    button.addEventListener("click", onToggle);
    return button;
}
