import { runApp } from "./app";
import { initExplorer, type ExplorerHandle } from "./explorer";
import { codicon } from "./codicon";
import { initCollapsiblePanel } from "./collapse-toggle";
import { createExplorerToggle, EXPLORER_PANEL_NAME } from "./explorer-toggle";
import { createModeToggle } from "./mode-toggle";
import { readPreferredMode, writePreferredMode } from "./preferred-mode";
import { setHelp } from "./help";
import type { Report, ServedMode } from "./model";
import type { BrowseListing, CreateOutcome } from "./project-fs";

/**
 * The served shell's **empty state**: no document is open, so mount the Explorer over the project
 * root and show a centered call to action to open a script or create the first one. This path skips
 * the live-session machinery (hot reload, save modes, the View/Edit toggle) — there is nothing to
 * edit yet; opening or creating a script navigates to that document's report, which wires it all.
 */
export function initEmptyShell(report: Report): void {
    const project = report.project;
    if (project === undefined) return;

    // Build the shell chrome; with no source, stages, or config it renders no tabs — just the frame
    // the Explorer and the call to action sit in.
    runApp(report);

    // There is no active tab to explain, so the footer help describes the Explorer instead.
    setHelp("explorer");

    // The View/Edit choice is the reader's and it outlives this page: the shell offers the mode
    // they last chose, remembers the next one, and opens the script they pick in it. It is the
    // session's own toggle, mounted where the badge sits, so the choice reads the same in both.
    let chosen = readPreferredMode() ?? (report.mode === "edit" ? "edit" : "view");
    // Writing is Edit's half of the shell: the Explorer's create actions and the card's call to
    // action follow the switch rather than promising something this mode will not do.
    const createHint = "Switch to Edit to create files and folders.";
    let explorer: ExplorerHandle | null = null;
    let createButton: HTMLButtonElement | null = null;
    let hint: HTMLElement | null = null;
    const applyMode = (mode: ServedMode): void => {
        chosen = mode;
        writePreferredMode(mode);
        document.documentElement.dataset.servedMode = mode;
        toggle.reflect(mode);
        explorer?.setEditable(mode === "edit");
        const editable = mode === "edit";
        if (createButton) {
            createButton.disabled = !editable;
            createButton.title = editable ? "" : createHint;
        }
        // The hint keeps its welcome in Edit and points at the switch in View, rather than being
        // cleared: the call to action sits right below it either way.
        if (hint) {
            hint.textContent = editable
                ? "Pick a script from the Explorer on the left, or start a new one below."
                : "Switch to Edit to create a new script.";
        }
    };
    const toggle = createModeToggle(chosen, applyMode);
    document.getElementById("mode-badge")?.replaceWith(toggle.element);
    document.documentElement.dataset.servedMode = chosen;

    const explorerEl = document.getElementById("explorer");
    const appEl = document.getElementById("app");
    const stagesEl = document.getElementById("stages");
    if (explorerEl === null || appEl === null || stagesEl === null) return;
    // The Explorer sits on the left; there is no node graph, so hide the detail panel.
    appEl.classList.add("has-explorer", "no-detail");

    // Opening or creating starts a session on the server and follows the 303 to that document's
    // report, in the mode the reader chose above. With nothing open there is no save-safe step —
    // the navigation is a fresh start.
    const openScriptSession = async (source: string): Promise<void> => {
        const response = await fetch("/api/open", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ source, mode: chosen }),
        });
        if (response.redirected) window.location.assign(response.url);
    };
    const createScriptSession = async (path: string): Promise<CreateOutcome> => {
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
    };

    explorer = initExplorer(explorerEl, project, {
        browse: async (path) => {
            const response = await fetch(`/api/browse?path=${encodeURIComponent(path)}`);
            return response.ok ? ((await response.json()) as BrowseListing) : null;
        },
        openScript: (path) => {
            void openScriptSession(path);
        },
        create: (path) => createScriptSession(path),
        createFolder: async (path) => {
            const response = await fetch("/api/create-folder", {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ path }),
            });
            if (response.ok) return { ok: true };
            const body = (await response.json().catch(() => ({}))) as { message?: string };
            return { ok: false, message: body.message ?? "Could not create the folder." };
        },
        rename: async (from, to) => {
            const response = await fetch("/api/rename", {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ from, to }),
            });
            if (response.ok) return { kind: "renamed", path: to };
            if (response.status === 409) return { kind: "exists" };
            const body = (await response.json().catch(() => ({}))) as { message?: string };
            return { kind: "error", message: body.message ?? "Could not rename the file." };
        },
        openConfig: () => {
            /* no config entry is shown in the empty state */
        },
        confirm: (message) => window.confirm(message),
    });

    // Open on arrival here, unlike a session with a document: nothing is showing, so the tree is
    // not a detour but the only thing to do — the call to action below points straight at it.
    const explorerPanel = initCollapsiblePanel({
        container: appEl,
        collapsedClass: "explorer-collapsed",
        storageKey: "dd-explorer-collapsed",
        name: EXPLORER_PANEL_NAME,
        createButton: createExplorerToggle,
    });
    document.getElementById("tabbar-leading")?.appendChild(explorerPanel.button);

    // The call to action in the main pane. "New dialogue file" runs the Explorer's own create flow
    // (its header New File action), so naming and creation happen in the tree as everywhere else.
    const card = document.createElement("section");
    card.className = "stage empty-shell active";
    card.innerHTML =
        `<div class="empty-shell-card">` +
        `<h2 class="empty-shell-title">No script open</h2>` +
        `<p class="empty-shell-hint">Pick a script from the Explorer on the left, or start a new ` +
        `one below.</p>` +
        `<button type="button" class="empty-shell-create"></button>` +
        `</div>`;
    stagesEl.appendChild(card);
    createButton = card.querySelector<HTMLButtonElement>(".empty-shell-create");
    hint = card.querySelector<HTMLElement>(".empty-shell-hint");
    // A start-page row rather than a button: the mark the Explorer's own New file action wears,
    // its words at the report's own size, and the ellipsis that says a name is asked for next.
    const createLabel = document.createElement("span");
    createLabel.textContent = "New dialogue file…";
    createButton?.append(codicon("new-file", "empty-shell-create-icon"), createLabel);
    createButton?.addEventListener("click", () => {
        explorerEl.querySelector<HTMLButtonElement>('[data-action="new-file"]')?.click();
    });
    // The card is built after the switch above, so it starts in whatever mode the reader is in.
    applyMode(chosen);
}
