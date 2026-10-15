/**
 * @file Whether the editors draw whitespace: one switch shared by every editor in the report.
 *
 * Shown whitespace draws each space as a faint dot and each tab as an arrow (CodeMirror's
 * `highlightWhitespace`), the way VS Code's "Render Whitespace" does. Hidden is the default; the
 * reader's choice is remembered in `localStorage`, like the theme.
 */

import { Compartment, type Extension } from "@codemirror/state";
import { EditorView, ViewPlugin, highlightWhitespace } from "@codemirror/view";

const STORAGE_KEY = "dd-show-whitespace";

/**
 * One compartment for every editor. A compartment only names a slot, and each editor's state
 * keeps its own content for it, so a single one serves them all.
 */
const whitespace = new Compartment();

/**
 * The editors currently open, so a switch reaches each of them. Held weakly: the Playbook and
 * Config views drop their editors without destroying them, and this list must not keep those
 * alive.
 */
const openEditors = new Set<WeakRef<EditorView>>();

const listeners = new Set<(visible: boolean) => void>();

/** The current setting, read from storage on first use. */
let visible: boolean | undefined;

/** Registers an editor while it is open, so {@link setWhitespaceVisible} can reconfigure it. */
const tracking = ViewPlugin.define((view) => {
    const ref = new WeakRef(view);
    openEditors.add(ref);
    return { destroy: () => openEditors.delete(ref) };
});

const marks = (show: boolean): Extension => (show ? highlightWhitespace() : []);

/** The remembered choice: shown only when the reader chose it; hidden otherwise. */
export function readWhitespacePreference(
    storage: Storage | undefined = localStorageOrUndefined(),
): boolean {
    try {
        return storage?.getItem(STORAGE_KEY) === "true";
    } catch {
        return false;
    }
}

/** Whether the editors currently draw whitespace. */
export function isWhitespaceVisible(): boolean {
    visible ??= readWhitespacePreference();
    return visible;
}

/**
 * The extension an editor installs to follow the switch. It opens in the current setting and
 * changes in place when the setting does, keeping the document, selection, and history.
 */
export function whitespaceVisibility(): Extension {
    return [whitespace.of(marks(isWhitespaceVisible())), tracking];
}

/**
 * Show or hide whitespace in every open editor, remember the choice, and tell the listeners.
 * Storage failures are ignored: the choice still applies to this session.
 */
export function setWhitespaceVisible(
    show: boolean,
    storage: Storage | undefined = localStorageOrUndefined(),
): void {
    visible = show;
    try {
        storage?.setItem(STORAGE_KEY, String(show));
    } catch {
        /* a sandboxed or `file://` report has nowhere to remember it */
    }
    for (const ref of openEditors) {
        const view = ref.deref();
        if (view) view.dispatch({ effects: whitespace.reconfigure(marks(show)) });
        else openEditors.delete(ref);
    }
    for (const listener of listeners) listener(show);
}

/** Call `listener` with the new setting on every change; returns the unsubscribe function. */
export function onWhitespaceVisibilityChange(listener: (visible: boolean) => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

/** `localStorage`, or undefined when it is not available (e.g. a sandboxed `file://`). */
function localStorageOrUndefined(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
