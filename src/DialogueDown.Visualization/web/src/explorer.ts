/**
 * The Explorer client: a lazy, expand/collapse tree of the project root's folders and
 * `.dialogue.md` scripts, with the active script highlighted and revealed. A folder loads its
 * children the first time it is expanded (one `GET /api/browse` per folder). The DOM building
 * lives here (unit-tested with jsdom); the browser wiring — `fetch`, and save-safe navigation —
 * is injected through {@link ExplorerPorts}, mirroring the launcher.
 */

import { leafName, SCRIPT_EXTENSION, type BrowseListing, type CreateOutcome } from "./project-fs";
import { codicon } from "./codicon";
import { openContextMenu } from "./context-menu";
import type { ReportProject } from "./model";

/** The outcome of a folder-create request. */
export type CreateFolderOutcome = { ok: true } | { ok: false; message: string };

/** The outcome of a script-rename request. */
export type RenameOutcome =
    { kind: "renamed"; path: string } | { kind: "exists" } | { kind: "error"; message: string };

/** The project's configuration file, shown as a pinned Explorer entry above the tree. */
export interface ExplorerConfig {
    /** The file's display label, e.g. "dialogue.toml". */
    label: string;
}

/** The side-effecting collaborators, injected so the tree is testable without a server. */
export interface ExplorerPorts {
    /** One folder's children (`GET /api/browse`); `null` when it escapes the root or is missing. */
    browse(path: string): Promise<BrowseListing | null>;
    /** Open a script by its root-relative path — save-safe navigation, wired by the host. */
    openScript(path: string): void;
    /** Create a script at a root-relative path — save-safe; the host navigates on success. */
    create(path: string): Promise<CreateOutcome>;
    /** Create a folder at a root-relative path; the host refreshes the tree on success. */
    createFolder(path: string): Promise<CreateFolderOutcome>;
    /** Rename (move) a script to a new root-relative path; the host reopens it if it was active. */
    rename(from: string, to: string): Promise<RenameOutcome>;
    /** Open the project's configuration (dialogue.toml) — the host activates the Config tab. */
    openConfig(): void;
    /** Confirm a branching choice, e.g. opening an existing file instead of creating it. */
    confirm(message: string): boolean;
}

/** A folder row and the handle needed to expand it and reach its child list, used when revealing. */
interface FolderHandle {
    readonly item: HTMLLIElement;
    /** Expand or collapse the folder, loading and returning its child folders on first expand. */
    expand(expanded: boolean): Promise<Map<string, FolderHandle>>;
}

/**
 * Build the Explorer tree of {@link project} into {@link container} and wire it to {@link ports}.
 * The root loads immediately; each folder loads on first expand. The active script is highlighted
 * and its ancestor folders are opened so it is visible on load.
 */
