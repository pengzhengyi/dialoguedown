/** @file Tells which elements take typed input, so global shortcuts leave them alone. */

/**
 * Selector for surfaces that own their keyboard input: form fields and the CodeMirror
 * editor (a `contenteditable` `.cm-editor`). Global shortcuts and graph-navigation
 * handlers must yield to them so typing, cursor movement, and selection stay with the
 * reader instead of being hijacked.
 */
export const TEXT_ENTRY_SELECTOR = "input, textarea, select, [contenteditable], .cm-editor";

/** Whether a key event's target sits inside a {@link TEXT_ENTRY_SELECTOR} surface. */
export function isTextEntryTarget(target: EventTarget | null): boolean {
    return target instanceof Element && target.closest(TEXT_ENTRY_SELECTOR) !== null;
}
