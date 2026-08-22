/**
 * Undo history that belongs to **one document**. Replacing an editor's text covers two different
 * intents: reverting the same file (a reload, a discard), where undo should still reach the text
 * before it; and opening a different file, where it must not — undoing into another script's text
 * would leave it in this buffer, and a save would then write it to the wrong file.
 */

import { Compartment } from "@codemirror/state";
import type { Extension } from "@codemirror/state";
import type { EditorView } from "@codemirror/view";
import { history } from "@codemirror/commands";

/**
 * Holds the editor's history extension so it can be rebuilt. Reconfiguring a compartment discards
 * the state its extension was keeping, which is how a history is cleared.
 */
const historyState = new Compartment();

/** The history extension, wrapped so {@link openDocument} can reset it. */
export function documentHistory(): Extension {
    return historyState.of(history());
}

/** Replace the editor's text, keeping the undo history — the same document, different content. */
export function setDocumentContent(view: EditorView, source: string): void {
    view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: source } });
}

/** Replace the editor's text with a **different** document, dropping the previous one's history. */
export function openDocument(view: EditorView, source: string): void {
    view.dispatch({
        changes: { from: 0, to: view.state.doc.length, insert: source },
        selection: { anchor: 0 },
    });
    // Clearing the history means removing the extension and putting it back: `history()` always
    // returns the same state field, so reconfiguring straight to a new one would keep the old
    // entries. Both steps follow the change, which the outgoing history would otherwise record.
    view.dispatch({ effects: historyState.reconfigure([]) });
    view.dispatch({ effects: historyState.reconfigure(history()) });
}