export function initExplorer(
    container: HTMLElement,
    project: ReportProject,
    ports: ExplorerPorts,
    config?: ExplorerConfig,
): void {
    container.replaceChildren();

    const tree = element("ul", "explorer-tree");
    tree.setAttribute("role", "tree");
    const createError = withText("p", "explorer-create-error", "");
    createError.setAttribute("role", "alert");
    createError.hidden = true;

    const showError = (message: string): void => {
        createError.textContent = message;
        createError.hidden = message === "";
    };

    // Populate one folder's list with its sub-folders then its scripts. Returns the folder
    // handles by root-relative path, so a caller can walk them to reveal the active script.
    const populate = async (
        list: HTMLElement,
        path: string,
    ): Promise<Map<string, FolderHandle>> => {
        const listing = await ports.browse(path);
        const folders = new Map<string, FolderHandle>();
        if (listing === null) return folders;

        list.replaceChildren();
        for (const dir of listing.directories) {
            const handle = folderNode(dir);
            folders.set(dir, handle);
            list.append(handle.item);
        }
        for (const source of listing.sources) {
            list.append(scriptNode(source));
        }
        if (listing.directories.length === 0 && listing.sources.length === 0 && path === "") {
            list.append(withText("li", "explorer-empty", "No scripts or folders here."));
        }
        return folders;
    };

    const folderNode = (path: string): FolderHandle => {
        const item = element("li", "explorer-folder") as HTMLLIElement;
        item.setAttribute("role", "treeitem");
        item.dataset.path = path;
        const row = treeRow("explorer-folder-row", "folder", leafName(path), true);
        row.setAttribute("aria-expanded", "false");
        const children = element("ul", "explorer-children");
        children.hidden = true;
        item.append(row, children);

        let loaded = false;
        let childFolders = new Map<string, FolderHandle>();
        const expand = async (expanded: boolean): Promise<Map<string, FolderHandle>> => {
            item.classList.toggle("expanded", expanded);
            row.setAttribute("aria-expanded", String(expanded));
            children.hidden = !expanded;
            if (expanded && !loaded) {
                loaded = true;
                childFolders = await populate(children, path);
            }
            return childFolders;
        };
        row.addEventListener("click", () => void expand(!item.classList.contains("expanded")));
        row.addEventListener("contextmenu", (event) =>
            folderMenu(event, path, children, expand, row),
        );
        return { item, expand };
    };

    const scriptNode = (path: string): HTMLLIElement => {
        const item = element("li", "explorer-script") as HTMLLIElement;
        item.setAttribute("role", "treeitem");
        const row = treeRow("explorer-script-row", "markdown", leafName(path), false);
        if (path === project.activePath) {
            item.classList.add("active");
            row.setAttribute("aria-current", "true");
        }
        row.addEventListener("click", () => ports.openScript(path));
        row.addEventListener("contextmenu", (event) =>
            openContextMenu(event, [
                {
                    icon: "edit",
                    label: "Rename",
                    run: () => {
                        startInlineRename(path, row, "file");
                    },
                },
            ]),
        );
        item.append(row);
        return item;
    };

    // Render the root and open exactly the folders in `open` (a folder's parent opens first, so its
    // children load before it). Shared by the initial reveal, refresh, and collapse.
    const rebuild = async (open: Set<string>): Promise<void> => {
        const handles = new Map<string, FolderHandle>();
        for (const [path, handle] of await populate(tree, "")) handles.set(path, handle);
        const shallowFirst = [...open].sort((a, b) => a.split("/").length - b.split("/").length);
        for (const path of shallowFirst) {
            const handle = handles.get(path);
            if (handle === undefined) continue;
            for (const [child, childHandle] of await handle.expand(true)) {
                handles.set(child, childHandle);
            }
        }
    };

    // The root-relative paths of the folders currently open in the tree.
    const openFolders = (): Set<string> =>
        new Set(
            [...tree.querySelectorAll<HTMLElement>(".explorer-folder.expanded")]
                .map((el) => el.dataset.path)
                .filter((path): path is string => path !== undefined),
        );

    // The initial view: open the ancestors of the active script so it is visible and highlighted.
    const reveal = (): void => {
        void rebuild(new Set(ancestorFolders(project.activePath)));
    };

    // Re-read the tree from disk, keeping the folders the reader had open (new files appear).
    const refresh = (): void => {
        void rebuild(openFolders());
    };

    // Collapse every folder.
    const collapseAll = (): void => {
        void rebuild(new Set());
    };

    // Create a script under {@link parent} ("" is the project root), reusing the launcher's rules:
    // append the extension, open the new file, or — when the name is taken — offer to open the
    // existing one.
    const submitCreate = async (name: string, parent: string): Promise<void> => {
        const typed = name.trim();
        if (typed === "") return;
        const fileName = typed.endsWith(SCRIPT_EXTENSION) ? typed : `${typed}${SCRIPT_EXTENSION}`;
        const outcome = await ports.create(joinPath(parent, fileName));
        if (outcome.kind === "exists") {
            if (ports.confirm(`A file named ${fileName} already exists. Open it instead?`)) {
                ports.openScript(outcome.path);
            }
        } else if (outcome.kind === "error" && outcome.message !== "") {
            showError(outcome.message);
        }
        // "opened": the host navigates to the new file's report.
    };

    // Create a folder under {@link parent} ("" is the project root), then refresh so it appears.
    const submitCreateFolder = async (name: string, parent: string): Promise<void> => {
        const typed = name.trim();
        if (typed === "") return;
        const outcome = await ports.createFolder(joinPath(parent, typed));
        if (outcome.ok) refresh();
        else if (outcome.message !== "") showError(outcome.message);
    };

    // Drop an inline name row into {@link list} — the tree root, or a folder's child list. Enter
    // creates under {@link parent} ("" is the root), Escape or blur cancels, so naming happens in
    // place, VS Code style.
    const startInlineCreate = (
        kind: "file" | "folder",
        parent: string,
        list: HTMLElement,
    ): void => {
        showError("");
        tree.querySelector(".explorer-input-row")?.remove();
        const input = document.createElement("input");
        input.className = "explorer-create-name";
        input.setAttribute("aria-label", kind === "folder" ? "New folder name" : "New script name");
        const row = element("span", "explorer-row");
        row.append(
            codicon("", "explorer-chevron"),
            codicon(kind === "folder" ? "folder" : "markdown", "explorer-icon"),
            input,
        );
        // A script always ends in .dialogue.md; show it as a fixed suffix so the writer types
        // only the name.
        if (kind === "file") {
            row.append(withText("span", "explorer-create-ext", SCRIPT_EXTENSION));
        }
        const item = element("li", "explorer-input-row");
        item.append(row);
        // Guard against a double teardown: removing the focused input fires blur, whose handler also
        // cancels — without this the second remove throws "node is no longer a child".
        let settled = false;
        const cancel = (): void => {
            if (settled) return;
            settled = true;
            item.remove();
        };
        input.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                const value = input.value;
                cancel();
                if (kind === "folder") void submitCreateFolder(value, parent);
                else void submitCreate(value, parent);
            } else if (event.key === "Escape") {
                event.preventDefault();
                cancel();
            }
        });
        input.addEventListener("blur", cancel);
        list.append(item);
        input.focus();
    };

    // Reveal a folder, then drop the inline create field inside it, so New File / New Folder from a
    // folder's right-click menu land within that folder — the counterpart to the header actions,
    // which target the root.
    const createInFolder = async (
        kind: "file" | "folder",
        folderPath: string,
        list: HTMLElement,
        expand: (expanded: boolean) => Promise<unknown>,
    ): Promise<void> => {
        await expand(true);
        startInlineCreate(kind, folderPath, list);
    };

    // The New File / New Folder / Rename menu for a folder row.
    const folderMenu = (
        event: MouseEvent,
        folderPath: string,
        list: HTMLElement,
        expand: (expanded: boolean) => Promise<unknown>,
        row: HTMLElement,
    ): void => {
        openContextMenu(event, [
            {
                icon: "new-file",
                label: "New File",
                run: () => {
                    void createInFolder("file", folderPath, list, expand);
                },
            },
            {
                icon: "new-folder",
                label: "New Folder",
                run: () => {
                    void createInFolder("folder", folderPath, list, expand);
                },
            },
            {
                icon: "edit",
                label: "Rename",
                run: () => {
                    startInlineRename(folderPath, row, "folder");
                },
            },
        ]);
    };

    // Rename a script or folder in place: swap its row for a name field (a script's extension shown
    // as a fixed suffix). Enter renames — moving the file or folder — Escape or blur cancels.
    const startInlineRename = (path: string, row: HTMLElement, kind: "file" | "folder"): void => {
        showError("");
        const leaf = leafName(path);
        const isScript = kind === "file" && leaf.endsWith(SCRIPT_EXTENSION);
        const base = isScript ? leaf.slice(0, -SCRIPT_EXTENSION.length) : leaf;
        const input = document.createElement("input");
        input.className = "explorer-create-name";
        input.value = base;
        input.setAttribute("aria-label", kind === "folder" ? "Rename folder" : "Rename script");
        const editRow = element("span", "explorer-row");
        editRow.append(
            codicon("", "explorer-chevron"),
            codicon(kind === "folder" ? "folder" : "markdown", "explorer-icon"),
            input,
        );
        if (isScript) editRow.append(withText("span", "explorer-create-ext", SCRIPT_EXTENSION));
        row.replaceWith(editRow);
        let settled = false;
        const restore = (): void => {
            if (settled) return;
            settled = true;
            editRow.replaceWith(row);
        };
        input.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                const value = input.value;
                restore();
                void submitRename(path, value, kind);
            } else if (event.key === "Escape") {
                event.preventDefault();
                restore();
            }
        });
        input.addEventListener("blur", restore);
        input.focus();
        input.select();
    };

    // Rename to a trimmed name in the item's own folder (a script keeps its extension); refresh on
    // success (an active file/folder rename navigates via the host), or surface a clash / error.
    const submitRename = async (
        fromPath: string,
        newBase: string,
        kind: "file" | "folder",
    ): Promise<void> => {
        const typed = newBase.trim();
        const leaf = leafName(fromPath);
        const isScript = kind === "file" && leaf.endsWith(SCRIPT_EXTENSION);
        const currentBase = isScript ? leaf.slice(0, -SCRIPT_EXTENSION.length) : leaf;
        if (typed === "" || typed === currentBase) return;
        const newLeaf =
            kind === "file" && !typed.endsWith(SCRIPT_EXTENSION)
                ? `${typed}${SCRIPT_EXTENSION}`
                : typed;
        const parent = fromPath.includes("/") ? fromPath.slice(0, fromPath.lastIndexOf("/")) : "";
        const outcome = await ports.rename(fromPath, joinPath(parent, newLeaf));
        if (outcome.kind === "renamed") refresh();
        else if (outcome.kind === "exists") showError(`A ${kind} named ${newLeaf} already exists.`);
        else if (outcome.kind === "error" && outcome.message !== "") showError(outcome.message);
    };

    const toolbar = element("div", "explorer-actions");
    toolbar.append(
        actionButton("new-file", "New file", () => startInlineCreate("file", "", tree)),
        actionButton("new-folder", "New folder", () => startInlineCreate("folder", "", tree)),
        actionButton("refresh", "Refresh", refresh),
        actionButton("collapse-all", "Collapse folders", collapseAll),
    );
    const heading = element("header", "explorer-header");
    heading.append(withText("span", "explorer-title", "Explorer"), toolbar);
    const root = withText("p", "explorer-root", project.root);
    root.title = project.root;
    // The project's dialogue.toml is project-level (one per root), so it is a pinned entry above
    // the lazy tree rather than a browsable node; clicking it opens the Config tab.
    let configRow: HTMLButtonElement | null = null;
    if (config) {
        configRow = treeRow("explorer-config-row", "settings-gear", config.label, false);
        configRow.title = `Open ${config.label}`;
        configRow.addEventListener("click", () => ports.openConfig());
    }
    container.append(heading, root, ...(configRow ? [configRow] : []), tree, createError);

    reveal();
}

