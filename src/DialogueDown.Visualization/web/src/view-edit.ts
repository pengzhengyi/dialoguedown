import type { AppController } from "./app";
import type { LiveEditController } from "./live-edit";
import type { Report, ServedMode } from "./model";

/** The side-effecting collaborators the mode controller drives (injected for testing). */
export interface ModeControllerPorts {
    /** Reconfigures the editors (editable/read-only, content) and the graph tabs. */
    app: AppController;
    /** The dialogue document's Edit-mode dirty/save state machine. */
    dialogueLive: LiveEditController;
    /** The config document's state machine, when the compile applied a `dialogue.toml`. */
    configLive: LiveEditController | null;
    /** Shows or hides the Save and Discard buttons (Edit only). */
    setEditControlsVisible(visible: boolean): void;
    /** Reflects the active mode on the toggle control. */
    reflect(mode: ServedMode): void;
    /**
     * Resolves one document before leaving Edit: flush an Auto save, or run the Manual
     * save-or-discard prompt. Resolves true when it is safe to leave, false to stay in Edit
     * (a paused conflict/uncertain, or a declined prompt).
     */
    resolveDocument(controller: LiveEditController): Promise<boolean>;
}

/** Drives a served session's View ⇄ Edit lifecycle. */
export interface ModeController {
    /** The active mode. */
    readonly mode: ServedMode;
    /** Route a Source-editor change: dirty tracking in Edit, ignored in View (a reload). */
    onEditorChange(buffer: string): void;
    /** Route a config-editor change: dirty tracking in Edit, ignored in View. */
    onConfigEditorChange(buffer: string): void;
    /** Route a pushed report: View re-syncs the editor + graphs; Edit raises the chip. */
    onReload(report: Report): void;
    /** Route a pushed config report (external `dialogue.toml` change): View re-syncs, Edit chips. */
    onReloadConfig(report: Report): void;
    /**
     * Route a pushed problem (a missing or unreadable document/config on disk). In Edit it enters
     * the target controller's Conflict/epoch path so an in-flight save cannot silently overwrite
     * the file; in View it surfaces the message as a banner.
     */
    onProblem(message: string, target?: ProblemTarget): void;
    /** Request a mode switch (from the toggle). */
    switchTo(mode: ServedMode): void;
}

/** Which document a disk problem event is about, so it routes to the matching controller. */
export type ProblemTarget = "document" | "config";

/**
 * The View ⇄ Edit controller for a served session. View is read-only and auto-updating
 * (a disk change re-syncs the editor and graphs); Edit is editable and owns its buffers
 * (a disk change only raises the passive chip). Switching reconfigures both editors in
 * place — the buffers, cursors, and history survive — and prompts before discarding unsaved
 * edits when leaving Edit. At most one document is dirty at a time (navigation is locked
 * while dirty), so leaving Edit discards whichever one it is.
 */
export function createModeController(
    initial: ServedMode,
    ports: ModeControllerPorts,
): ModeController {
    let mode: ServedMode = initial;
    // A monotonic transition token. Leaving Edit is async (it awaits each document's flush or
    // prompt); reasserting Edit, or a newer switch, bumps this so a stale in-flight transition can
    // never apply View after the writer changed their mind.
    let transition = 0;

    function apply(next: ServedMode): void {
        mode = next;
        ports.app.setEditable(next === "edit");
        ports.app.setConfigEditable(next === "edit");
        ports.setEditControlsVisible(next === "edit");
        ports.reflect(next);
    }

    apply(initial);

    return {
        get mode() {
            return mode;
        },
        onEditorChange(buffer) {
            if (mode === "edit") ports.dialogueLive.onEdit(buffer);
        },
        onConfigEditorChange(buffer) {
            if (mode === "edit") ports.configLive?.onEdit(buffer);
        },
        onReload(report) {
            if (mode === "edit") {
                ports.dialogueLive.onDiskChange(); // external change → Conflict; never clobber
                return;
            }
            ports.app.showBanner(null);
            if (report.source != null) {
                ports.app.setContent(report.source);
                // Adopt the external content as the dialogue controller's clean baseline, so a
                // later Edit session starts from what is on disk, not stale text. Pass the report
                // too, so a later Discard reapplies these overlays rather than the pre-reload ones.
                ports.dialogueLive.adoptDisk(report.source, true, undefined, report);
            }
            ports.app.setDiagnostics(report.diagnostics ?? []);
            ports.app.setSemanticTokens(report.semanticTokens ?? []);
            ports.app.setReservedTargets(report.symbols?.reservedTargets ?? []);
            ports.app.updateStages(report.stages);
        },
        onReloadConfig(report) {
            if (mode === "edit") {
                // External config change → Conflict on the config controller; never clobber it.
                ports.configLive?.onDiskChange();
                return;
            }
            ports.app.showBanner(null);
            const source = report.configuration?.file?.source;
            if (report.configuration != null) ports.app.updateConfig(report.configuration);
            if (source != null) ports.app.setConfigContent(source);
            ports.app.setDiagnostics(report.diagnostics ?? []);
            ports.app.setSemanticTokens(report.semanticTokens ?? []);
            ports.app.setReservedTargets(report.symbols?.reservedTargets ?? []);
            ports.app.updateStages(report.stages);
            // Adopt the external config as the config controller's clean baseline; an invalid
            // reload keeps the last valid report but adopts the (invalid) text as saved-invalid,
            // carrying its parse detail so a later Discard restores it.
            if (source != null) {
                const invalid = (report as { outcome?: string }).outcome === "invalid";
                ports.configLive?.adoptDisk(
                    source,
                    !invalid,
                    invalid ? report.configMessage : undefined,
                    report,
                );
            }
        },
        switchTo(next) {
            if (next === mode) {
                // Reasserting the current mode cancels any in-flight transition away from it — a
                // click back into Edit while an Edit→View flush is still awaiting must keep Edit.
                transition += 1;
                return;
            }
            if (mode === "edit") {
                void leaveEditThen(next);
                return;
            }
            apply(next);
        },
        onProblem(message, target) {
            const live = target === "config" ? ports.configLive : ports.dialogueLive;
            if (mode === "edit" && live) {
                // A deletion/read failure under an active buffer is a disk change: route it through
                // the controller so an in-flight save is invalidated (epoch) and the writer is
                // paused in Conflict, rather than only flashing a banner that a save could ignore.
                live.onDiskChange(message);
                return;
            }
            ports.app.showBanner(message);
        },
    };

    // Leaving Edit is navigation: flush each Auto document and await it, or run the Manual
    // save-or-discard prompt. A paused conflict/uncertain, or a declined prompt, keeps Edit. The
    // transition token is rechecked after every await and before the final apply, so a reasserted
    // Edit (or a newer switch) cancels this transition rather than dropping into a stale View.
    async function leaveEditThen(next: ServedMode): Promise<void> {
        const token = ++transition;
        for (const controller of [ports.configLive, ports.dialogueLive]) {
            if (!controller) continue;
            const resolved = await ports.resolveDocument(controller);
            if (token !== transition) return; // superseded: a reasserted Edit or a newer switch won
            if (!resolved) return;
        }
        if (token !== transition) return;
        ports.dialogueLive.discard();
        ports.configLive?.discard();
        apply(next);
    }
}
