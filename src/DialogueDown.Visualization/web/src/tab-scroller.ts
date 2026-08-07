/**
 * Arrow controls for the stage-tab row. The row scrolls horizontally on a narrow window, but
 * a horizontal scroll gesture is not something every pointing device offers — a plain wheel
 * mouse or a trackpad-less desktop has no way to reach an off-screen tab except by tabbing
 * through it. These give that reader an explicit control, the way Material's scrollable tabs
 * do, and stay out of the way entirely when the whole row already fits.
 */

/** How much of the visible row a single press travels, leaving a tab of context behind. */
const STEP_RATIO = 0.8;

/** Slack for sub-pixel scroll positions, which fractional device pixel ratios produce. */
const EPSILON = 1;

export interface TabScroller {
    /** Scrolls the row back toward the first tab. */
    readonly previous: HTMLButtonElement;
    /** Scrolls the row on toward the last tab. */
    readonly next: HTMLButtonElement;
    /** Re-read the row's geometry — call when its size or its tabs change. */
    refresh(): void;
}

function arrow(label: string, path: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "tab-arrow";
    button.setAttribute("aria-label", label);
    button.innerHTML =
        `<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" ` +
        `stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">` +
        `<path d="${path}"/></svg>`;
    return button;
}

/**
 * Build the two arrows for `tabs` and keep them in step with its scroll position. Both are
 * hidden while the row fits, and the arrow at a spent end is disabled rather than removed, so
 * pressing repeatedly toward one end never shifts the other control out from under the cursor.
 */
export function createTabScroller(tabs: HTMLElement): TabScroller {
    const previous = arrow("Show earlier stages", "m15 18-6-6 6-6");
    const next = arrow("Show later stages", "m9 18 6-6-6-6");

    function step(): number {
        return Math.round(tabs.clientWidth * STEP_RATIO);
    }

    function refresh(): void {
        const overflowing = tabs.scrollWidth > tabs.clientWidth + EPSILON;
        previous.hidden = !overflowing;
        next.hidden = !overflowing;
        previous.disabled = tabs.scrollLeft <= EPSILON;
        next.disabled = tabs.scrollLeft >= tabs.scrollWidth - tabs.clientWidth - EPSILON;
    }

    previous.addEventListener("click", () => tabs.scrollBy({ left: -step(), behavior: "smooth" }));
    next.addEventListener("click", () => tabs.scrollBy({ left: step(), behavior: "smooth" }));
    tabs.addEventListener("scroll", refresh, { passive: true });

    // The row's width changes with the window and with the header around it, and its content
    // changes when a report renders a different set of stages. Guarded because jsdom, which
    // the unit tests run in, has no ResizeObserver.
    if (globalThis.ResizeObserver) {
        new ResizeObserver(refresh).observe(tabs);
    }

    refresh();
    return { previous, next, refresh };
}
