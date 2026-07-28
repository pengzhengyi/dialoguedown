/**
 * The Explorer client: a lazy, expand/collapse tree of the project root's folders and
 * `.dialogue.md` scripts, with the active script highlighted and revealed. A folder loads its
 * children the first time it is expanded (one `GET /api/browse` per folder). The DOM building
 * lives here (unit-tested with jsdom); the browser wiring — `fetch`, and save-safe navigation —
 * is injected through {@link ExplorerPorts}, mirroring the launcher.
 */

import {
    leafName,
    parentPath,
    SCRIPT_EXTENSION,
    type BrowseListing,
    type CreateOutcome,
} from "./launcher";
import type { ReportProject } from "./model";

/** The side-effecting collaborators, injected so the tree is testable without a server. */
export interface ExplorerPorts {
    /** One folder's children (`GET /api/browse`); `null` when it escapes the root or is missing. */
    browse(path: string): Promise<BrowseListing | null>;
    /** Open a script by its root-relative path — save-safe navigation, wired by the host. */
    openScript(path: string): void;
    /** Create a script at a root-relative path — save-safe; the host navigates on success. */
    create(path: string): Promise<CreateOutcome>;
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
): void {
    container.replaceChildren();

    const activeFolder = parentPath(project.activePath);
    const createError = withText("p", "explorer-create-error", "");
    createError.setAttribute("role", "alert");
    createError.hidden = true;

    const showError = (message: string): void => {
        createError.textContent = message;
        createError.hidden = message === "";
    };

    // Create a script in the active script's folder, reusing the launcher's rules: append the
    // extension, open the new file, or — when the name is taken — offer to open the existing one.
    const submitCreate = async (name: string): Promise<void> => {
        const typed = name.trim();
        if (typed === "") {
            showError("Enter a name.");
            return;
        }
        const fileName = typed.endsWith(SCRIPT_EXTENSION) ? typed : `${typed}${SCRIPT_EXTENSION}`;
        const path = activeFolder === "" ? fileName : `${activeFolder}/${fileName}`;
        const outcome = await ports.create(path);
        if (outcome.kind === "exists") {
            if (ports.confirm(`A file named ${fileName} already exists. Open it instead?`)) {
                ports.openScript(outcome.path);
            }
        } else if (outcome.kind === "error" && outcome.message !== "") {
            showError(outcome.message);
        }
        // "opened": the host navigates to the new file's report.
    };

    // The New file trigger turns into an inline name field; Enter creates, Escape cancels.
    const startCreate = (trigger: HTMLElement): void => {
        showError("");
        const input = document.createElement("input");
        input.type = "text";
        input.className = "explorer-create-name";
        input.placeholder = "new-script";
        input.setAttribute("aria-label", "New script name");
        const restore = (): void => input.replaceWith(trigger);
        input.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                void submitCreate(input.value).then(restore);
            } else if (event.key === "Escape") {
                event.preventDefault();
                restore();
            }
        });
        trigger.replaceWith(input);
        input.focus();
    };

    const newFile = withText("button", "explorer-new", "New file") as HTMLButtonElement;
    newFile.type = "button";
    newFile.addEventListener("click", () => startCreate(newFile));

    const heading = element("header", "explorer-header");
    heading.append(withText("span", "explorer-title", "Explorer"), newFile);
    const root = withText("p", "explorer-root", project.root);
    root.title = project.root;
    const tree = element("ul", "explorer-tree");
    tree.setAttribute("role", "tree");
    container.append(heading, root, tree, createError);

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
        const row = withText("button", "explorer-row explorer-folder-row", leafName(path));
        (row as HTMLButtonElement).type = "button";
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
        return { item, expand };
    };

    const scriptNode = (path: string): HTMLLIElement => {
        const item = element("li", "explorer-script") as HTMLLIElement;
        item.setAttribute("role", "treeitem");
        const row = withText("button", "explorer-row explorer-script-row", leafName(path));
        (row as HTMLButtonElement).type = "button";
        if (path === project.activePath) {
            item.classList.add("active");
            row.setAttribute("aria-current", "true");
        }
        row.addEventListener("click", () => ports.openScript(path));
        item.append(row);
        return item;
    };

    // Load the root, then open each ancestor folder of the active script in turn so the script
    // is on screen and highlighted from the start.
    const reveal = async (): Promise<void> => {
        let folders = await populate(tree, "");
        for (const folder of ancestorFolders(project.activePath)) {
            const handle = folders.get(folder);
            if (handle === undefined) break;
            folders = await handle.expand(true);
        }
    };

    void reveal();
}

/**
 * The ancestor folders of a root-relative path, root-first, excluding the file itself:
 * `"act-1/scene/x.dialogue.md"` → `["act-1", "act-1/scene"]`.
 */
export function ancestorFolders(activePath: string): string[] {
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
