import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { splitPath, copyToClipboard, initPathDisplay, initConfigPath } from "./path-display";

describe("splitPath", () => {
    it("splits a POSIX path into directory and filename", () => {
        expect(splitPath("/home/alice/scene.dialogue.md")).toEqual({
            head: "/home/alice",
            tail: "/scene.dialogue.md",
        });
    });

    it("splits a Windows path", () => {
        expect(splitPath("C:\\docs\\scene.dialogue.md")).toEqual({
            head: "C:\\docs",
            tail: "\\scene.dialogue.md",
        });
    });

    it("returns the whole string as the tail when there is no separator", () => {
        expect(splitPath("scene.dialogue.md")).toEqual({ head: "", tail: "scene.dialogue.md" });
    });
});

describe("copyToClipboard", () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("uses the Clipboard API when available", async () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal("navigator", { clipboard: { writeText } });

        await copyToClipboard("/path/x.md");

        expect(writeText).toHaveBeenCalledWith("/path/x.md");
    });

    it("falls back to execCommand when the Clipboard API is missing", async () => {
        vi.stubGlobal("navigator", {});
        const exec = vi.fn();
        (document as unknown as { execCommand: unknown }).execCommand = exec;

        await copyToClipboard("/path/x.md");

        expect(exec).toHaveBeenCalledWith("copy");
    });
});

describe("initPathDisplay", () => {
    beforeEach(() => {
        document.body.innerHTML = `
      <button id="doc-path" hidden>
        <span class="path-head"></span><span class="path-tail"></span>
      </button>`;
    });

    it("fills the head and tail and reveals the button", () => {
        const button = initPathDisplay("/home/alice/scene.dialogue.md")!.element;
        expect(button.hidden).toBe(false);
        expect(button.querySelector(".path-head")!.textContent).toBe("/home/alice");
        expect(button.querySelector(".path-tail")!.textContent).toBe("/scene.dialogue.md");
    });

    it("stays hidden when there is no path", () => {
        const button = initPathDisplay(undefined)!.element;
        expect(button.hidden).toBe(true);
    });

    it("returns null when the path element is absent", () => {
        document.body.innerHTML = "";
        expect(initPathDisplay("/x.md")).toBeNull();
    });

    it("copies the full path and confirms with a toast when clicked", async () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal("navigator", { clipboard: { writeText } });

        const button = initPathDisplay("/home/alice/scene.dialogue.md")!.element;
        button.click();
        await new Promise((resolve) => setTimeout(resolve));

        expect(writeText).toHaveBeenCalledWith("/home/alice/scene.dialogue.md");
        expect(document.querySelector(".toast.visible")?.textContent).toBe(
            "Copied /home/alice/scene.dialogue.md",
        );
        vi.restoreAllMocks();
    });

    it("re-points at another path, copying the new one", async () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal("navigator", { clipboard: { writeText } });
        const display = initPathDisplay("/home/alice/scene.dialogue.md")!;

        display.setPath("/home/alice/act-1/finale.dialogue.md");

        expect(display.element.querySelector(".path-head")!.textContent).toBe("/home/alice/act-1");
        expect(display.element.querySelector(".path-tail")!.textContent).toBe(
            "/finale.dialogue.md",
        );
        display.element.click();
        await new Promise((resolve) => setTimeout(resolve));
        // Re-pointing replaces the copy target rather than stacking a second handler on the old one.
        expect(writeText).toHaveBeenCalledTimes(1);
        expect(writeText).toHaveBeenCalledWith("/home/alice/act-1/finale.dialogue.md");
        vi.restoreAllMocks();
    });

    it("hides the chip when re-pointed at no path", () => {
        const display = initPathDisplay("/home/alice/scene.dialogue.md")!;

        display.setPath(undefined);

        expect(display.element.hidden).toBe(true);
    });
});

describe("initConfigPath", () => {
    beforeEach(() => {
        document.body.innerHTML = `
      <button id="config-path" hidden>
        <span class="path-head"></span><span class="path-tail"></span>
      </button>`;
    });

    it("shows the config file's path when one was applied", () => {
        const button = initConfigPath({
            file: { path: "/proj/dialogue.toml", source: "" },
            speakers: [],
        })!.element as HTMLButtonElement;
        expect(button.hidden).toBe(false);
        expect(button.disabled).toBe(false);
        expect(button.querySelector(".path-tail")!.textContent).toBe("/dialogue.toml");
    });

    it("shows a plain no-config label when there is no file", () => {
        const button = initConfigPath({ speakers: [] })!.element as HTMLButtonElement;
        expect(button.hidden).toBe(false);
        expect(button.disabled).toBe(true);
        expect(button.querySelector(".path-tail")!.textContent).toBe("No config file");
    });

    it("stays hidden when there is no configuration context", () => {
        const button = initConfigPath(undefined)!.element;
        expect(button.hidden).toBe(true);
    });
});
