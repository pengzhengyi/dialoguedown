import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "@vscode/codicons/dist/codicon.css";
import "./styles.css";

import { runApp } from "./app";
import { watchServerEvents } from "./live-client";
import { createLiveEdit, type LiveEditController } from "./live-edit";
import { initLiveEditUi, type DocumentBinding } from "./live-edit-ui";
import { createSaveModeStore } from "./save-mode";
import { createModeToggle } from "./mode-toggle";
import { createModeController } from "./view-edit";
import { createConfig, browserConfigCreatePorts } from "./config-create";
import { resolveDocumentForNavigation } from "./navigation";
import { initModeBadge } from "./mode-badge";
import { initPathDisplay, initConfigPath } from "./path-display";
import { initBackToLauncher } from "./back-link";
import { initTheme } from "./theme";
import { mermaidPreviews } from "./mermaid-preview";
import { DEV_SOURCE, DEV_STAGES } from "./dev-stages";
import { initExplorer, resolveProjectPath, type ExplorerConfig } from "./explorer";
import { initEmptyShell } from "./empty-shell";
import { initCollapsiblePanel } from "./collapse-toggle";
import { createExplorerToggle, EXPLORER_PANEL_NAME } from "./explorer-toggle";
import {
    type ConfigReport,
    type DialogueSymbols,
    EMPTY_SYMBOLS,
    type Report,
    type ServedMode,
} from "./model";
import { parentPath, type BrowseListing, type CreateOutcome } from "./project-fs";

/**
 * The Explorer's pinned configuration entry for a report — present only when a `dialogue.toml`
 * was applied. The label is the file's leaf (separator-robust, so a Windows-authored report
 * reads cleanly too).
 */
function configExplorerEntry(configuration: ConfigReport | undefined): ExplorerConfig | undefined {
    const path = configuration?.file?.path;
    if (path === undefined) return undefined;
    return { label: path.split(/[\\/]/).pop() ?? path };
}

/**
 * The .NET library replaces the `"__REPORT__"` slot in report.html with the
 * report JSON, so `window.__DD_REPORT__` is an object at runtime. During local
 * development the placeholder is left as-is and a sample is shown instead.
 */
function resolveReport(): Report {
    const raw = (window as unknown as { __DD_REPORT__?: unknown }).__DD_REPORT__;
    if (raw && typeof raw === "object" && Array.isArray((raw as Report).stages)) {
        return raw as Report;
    }
    if (import.meta.env.DEV) return { source: DEV_SOURCE, stages: DEV_STAGES, mode: "edit" };
    return { stages: [] };
}

const report = resolveReport();
const header = document.querySelector<HTMLElement>(".app-header");

// Apply the saved color theme and mount the System/Light/Dark toggle (every mode).
initTheme(header?.querySelector(".header-controls") ?? null, () => {
    void mermaidPreviews.rerenderAll();
});

