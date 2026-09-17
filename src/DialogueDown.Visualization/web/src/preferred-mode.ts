import type { ServedMode } from "./model";

/**
 * The View/Edit mode a reader last chose, remembered for the next script they open.
 *
 * The choice is the reader's, not the document's: a session keeps its own mode while it is open,
 * and the shell that has no document yet asks this for what to offer. It survives a reload of the
 * served report — the live server's port may change between runs, so it is `localStorage` and
 * therefore per-origin, which is exactly the tab's own reach.
 */

const STORAGE_KEY = "dd-served-mode";

/** The remembered mode, or undefined when the reader has never chosen one. */
export function readPreferredMode(
    storage: Storage | undefined = localStorageOrUndefined(),
): ServedMode | undefined {
    try {
        const value = storage?.getItem(STORAGE_KEY);
        return value === "edit" || value === "view" ? value : undefined;
    } catch {
        return undefined;
    }
}

/** Remember a mode. Storage failures are ignored: the choice still applies to this session. */
export function writePreferredMode(
    mode: ServedMode,
    storage: Storage | undefined = localStorageOrUndefined(),
): void {
    try {
        storage?.setItem(STORAGE_KEY, mode);
    } catch {
        /* a sandboxed or `file://` report has nowhere to remember it */
    }
}

/** `localStorage`, or undefined when it is not available (e.g. a sandboxed `file://`). */
function localStorageOrUndefined(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
