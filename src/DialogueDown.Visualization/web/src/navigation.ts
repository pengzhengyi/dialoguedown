import type { LiveEditController, SaveStatus } from "./live-edit";

/** The reader's answer to the Manual "discard unsaved changes?" navigation prompt. */
export type ConfirmDiscard = () => boolean;

const PAUSED: ReadonlySet<SaveStatus> = new Set<SaveStatus>([
    "conflict",
    "uncertain",
    "waiting",
    "error",
]);

/**
 * Resolve one document before navigation continues (a tab change, a jump between the source and a
 * stage, opening another script, or Edit→View).
 *
 * The current save (if any) is awaited first, so a decision is made on a settled state rather than
 * mid-write: Manual never prompts while a save is in flight, and its Discard is never a no-op.
 * A paused conflict/uncertain/waiting/error stays in place — navigation is never an implicit retry.
 * In Auto it flushes and awaits the latest generation, looping until the buffer is clean, so an
 * edit made during a flush is saved before navigation proceeds. It rechecks the mode and the
 * optional {@link isCancelled} signal before every follow-up flush, so switching to Manual (or a
 * newer navigation superseding this one) stops the follow-up saving and cancels this navigation
 * rather than driving stale Auto flushes. In Manual it asks the reader whether to discard whatever
 * remains dirty.
 */
export async function resolveDocumentForNavigation(
    live: LiveEditController,
    confirmDiscard: ConfirmDiscard,
    isCancelled: () => boolean = () => false,
): Promise<boolean> {
    await live.whenIdle();
    if (isCancelled()) return false;
    if (PAUSED.has(live.status)) return false;
    if (!live.dirty) return true;

    if (live.mode === "auto") {
        while (live.dirty) {
            // Recheck before every (follow-up) flush: an Auto→Manual switch or a superseding
            // navigation cancels the loop instead of continuing to save and navigate.
            if (live.mode !== "auto" || isCancelled()) return false;
            await live.flush();
            if (PAUSED.has(live.status)) return false;
            // A follow-up save may have been queued for edits made during the flush; let it settle
            // before re-checking, then flush again if the buffer is still dirty.
            await live.whenIdle();
        }
        return true;
    }

    const discard = confirmDiscard();
    if (discard) live.discardChanges();
    return discard;
}