/**
 * The ancestor folders of a root-relative path, root-first, excluding the file itself:
 * `"act-1/scene/x.dialogue.md"` → `["act-1", "act-1/scene"]`. No active path (the empty state)
 * has no ancestors to reveal.
 */
export function ancestorFolders(activePath: string | undefined): string[] {
    if (activePath === undefined) return [];
    const parts = activePath.split("/").filter(Boolean);
    parts.pop();
    return parts.map((_, index) => parts.slice(0, index + 1).join("/"));
}

/**
 * Resolve a preview link's file part — written relative to the folder of the script that contains
 * it — to a root-relative path the Explorer can open, or `null` when it escapes the project root or
 * names no file. The `#anchor` is dropped: opening the file is the Explorer's job; resolving the
 * anchor is the linker's (deferred).
 */
export function resolveProjectPath(baseFolder: string, link: string): string | null {
    const filePart = link.split("#")[0];
    if (filePart === "") return null; // a bare "#anchor" is same-file, not a cross-file open
    const segments = (baseFolder === "" ? [] : baseFolder.split("/")).concat(filePart.split("/"));
    const stack: string[] = [];
    for (const segment of segments) {
        if (segment === "" || segment === ".") continue;
        if (segment === "..") {
            if (stack.length === 0) return null; // escapes the root
            stack.pop();
        } else {
            stack.push(segment);
        }
    }
    return stack.length === 0 ? null : stack.join("/");
}

