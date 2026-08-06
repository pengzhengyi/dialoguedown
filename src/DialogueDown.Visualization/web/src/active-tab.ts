/**
 * Remembering the last-open report tab so a refresh returns to it instead of resetting to the
 * Source tab. Kept in `sessionStorage` (per browser tab, cleared when the tab closes) — the
 * same store the config-create reload flag uses — so two reports open side by side each keep
 * their own tab, and a blocked store simply falls back to the default tab.
 */
const ACTIVE_TAB_KEY = "dd-active-tab";

/** `sessionStorage`, or `undefined` when it is unavailable (e.g. a sandboxed context). */
function defaultStorage(): Storage | undefined {
    try {
        return window.sessionStorage;
    } catch {
        return undefined;
    }
}

/** Remember `title` as the last-open tab. A blocked store is not fatal — the next load defaults. */
export function rememberActiveTab(title: string, storage = defaultStorage()): void {
    try {
        storage?.setItem(ACTIVE_TAB_KEY, title);
    } catch {
        // sessionStorage unavailable (private mode / sandboxed file://) — default tab on reload.
    }
}

/** The last-open tab's title, or `null` when none was remembered or the store is unavailable. */
export function rememberedActiveTab(storage = defaultStorage()): string | null {
    try {
        return storage?.getItem(ACTIVE_TAB_KEY) ?? null;
    } catch {
        return null;
    }
}

/**
 * Scroll the active tab into view along the tab row. The row is a horizontal scroller on a
 * narrow window, so the tab activated by a keyboard shortcut or restored from the last
 * session can sit outside the visible span.
 *
 * Only the row scrolls: `block: "nearest"` leaves the page where it is, so revealing a tab
 * never pulls the report out from under the reader. `scrollIntoView` is absent in jsdom and
 * in older engines, so the call is guarded rather than assumed.
 */
export function revealActiveTab(tabs: HTMLElement): void {
    const active = tabs.querySelector<HTMLElement>(".tab.active");
    active?.scrollIntoView?.({ behavior: "smooth", inline: "nearest", block: "nearest" });
}
