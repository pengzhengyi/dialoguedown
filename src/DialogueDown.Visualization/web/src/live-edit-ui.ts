import type { AppController } from "./app";
import type {
    DiskLoad,
    LiveEditController,
    LiveEditPorts,
    SaveOutcome,
    SaveRequest,
    SaveStatus,
} from "./live-edit";
import type { DocumentType, SaveMode } from "./save-mode";
import type { Report } from "./model";

const SAVE_URL = "/api/save";
const RELOAD_URL = "/api/reload";

/** The fixed trailing-edge idle delay before an Auto save fires (VS Code Web's default). */
export const IDLE_DELAY_MS = 1000;

/** The accessible label shown for each save status; detail messages are appended as a title. */
const STATUS_LABEL: Record<SaveStatus, string> = {
    saved: "Saved",
    dirty: "Unsaved",
    saving: "Saving…",
    conflict: "Conflict",
    waiting: "Waiting for valid TOML",
    "saved-invalid": "Saved — invalid TOML",
    uncertain: "Uncertain",
    error: "Save failed",
};

/** Binds one editable document to the shared Live Edit chrome (capsule, status, Save/Discard). */
export interface DocumentBinding {
    /** Which document this binds — drives its save mode preference and status reflection. */
    type: DocumentType;
    /** The save target: omitted for the dialogue document, `"config"` for `dialogue.toml`. */
    target?: "config";
    /** Toggle this document's own dirty cue (its tab marker, and any per-document staleness). */
    markDirty(dirty: boolean): void;
    /** Replace this document's editor content — restoring a saved baseline or external text. */
    setContent(source: string): void;
    /** Show a different document in this editor, dropping the previous one's undo history. */
    setDocument(source: string): void;
    /** Apply an accepted report beyond the graphs (config speakers, symbols, diagnostics). */
    applyReport(report: Report): void;
    /** Extract this document's on-disk editor source from a reload payload. */
    diskSource(report: Report): string;
    /** React to a status change (e.g. mark the config speakers stale unless Saved). */
    onStatus?(status: SaveStatus): void;
}

/** The shared Live Edit chrome plus a factory that binds it to one editable document. */
export interface LiveEditUi {
    /** Build the Live Edit ports for one editable document, sharing the capsule/status chrome. */
    portsFor(binding: DocumentBinding): LiveEditPorts;
    /** Show or hide the Live Edit controls as the session enters or leaves Edit. */
    setEditControlsVisible(visible: boolean): void;
    /** Re-render the capsule, status, and actions from the currently active document's controller. */
    reflectActiveDocument(): void;
}

/** The accessors the shared chrome needs into the live controllers. */
export interface LiveEditActions {
    /** The controller for the document the current UI action targets (Config tab → config, else source). */
    active(): LiveEditController | null;
}

/**
 * The browser-side chrome for the Live Edit state machine: a persisted Auto/Manual capsule beside
 * a separate Save button, a Discard button, a Reload-from-disk action for conflicts, an accessible
 * save-status readout, the ⌘/Ctrl-S shortcut, and a shared `beforeunload` guard. The
 * {@link LiveEditUi.portsFor} factory binds these to one editable document; all shared controls act
 * on {@link LiveEditActions.active}, so they always target the active document.
 */
