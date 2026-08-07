import tippy from "tippy.js";
import type { ConfigReport } from "./model";
import { showToast } from "./toast";

/** Split a path into its directory (head) and last segment (tail, with separator). */
export function splitPath(path: string): { head: string; tail: string } {
    const index = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
    if (index < 0) return { head: "", tail: path };
    return { head: path.slice(0, index), tail: path.slice(index) };
}

/** Copy text to the clipboard, falling back to a hidden textarea where the Clipboard API is unavailable. */
export async function copyToClipboard(text: string): Promise<void> {
    if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(text);
        return;
    }
    const area = document.createElement("textarea");
    area.value = text;
    area.style.position = "fixed";
    area.style.opacity = "0";
    document.body.appendChild(area);
    area.select();
    document.execCommand("copy");
    area.remove();
}

/**
 * Show the document path in the status bar: the filename always shows while the
 * middle of the directory is ellipsised (CSS), the full path is a hover tooltip,
 * and clicking copies the path. Hidden when there is no path (e.g. a library
 * render with no file). The element defaults to the dialogue document's path chip;
 * pass another id to reuse it (the config path).
 */
export function initPathDisplay(
    path: string | undefined,
    elementId = "doc-path",
): HTMLElement | null {
    const button = document.getElementById(elementId) as HTMLButtonElement | null;
    if (!button) return null;
    if (!path) {
        button.hidden = true;
        return button;
    }

    const { head, tail } = splitPath(path);
    button.querySelector(".path-head")!.textContent = head;
    button.querySelector(".path-tail")!.textContent = tail;
    button.hidden = false;
    button.disabled = false;

    // The hover tooltip shows the full path (the button ellipsises the directory); a click
    // copies it and confirms through the shared toast, the same cue the config table uses.
    tippy(button, { content: `${path}\n(click to copy)`, maxWidth: 480 });
    button.addEventListener("click", () => {
        void copyToClipboard(path).then(() => showToast(`Copied ${path}`));
    });
    return button;
}

/**
 * Show the config file's path in the status bar beside the document path. When the compile
 * found no `dialogue.toml` it shows a plain "No config file" label (not a broken path);
 * hidden entirely when the report has no configuration context.
 */
export function initConfigPath(config: ConfigReport | undefined): HTMLElement | null {
    const button = document.getElementById("config-path") as HTMLButtonElement | null;
    if (!button) return null;
    if (!config) {
        button.hidden = true;
        return button;
    }
    if (config.file) {
        return initPathDisplay(config.file.path, "config-path");
    }

    // The no-config state: a plain label, nothing to copy.
    button.querySelector(".path-head")!.textContent = "";
    button.querySelector(".path-tail")!.textContent = "No config file";
    button.classList.add("config-missing");
    button.hidden = false;
    button.disabled = true;
    tippy(button, { content: "No dialogue.toml — using the built-in defaults.", maxWidth: 320 });
    return button;
}
