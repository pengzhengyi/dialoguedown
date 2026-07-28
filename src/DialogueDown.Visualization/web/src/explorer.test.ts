import { describe, it, expect, vi } from "vitest";

import {
    ancestorFolders,
    initExplorer,
    resolveProjectPath,
    type CreateFolderOutcome,
    type ExplorerPorts,
    type RenameOutcome,
} from "./explorer";
import type { BrowseListing, CreateOutcome } from "./launcher";
import type { ReportProject } from "./model";

const rootListing: BrowseListing = {
    path: "",
    parent: null,
    directories: ["act-1"],
    sources: ["intro.dialogue.md"],
};
const act1Listing: BrowseListing = {
    path: "act-1",
    parent: "",
    directories: [],
    sources: ["act-1/prologue.dialogue.md"],
};

function ports(overrides: Partial<ExplorerPorts> = {}): ExplorerPorts {
    return {
        browse: vi.fn(async (path: string) => (path === "act-1" ? act1Listing : rootListing)),
        openScript: vi.fn<(path: string) => void>(),
        create: vi.fn(async () => ({ kind: "opened", url: "http://x/r/new/" }) as CreateOutcome),
        createFolder: vi.fn(async () => ({ ok: true }) as CreateFolderOutcome),
        rename: vi.fn(async () => ({ kind: "renamed", path: "x" }) as RenameOutcome),
        openConfig: vi.fn<() => void>(),
        confirm: vi.fn(() => true),
        ...overrides,
    };
}

const flush = () => new Promise((resolve) => setTimeout(resolve));
const settle = async () => {
    for (let i = 0; i < 5; i++) await flush();
};
const rowTexts = (root: HTMLElement, selector: string) =>
    [...root.querySelectorAll(selector)].map((node) => node.textContent);

/** Click a header action, type a name in the inline field, and submit it with Enter. */
async function createNamed(
    container: HTMLElement,
    name: string,
    action = "New file",
): Promise<void> {
    (container.querySelector(`.explorer-action[aria-label="${action}"]`) as HTMLElement).click();
    const input = container.querySelector(".explorer-create-name") as HTMLInputElement;
    input.value = name;
    input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter" }));
    await settle();
}

/** Right-click a folder row and return its context menu (appended to the document body). */
function openFolderContextMenu(container: HTMLElement, folderText: string): HTMLElement {
    const folderRow = [...container.querySelectorAll(".explorer-folder-row")].find(
        (node) => node.textContent === folderText,
    ) as HTMLElement;
    folderRow.dispatchEvent(
        new MouseEvent("contextmenu", { bubbles: true, clientX: 12, clientY: 12 }),
    );
    return document.querySelector(".explorer-context-menu") as HTMLElement;
}

/** Right-click a folder, choose New File / New Folder, type a name, and submit it with Enter. */
async function createInFolder(
    container: HTMLElement,
    folderText: string,
    action: "New File" | "New Folder",
    name: string,
): Promise<void> {
    const menu = openFolderContextMenu(container, folderText);
    const item = [...menu.querySelectorAll('[role="menuitem"]')].find(
        (node) => node.textContent === action,
    ) as HTMLElement;
    item.click();
    await settle();
    const input = container.querySelector(".explorer-create-name") as HTMLInputElement;
    input.value = name;
    input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter" }));
    await settle();
}

describe("ancestorFolders", () => {
    it("lists ancestor folders root-first, excluding the file", () => {
        expect(ancestorFolders("act-1/scene/x.dialogue.md")).toEqual(["act-1", "act-1/scene"]);
        expect(ancestorFolders("intro.dialogue.md")).toEqual([]);
        expect(ancestorFolders(undefined)).toEqual([]);
    });
});

describe("resolveProjectPath", () => {
    it("resolves a sibling link relative to the script's folder", () => {
        expect(resolveProjectPath("act-1", "chapter-02.dialogue.md#meet-bob")).toBe(
            "act-1/chapter-02.dialogue.md",
        );
    });

    it("resolves a parent traversal", () => {
        expect(resolveProjectPath("act-1/scenes", "../intro.dialogue.md")).toBe(
            "act-1/intro.dialogue.md",
        );
    });

    it("returns null when the link escapes the root", () => {
        expect(resolveProjectPath("act-1", "../../outside.dialogue.md")).toBeNull();
    });

    it("returns null for a bare same-file anchor", () => {
        expect(resolveProjectPath("act-1", "#crossroads")).toBeNull();
    });
});