export function initLiveEditUi(app: AppController, actions: LiveEditActions): LiveEditUi {
    const guarded = new Set<DocumentType>();
    let unloadHandler: ((event: BeforeUnloadEvent) => void) | null = null;

    const capsule = createModeCapsule((mode) => {
        actions.active()?.setMode(mode);
        reflectActiveDocument();
    });
    const status = createStatusReadout();
    const reloadButton = createAction(
        "reload-button",
        "Reload",
        () => void actions.active()?.reload(),
    );
    const discardButton = createAction("discard-button", "Discard", onDiscard);
    const saveButton = createAction("save-button", "Save", onSave);
    installSaveShortcut(onSave);

    function onSave(): void {
        const controller = actions.active();
        if (controller === null) return;
        if (controller.status === "conflict" || controller.status === "uncertain") {
            const overwrite = window.confirm(
                "Overwrite the file on disk with your version? Any external changes will be lost.",
            );
            if (overwrite) void controller.save({ overwrite: true });
            return;
        }
        void controller.save();
    }

    function onDiscard(): void {
        const controller = actions.active();
        if (controller === null || !controller.canDiscard) return;
        if (window.confirm("Discard all unsaved changes and restore the last saved version?")) {
            controller.discardChanges();
        }
    }

    function setUnloadGuard(type: DocumentType, active: boolean): void {
        if (active) guarded.add(type);
        else guarded.delete(type);
        const shouldGuard = guarded.size > 0;
        if (shouldGuard && unloadHandler === null) {
            unloadHandler = (event) => {
                event.preventDefault();
                event.returnValue = "";
            };
            window.addEventListener("beforeunload", unloadHandler);
        } else if (!shouldGuard && unloadHandler !== null) {
            window.removeEventListener("beforeunload", unloadHandler);
            unloadHandler = null;
        }
    }

    function reflectActiveDocument(): void {
        const controller = actions.active();
        if (controller === null) {
            status.render("saved");
            capsule.reflect("auto", false);
            saveButton.setEnabled(false);
            discardButton.setEnabled(false);
            reloadButton.setVisible(false);
            return;
        }
        const current = controller.status;
        capsule.reflect(controller.mode, true);
        status.render(current, controller.statusMessage);
        saveButton.setEnabled(canSave(controller));
        discardButton.setEnabled(controller.canDiscard && canDiscardNow(current, controller.dirty));
        const showReload = current === "conflict" || current === "uncertain";
        reloadButton.setVisible(showReload);
        reloadButton.setEnabled(showReload);
    }

    return {
        portsFor: (binding) => ({
            save: (request) => postSave(request),
            loadFromDisk: () => loadFromDisk(binding),
            applyReport: (report) => binding.applyReport(report),
            setContent: (source) => binding.setContent(source),
            setDocument: (source) => binding.setDocument(source),
            setDirty: (dirty) => {
                binding.markDirty(dirty);
                reflectActiveDocument();
            },
            setStatus: (state) => {
                binding.onStatus?.(state);
                reflectActiveDocument();
            },
            setUnloadGuard: (active) => setUnloadGuard(binding.type, active),
            scheduleIdle: (callback) => {
                const id = window.setTimeout(callback, IDLE_DELAY_MS);
                return () => window.clearTimeout(id);
            },
        }),
        setEditControlsVisible: (visible) => {
            capsule.setVisible(visible);
            status.setVisible(visible);
            reloadButton.setVisible(false);
            discardButton.setVisible(visible);
            saveButton.setVisible(visible);
            if (visible) reflectActiveDocument();
        },
        reflectActiveDocument,
    };

    async function postSave(request: SaveRequest): Promise<SaveOutcome> {
        // A transport failure (fetch rejecting) propagates so the controller enters Uncertain.
        const response = await fetch(SAVE_URL, {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({
                source: request.source,
                target: request.target,
                expectedBaseline: request.expectedBaseline,
                validation: request.validation,
                conflict: request.conflict,
            }),
        });
        if (!response.ok) {
            app.showBanner("Save failed — the file could not be written.");
            return { kind: "failure", message: await errorMessage(response) };
        }
        app.showBanner(null);
        const body = (await response.json()) as SavePayload;
        switch (body.outcome) {
            case "saved":
                return { kind: "saved", report: body, source: request.source };
            case "saved-invalid":
                return {
                    kind: "saved-invalid",
                    report: body,
                    source: request.source,
                    message: body.message ?? "Invalid TOML.",
                };
            case "invalid-auto":
                return { kind: "invalid-auto", message: body.message ?? "Invalid TOML." };
            case "conflict":
                return { kind: "conflict", message: body.message ?? "The file changed on disk." };
            case "uncertain":
                return {
                    kind: "uncertain",
                    message:
                        body.message ??
                        "The file's state on disk is uncertain; reload or overwrite.",
                };
            default:
                return { kind: "failure", message: body.message ?? "Save failed." };
        }
    }

    async function loadFromDisk(binding: DocumentBinding): Promise<DiskLoad> {
        let response: Response;
        try {
            response = await fetch(RELOAD_URL, {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ target: binding.target }),
            });
        } catch {
            return { kind: "failure", message: "The server could not be reached." };
        }
        if (!response.ok) return { kind: "failure", message: await errorMessage(response) };
        const body = (await response.json()) as SavePayload;
        if (body.outcome === "missing") return { kind: "missing" };
        const source = binding.diskSource(body);
        if (body.outcome === "invalid") {
            return {
                kind: "invalid",
                source,
                report: body,
                message: body.message ?? "Invalid TOML.",
            };
        }
        return { kind: "loaded", source, report: body };
    }
}