if ((report.mode === "view" || report.mode === "edit") && report.source == null && report.project) {
    // A served shell with no document open: mount the Explorer over the project root and show the
    // empty-state call to action. No live session yet — opening or creating a script navigates to
    // its report, which wires the editor, save modes, and hot reload.
    initEmptyShell(report);
} else if (report.mode === "view" || report.mode === "edit") {
    // A served session: two editable inputs to one dialogue compile — the dialogue source
    // (Source tab) and the config `dialogue.toml` (Config tab) — each read-only in View and
    // editable in Edit. The wiring closures reference the controllers only when invoked
    // (after they are created below).
    const initialMode: ServedMode = report.mode;
    const toggle = createModeToggle(initialMode, (mode) => controller.switchTo(mode));
    // The semantic analyzer's resolved symbols, refreshed on each hot-reload. The editor's
    // completion source reads this holder every call, so a reload updates completions in place.
    let currentSymbols: DialogueSymbols = report.symbols ?? EMPTY_SYMBOLS;
    const sourceStore = createSaveModeStore("source");
    const configStore = createSaveModeStore("config");
    // Only the latest navigation intent runs; a later navigation or a mode change bumps the token
    // so a superseded flush never replays a stale transition.
    let navToken = 0;
    // runApp activates a tab during construction (before `ui` and the controllers exist), so the
    // active-document reflection is armed only once everything below is wired.
    let controllersReady = false;

    // The active document's controller: the config when the Config tab is active, else the
    // dialogue (node-inspector edits share the Source controller). A Config tab with no config
    // file has no controller, so there is no active save controller (shared controls disable).
    const activeLive = (): LiveEditController | null =>
        app.isConfigTabActive() ? configLive : dialogueLive;

    // Resolve one document before navigation: Auto flushes and awaits the latest generation;
    // Manual awaits the current save, then prompts save-or-discard. A paused
    // conflict/uncertain/waiting/error stays in place, so navigation is never an implicit retry.
    function resolveDocument(
        live: LiveEditController,
        isCancelled: () => boolean = () => false,
    ): Promise<boolean> {
        return resolveDocumentForNavigation(
            live,
            () =>
                window.confirm(
                    "You have unsaved changes. Discard them to continue? " +
                        "Click Cancel to keep editing, then Save.",
                ),
            isCancelled,
        );
    }

    // The async navigation boundary for tabs and node selection: resolve the active document,
    // then run `proceed` — but only when a newer navigation has not superseded this one. A Config
    // tab with no controller has nothing to resolve, so navigation proceeds.
    function beginNavigation(proceed: () => void): void {
        const token = ++navToken;
        const live = activeLive();
        if (live === null) {
            proceed();
            return;
        }
        // A newer navigation (or a mode change) bumps navToken; pass that as the cancellation
        // signal so the Auto flush loop stops rather than saving on behalf of a superseded intent.
        void resolveDocument(live, () => token !== navToken).then((ok) => {
            if (ok && token === navToken) proceed();
        });
    }

    const app = runApp(report, {
        editable: initialMode === "edit",
        onChange: (buffer) => controller.onEditorChange(buffer),
        configOnChange: (buffer) => controller.onConfigEditorChange(buffer),
        onCreateConfig: async () => {
            await createConfig(browserConfigCreatePorts());
        },
        beginNavigation,
        onActiveTabChange: () => {
            if (controllersReady) ui.reflectActiveDocument();
        },
        symbols: () => currentSymbols,
    });
    const ui = initLiveEditUi(app, { active: () => activeLive() });
    const dialogueBinding: DocumentBinding = {
        type: "source",
        markDirty: app.markSourceDirty,
        setContent: app.setContent,
        applyReport: (applied) => {
            app.updateStages(applied.stages);
            // A save recompiles, so the analyzer's symbols change — refresh the completion
            // holder or the editor keeps offering the old speakers/ids.
            currentSymbols = applied.symbols ?? EMPTY_SYMBOLS;
            app.setDiagnostics(applied.diagnostics ?? []);
            app.setSemanticTokens(applied.semanticTokens ?? []);
            app.setReservedTargets(currentSymbols.reservedTargets);
        },
        diskSource: (applied) => applied.source ?? "",
    };
    const dialogueLive = createLiveEdit(
        ui.portsFor(dialogueBinding),
        {
            documentType: "source",
            mode: sourceStore.get(),
            initialReport: report,
            onModeChange: (m) => {
                sourceStore.set(m);
                navToken += 1; // a mode change clears any pending navigation
            },
        },
        report.source ?? "",
    );
    const configBinding: DocumentBinding = {
        type: "config",
        target: "config",
        markDirty: app.markConfigDirty,
        setContent: (source) => app.setConfigContent(source),
        applyReport: (applied) => {
            if (applied.configuration) app.updateConfig(applied.configuration);
            // A config recompile changes the graph too, so refresh the stages exactly once, like
            // the Source binding does.
            app.updateStages(applied.stages);
            // Editing the config changes the resolved speakers/ids, so refresh the Source
            // editor's completion symbols and diagnostics from the same recompile.
            currentSymbols = applied.symbols ?? EMPTY_SYMBOLS;
            app.setDiagnostics(applied.diagnostics ?? []);
            // A config recompile also changes the dialogue's highlighting (a newly known
            // speaker), so refresh the semantic tokens from the same report.
            app.setSemanticTokens(applied.semanticTokens ?? []);
            app.setReservedTargets(currentSymbols.reservedTargets);
        },
        diskSource: (applied) => applied.configuration?.file?.source ?? "",
        // The speakers pane is stale whenever the buffer's config is not the compiled report.
        onStatus: (status) => app.setConfigStale(status !== "saved"),
    };
    const configInitiallyInvalid = report.configStatus === "saved-invalid";
    const configLive: LiveEditController | null = report.configuration?.file
        ? createLiveEdit(
              ui.portsFor(configBinding),
              {
                  documentType: "config",
                  mode: configStore.get(),
                  initialValid: !configInitiallyInvalid,
                  initialMessage: configInitiallyInvalid ? report.configMessage : undefined,
                  initialReport: report,
                  onModeChange: (m) => {
                      configStore.set(m);
                      navToken += 1;
                  },
              },
              report.configuration.file.source,
          )
        : null;
    // A page that loaded with a persisted-but-invalid Config starts with a stale report: the
    // speakers pane reflects the last valid compile, not the invalid buffer now in the editor.
    if (configInitiallyInvalid) app.setConfigStale(true);
    controllersReady = true;
    const controller = createModeController(initialMode, {
        app,
        dialogueLive,
        configLive,
        setEditControlsVisible: ui.setEditControlsVisible,
        reflect: (mode) => {
            // Drive the blue (View) / green (Edit) accent, then the toggle's pressed state.
            document.documentElement.dataset.servedMode = mode;
            toggle.reflect(mode);
        },
        resolveDocument,
    });
    document.getElementById("mode-badge")?.replaceWith(toggle.element);
    watchServerEvents({
        onReload: (next) => {
            currentSymbols = next.symbols ?? EMPTY_SYMBOLS;
            controller.onReload(next);
        },
        onReloadConfig: (next) => {
            currentSymbols = next.symbols ?? EMPTY_SYMBOLS;
            controller.onReloadConfig(next);
        },
        onProblem: (message, target) => controller.onProblem(message, target),
    });
    // The Explorer sidebar: present only for a served, browsable report (report.project is set by
    // the project server). It reuses the launcher's browse/open endpoints and routes a file open
    // through beginNavigation, so switching scripts respects the save mode (Auto flushes, Manual
    // prompts) before the server switches sessions.
    if (report.project) {
        const explorerEl = document.getElementById("explorer");
        const appEl = document.getElementById("app");
        if (explorerEl && appEl) {
            appEl.classList.add("has-explorer");
            initExplorer(
                explorerEl,
                report.project,
                {
                    browse: async (path) => {
                        const response = await fetch(
                            `/api/browse?path=${encodeURIComponent(path)}`,
                        );
                        return response.ok ? ((await response.json()) as BrowseListing) : null;
                    },
                    openScript: (path) =>
                        beginNavigation(() => {
                            void openScriptSession(path);
                        }),
                    create: async (path) => {
                        // Resolve the current document save-safely first; a cancelled Manual save
                        // aborts the create (empty message → the Explorer shows nothing).
                        const live = activeLive();
                        if (live && !(await resolveDocument(live))) {
                            return { kind: "error", message: "" };
                        }
                        return createScriptSession(path);
                    },
                    createFolder: async (path) => {
                        const response = await fetch("/api/create-folder", {
                            method: "POST",
                            headers: { "content-type": "application/json" },
                            body: JSON.stringify({ path }),
                        });
                        if (response.ok) return { ok: true };
                        const body = (await response.json().catch(() => ({}))) as {
                            message?: string;
                        };
                        return {
                            ok: false,
                            message: body.message ?? "Could not create the folder.",
                        };
                    },
                    rename: async (from, to) => {
                        // A rename that carries the document on screen — the file itself, or a
                        // file inside a renamed folder — saves it first (save-safe) and reopens it
                        // under the new path; a cancelled Manual save aborts the rename.
                        const activePath = report.project?.activePath;
                        const carriesActive =
                            activePath === from || (activePath?.startsWith(`${from}/`) ?? false);
                        if (carriesActive) {
                            const live = activeLive();
                            if (live && !(await resolveDocument(live))) {
                                return { kind: "error", message: "" };
                            }
                        }
                        const response = await fetch("/api/rename", {
                            method: "POST",
                            headers: { "content-type": "application/json" },
                            body: JSON.stringify({ from, to }),
                        });
                        if (response.ok) {
                            const body = (await response.json().catch(() => ({}))) as {
                                path?: string;
                                active?: boolean;
                                activePath?: string;
                            };
                            const path = body.path ?? to;
                            if (body.active) void openScriptSession(body.activePath ?? path);
                            return { kind: "renamed", path };
                        }
                        if (response.status === 409) return { kind: "exists" };
                        const body = (await response.json().catch(() => ({}))) as {
                            message?: string;
                        };
                        return {
                            kind: "error",
                            message: body.message ?? "Could not rename the file.",
                        };
                    },
                    confirm: (message) => window.confirm(message),
                    openConfig: () => app.showConfigTab(),
                },
                configExplorerEntry(report.configuration),
            );
            // Shut on arrival: the reader asked for this script, so the tree is a detour. The
            // Files control in the tab bar summons it, and an explicit choice outranks this.
            const explorerPanel = initCollapsiblePanel({
                container: appEl,
                collapsedClass: "explorer-collapsed",
                storageKey: "dd-explorer-collapsed",
                name: EXPLORER_PANEL_NAME,
                startCollapsed: true,
                createButton: createExplorerToggle,
            });
            document.getElementById("tabbar-leading")?.appendChild(explorerPanel.button);

            // A cross-file link in the Source preview opens the target script like a hyperlink;
            // same-file #anchors keep their native scroll, and the anchor part is dropped (the
            // linker resolves anchors, deferred).
            const activeFolder = parentPath(report.project.activePath ?? "");
            for (const preview of document.querySelectorAll(".source-preview")) {
                preview.addEventListener("click", (event) => {
                    const anchor = (event.target as Element | null)?.closest("a");
                    const href = anchor?.getAttribute("href");
                    if (href == null || href.startsWith("#") || /^[a-z][\w+.-]*:/i.test(href)) {
                        return;
                    }
                    event.preventDefault();
                    const target = resolveProjectPath(activeFolder, href);
                    if (target !== null) {
                        beginNavigation(() => {
                            void openScriptSession(target);
                        });
                    }
                });
            }
        }
    }

    // Open another script: preserve the current View/Edit mode (read from the live accent the
    // toggle sets), start its session, and follow the 303 to its report.
    async function openScriptSession(source: string): Promise<void> {
        const mode = document.documentElement.dataset.servedMode ?? initialMode;
        const response = await fetch("/api/open", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ source, mode }),
        });
        if (response.redirected) window.location.assign(response.url);
    }

    // Create a new script (POST /api/create) and follow the 303 to its report; a name clash is a
    // 409 the Explorer turns into "open the existing one instead".
    async function createScriptSession(path: string): Promise<CreateOutcome> {
        const response = await fetch("/api/create", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ path }),
        });
        if (response.redirected) {
            window.location.assign(response.url);
            return { kind: "opened", url: response.url };
        }
        if (response.status === 409) {
            const body = (await response.json().catch(() => ({}))) as { path?: string };
            return { kind: "exists", path: body.path ?? path };
        }
        const body = (await response.json().catch(() => ({}))) as { message?: string };
        return { kind: "error", message: body.message ?? "Could not create the file." };
    }

    if (header) initBackToLauncher(header, window.location.pathname);
} else {
    // Static export: read-only, no server, no toggle.
    runApp(report);
    initModeBadge("static");
    if (header) initBackToLauncher(header, window.location.pathname);
}

initPathDisplay(report.path);
initConfigPath(report.configuration);