describe("initExplorer", () => {
    const revealing: ReportProject = { root: "/project", activePath: "act-1/prologue.dialogue.md" };
    const atRoot: ReportProject = { root: "/project", activePath: "intro.dialogue.md" };

    it("renders the root's folders and scripts under the root label", async () => {
        const container = document.createElement("aside");
        initExplorer(container, atRoot, ports());
        await settle();

        expect(rowTexts(container, ".explorer-folder-row")).toContain("act-1");
        expect(rowTexts(container, ".explorer-script-row")).toContain("intro.dialogue.md");
        expect(container.querySelector(".explorer-root")?.textContent).toBe("/project");
    });

    it("reveals and highlights the active script inside a nested folder", async () => {
        const container = document.createElement("aside");
        initExplorer(container, revealing, ports());
        await settle();

        const active = container.querySelector(".explorer-script.active .explorer-script-row");
        expect(active?.textContent).toBe("prologue.dialogue.md");
    });

    it("loads a folder's children lazily on expand", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts);
        await settle();

        // act-1 starts collapsed (the active script is at the root), so its child is not loaded.
        expect(rowTexts(container, ".explorer-script-row")).not.toContain("prologue.dialogue.md");

        (container.querySelector(".explorer-folder-row") as HTMLElement).click();
        await settle();

        expect(rowTexts(container, ".explorer-script-row")).toContain("prologue.dialogue.md");
    });

    it("opens a script on click", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts);
        await settle();

        const introRow = [...container.querySelectorAll(".explorer-script-row")].find(
            (node) => node.textContent === "intro.dialogue.md",
        ) as HTMLElement;
        introRow.click();

        expect(explorerPorts.openScript).toHaveBeenCalledWith("intro.dialogue.md");
    });

    it("keeps the open folders open across a refresh", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts); // act-1 starts collapsed (active at root)
        await settle();

        (container.querySelector(".explorer-folder-row") as HTMLElement).click();
        await settle();
        expect(rowTexts(container, ".explorer-script-row")).toContain("prologue.dialogue.md");

        (container.querySelector('.explorer-action[aria-label="Refresh"]') as HTMLElement).click();
        await settle();

        // act-1 is still open (refresh preserves expansion), so its child is still shown.
        expect(rowTexts(container, ".explorer-script-row")).toContain("prologue.dialogue.md");
    });

    it("creates a script at the project root", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, revealing, explorerPorts);
        await settle();

        await createNamed(container, "villain");

        expect(explorerPorts.create).toHaveBeenCalledWith("villain.dialogue.md");
    });

    it("offers to open an existing file when the name is taken", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports({
            create: vi.fn(
                async () => ({ kind: "exists", path: "villain.dialogue.md" }) as CreateOutcome,
            ),
        });
        initExplorer(container, revealing, explorerPorts);
        await settle();

        await createNamed(container, "villain.dialogue.md");

        expect(explorerPorts.openScript).toHaveBeenCalledWith("villain.dialogue.md");
    });

    it("creates a folder from the header action", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, revealing, explorerPorts);
        await settle();

        await createNamed(container, "act-2", "New folder");

        expect(explorerPorts.createFolder).toHaveBeenCalledWith("act-2");
    });

    it("opens a folder context menu with New File and New Folder", async () => {
        const container = document.createElement("aside");
        initExplorer(container, atRoot, ports());
        await settle();

        const menu = openFolderContextMenu(container, "act-1");
        expect(rowTexts(menu, '[role="menuitem"]')).toEqual(["New File", "New Folder", "Rename"]);

        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    });

    it("creates a folder inside the right-clicked folder", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts);
        await settle();

        await createInFolder(container, "act-1", "New Folder", "scenes");

        expect(explorerPorts.createFolder).toHaveBeenCalledWith("act-1/scenes");
    });

    it("creates a script inside the right-clicked folder", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts);
        await settle();

        await createInFolder(container, "act-1", "New File", "villain");

        expect(explorerPorts.create).toHaveBeenCalledWith("act-1/villain.dialogue.md");
    });

    it("dismisses the folder context menu on Escape", async () => {
        const container = document.createElement("aside");
        initExplorer(container, atRoot, ports());
        await settle();

        openFolderContextMenu(container, "act-1");
        expect(document.querySelector(".explorer-context-menu")).not.toBeNull();

        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
        expect(document.querySelector(".explorer-context-menu")).toBeNull();
    });

    it("renames a script from its context menu", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts);
        await settle();

        const scriptRow = [...container.querySelectorAll(".explorer-script-row")].find(
            (node) => node.textContent === "intro.dialogue.md",
        ) as HTMLElement;
        scriptRow.dispatchEvent(
            new MouseEvent("contextmenu", { bubbles: true, clientX: 8, clientY: 8 }),
        );
        const menu = document.querySelector(".explorer-context-menu") as HTMLElement;
        const rename = [...menu.querySelectorAll('[role="menuitem"]')].find(
            (node) => node.textContent === "Rename",
        ) as HTMLElement;
        rename.click();
        await settle();

        const input = container.querySelector(".explorer-create-name") as HTMLInputElement;
        expect(input.value).toBe("intro"); // the extension is a fixed suffix, not editable
        input.value = "introduction";
        input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter" }));
        await settle();

        expect(explorerPorts.rename).toHaveBeenCalledWith(
            "intro.dialogue.md",
            "introduction.dialogue.md",
        );
    });

    it("renames a folder from its context menu", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts);
        await settle();

        const menu = openFolderContextMenu(container, "act-1");
        const rename = [...menu.querySelectorAll('[role="menuitem"]')].find(
            (node) => node.textContent === "Rename",
        ) as HTMLElement;
        rename.click();
        await settle();

        const input = container.querySelector(".explorer-create-name") as HTMLInputElement;
        expect(input.value).toBe("act-1"); // folders edit the whole name (no extension suffix)
        input.value = "act-two";
        input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter" }));
        await settle();

        expect(explorerPorts.rename).toHaveBeenCalledWith("act-1", "act-two");
    });

    it("shows the configuration entry and opens it on click", async () => {
        const container = document.createElement("aside");
        const explorerPorts = ports();
        initExplorer(container, atRoot, explorerPorts, { label: "dialogue.toml" });
        await settle();

        const configRow = container.querySelector(".explorer-config-row");
        expect(configRow?.textContent).toBe("dialogue.toml");

        (configRow as HTMLElement).click();
        expect(explorerPorts.openConfig).toHaveBeenCalledTimes(1);
    });

    it("omits the configuration entry when the project has no config file", async () => {
        const container = document.createElement("aside");
        initExplorer(container, atRoot, ports());
        await settle();

        expect(container.querySelector(".explorer-config-row")).toBeNull();
    });
});