/** Whether Save should be enabled: while dirty, or while a paused state can retry/overwrite. */
function canSave(controller: LiveEditController): boolean {
    if (controller.dirty) return true;
    return (
        controller.status === "error" ||
        controller.status === "waiting" ||
        controller.status === "conflict" ||
        controller.status === "uncertain"
    );
}

function canDiscardNow(status: SaveStatus, dirty: boolean): boolean {
    return dirty || status === "waiting" || status === "error";
}

/** The server's save/reload payload — the document report plus a typed outcome and message. */
type SavePayload = Report & {
    outcome:
        | "saved"
        | "saved-invalid"
        | "invalid-auto"
        | "conflict"
        | "uncertain"
        | "loaded"
        | "invalid"
        | "missing";
    message?: string;
};

/** The Auto/Manual segmented capsule, reflecting and setting the active document's save mode. */
function createModeCapsule(onToggle: (mode: SaveMode) => void): {
    reflect(mode: SaveMode, enabled: boolean): void;
    setVisible(visible: boolean): void;
} {
    const group = document.createElement("div");
    group.className = "save-mode-capsule";
    group.setAttribute("role", "group");
    group.setAttribute("aria-label", "Save mode");
    group.hidden = true;
    const options: Record<SaveMode, HTMLButtonElement> = {
        auto: capsuleOption("Auto", "Save automatically 1s after you stop typing"),
        manual: capsuleOption("Manual", "Save only on ⌘/Ctrl-S or the Save button"),
    };
    (Object.keys(options) as SaveMode[]).forEach((mode) => {
        options[mode].addEventListener("click", () => onToggle(mode));
        group.appendChild(options[mode]);
    });
    document.querySelector(".status-bar")?.appendChild(group);
    return {
        reflect: (mode, enabled) => {
            (Object.keys(options) as SaveMode[]).forEach((key) => {
                options[key].setAttribute("aria-pressed", String(key === mode));
                options[key].disabled = !enabled;
            });
        },
        setVisible: (visible) => {
            group.hidden = !visible;
        },
    };
}

function capsuleOption(label: string, title: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.className = "save-mode-option";
    button.type = "button";
    button.textContent = label;
    button.title = title;
    button.setAttribute("aria-pressed", "false");
    return button;
}

/** The accessible save-status readout (aria-live polite) beside the actions. */
function createStatusReadout(): {
    render(status: SaveStatus, message?: string): void;
    setVisible(visible: boolean): void;
} {
    const readout = document.createElement("span");
    readout.className = "save-status";
    readout.setAttribute("aria-live", "polite");
    readout.hidden = true;
    document.querySelector(".status-bar")?.appendChild(readout);
    return {
        render: (status, message) => {
            readout.dataset.status = status;
            // Announce the detail alongside the label in the aria-live region itself — a title
            // tooltip is not read out — so a screen reader hears "Saved — invalid TOML: <error>".
            readout.textContent = message
                ? `${STATUS_LABEL[status]}: ${message}`
                : STATUS_LABEL[status];
            if (message) readout.title = message;
            else readout.removeAttribute("title");
        },
        setVisible: (visible) => {
            readout.hidden = !visible;
        },
    };
}

/** A status-bar action button (Save/Discard/Reload), shown only in Edit. */
function createAction(
    className: string,
    label: string,
    onClick: () => void,
): { setEnabled(enabled: boolean): void; setVisible(visible: boolean): void } {
    const button = document.createElement("button");
    button.className = className;
    button.type = "button";
    button.textContent = label;
    button.disabled = true;
    button.hidden = true;
    button.addEventListener("click", onClick);
    document.querySelector(".status-bar")?.appendChild(button);
    return {
        setEnabled: (enabled) => {
            button.disabled = !enabled;
        },
        setVisible: (visible) => {
            button.hidden = !visible;
        },
    };
}

/**
 * Install the Save shortcut on the whole document so it works from any tab regardless of focus.
 * Intercepting both Ctrl+S and ⌘S prevents the browser's own "save page" dialog either way.
 */
function installSaveShortcut(onSave: () => void): void {
    document.addEventListener("keydown", (event) => {
        if ((event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === "s") {
            event.preventDefault();
            onSave();
        }
    });
}

async function errorMessage(response: Response): Promise<string> {
    const fallback = "The file could not be written.";
    try {
        const body = (await response.json()) as { message?: string };
        return body.message ?? fallback;
    } catch {
        return fallback;
    }
}
