/**
 * The Explorer client: a lazy, expand/collapse tree of the project root's folders and
 * `.dialogue.md` scripts, with the active script highlighted and revealed. A folder loads its
 * children the first time it is expanded (one `GET /api/browse` per folder). The DOM building
 * lives here (unit-tested with jsdom); the browser wiring — `fetch`, and save-safe navigation —
 * is injected through {@link ExplorerPorts}, mirroring the launcher.
 */

import { leafName, type BrowseListing } from "./launcher";
import type { ReportProject } from "./model";

/** The side-effecting collaborators, injected so the tree is testable without a server. */
export interface ExplorerPorts {
    /** One folder's children (`GET /api/browse`); `null` when it escapes the root or is missing. */
    browse(path: string): Promise<BrowseListing | null>;
    /** Open a script by its root-relative path — save-safe navigation, wired by the host. */
    openScript(path: string): void;
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

    const heading = withText("header", "explorer-title", "Explorer");
    const root = withText("p", "explorer-root", project.root);
    root.title = project.root;
    const tree = element("ul", "explorer-tree");
    tree.setAttribute("role", "tree");
    container.append(heading, root, tree);

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