function element(tag: string, className: string): HTMLElement {
    const node = document.createElement(tag);
    node.className = className;
    return node;
}

function withText(tag: string, className: string, text: string): HTMLElement {
    const node = element(tag, className);
    node.textContent = text;
    return node;
}

// A tree row: a chevron (folders) or an aligning spacer (scripts), a type icon, and the name.
function treeRow(
    rowClass: string,
    iconName: string,
    label: string,
    isFolder: boolean,
): HTMLButtonElement {
    const row = element("button", `explorer-row ${rowClass}`) as HTMLButtonElement;
    row.type = "button";
    row.append(
        codicon(isFolder ? "chevron-right" : "", "explorer-chevron"),
        codicon(iconName, "explorer-icon"),
        withText("span", "explorer-label", label),
    );
    return row;
}

// A small VS Code-style header action: an icon button with a tooltip.
function actionButton(iconName: string, title: string, onClick: () => void): HTMLButtonElement {
    const button = element("button", "explorer-action") as HTMLButtonElement;
    button.type = "button";
    button.title = title;
    button.setAttribute("aria-label", title);
    button.append(codicon(iconName, "explorer-action-icon"));
    button.addEventListener("click", onClick);
    return button;
}

// Join a root-relative parent folder and a leaf name; the parent is "" for the project root.
function joinPath(parent: string, leaf: string): string {
    return parent === "" ? leaf : `${parent}/${leaf}`;
}
