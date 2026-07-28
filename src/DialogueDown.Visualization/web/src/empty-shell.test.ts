import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

import { initEmptyShell } from "./empty-shell";
import type { Report } from "./model";
import type { BrowseListing } from "./project-fs";

function mountDom(): void {
    document.body.innerHTML = `
        <nav id="tabs"></nav>
        <div id="live-banner" hidden></div>
        <main id="app">
            <aside id="explorer"></aside>
            <div id="explorer-resizer"></div>
            <section id="stages"></section>
            <div id="resizer"></div>
            <aside id="detail"><header id="detail-title"></header><div id="detail-body"></div></aside>
        </main>
        <footer>
            <span id="mode-badge"></span>
            <span id="help-summary"></span>
            <button id="help-toggle" aria-expanded="false" aria-controls="help-content"></button>
            <div id="help-content" hidden></div>
        </footer>`;
}

const rootListing: BrowseListing = {
    path: "",
    parent: null,
    directories: ["act-1"],
    sources: ["intro.dialogue.md"],
};

const flush = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));
const settle = async (): Promise<void> => {
    for (let i = 0; i < 5; i++) await flush();
};

const emptyReport: Report = { mode: "edit", stages: [], project: { root: "/project" } };

describe("initEmptyShell", () => {
    beforeEach(() => {
        mountDom();
        vi.stubGlobal(
            "fetch",
            vi.fn(async () => ({ ok: true, json: async () => rootListing }) as unknown as Response),
        );
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it("mounts the Explorer over the project root", async () => {
        initEmptyShell(emptyReport);
        await settle();

        expect(document.querySelector(".explorer-root")?.textContent).toBe("/project");
        expect(document.querySelector(".explorer-script-row")?.textContent).toBe(
            "intro.dialogue.md",
        );
    });

    it("shows the empty-state call to action in the main pane", () => {
        initEmptyShell(emptyReport);

        expect(document.querySelector(".empty-shell-title")?.textContent).toBe("No script open");
        expect(document.querySelector(".empty-shell-create")).not.toBeNull();
    });

    it("the create button runs the Explorer's new-file flow", async () => {
        initEmptyShell(emptyReport);
        await settle();

        (document.querySelector(".empty-shell-create") as HTMLButtonElement).click();

        expect(document.querySelector(".explorer-create-name")).not.toBeNull();
    });
});
