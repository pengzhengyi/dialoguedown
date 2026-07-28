import { describe, it, expect, vi } from "vitest";

import { ancestorFolders, initExplorer, type ExplorerPorts } from "./explorer";
import type { BrowseListing } from "./launcher";
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
        ...overrides,
    };
}

const flush = () => new Promise((resolve) => setTimeout(resolve));
const settle = async () => {
    for (let i = 0; i < 5; i++) await flush();
};
const rowTexts = (root: HTMLElement, selector: string) =>
    [...root.querySelectorAll(selector)].map((node) => node.textContent);

describe("ancestorFolders", () => {
    it("lists ancestor folders root-first, excluding the file", () => {
        expect(ancestorFolders("act-1/scene/x.dialogue.md")).toEqual(["act-1", "act-1/scene"]);
        expect(ancestorFolders("intro.dialogue.md")).toEqual([]);
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
});
