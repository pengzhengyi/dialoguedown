import { codicon } from "./codicon";

const STORAGE_KEY = "dd-ignored-preview-collapsed";

export interface IgnoredPreviewController {
    /** The fixed footer mounted below the scrollable Preview document. */
    readonly footer: HTMLElement;
    /** Recount regions after the Preview document is rendered again. */
    refresh(): void;
    /** Release the toggle listener. */
    destroy(): void;
}

/**
 * Manage one global expanded/collapsed preference for every ignored region in a Preview.
 * Source remains untouched: this controller only changes the rendered Markdown pane.
 */
export function createIgnoredPreviewController(
    preview: HTMLElement,
    storage: Storage | undefined = localStorageOrUndefined(),
): IgnoredPreviewController {
    const footer = document.createElement("div");
    footer.className = "dd-ignored-preview-footer";
    footer.setAttribute("role", "region");
    footer.setAttribute("aria-label", "Ignored Preview content");

    const marker = codicon("eye-closed", "dd-ignored-preview-footer-marker");
    const count = document.createElement("strong");
    count.className = "dd-ignored-preview-count";
    const state = document.createElement("span");
    state.className = "dd-ignored-preview-state";
    const toggle = document.createElement("button");
    toggle.type = "button";
    toggle.className = "dd-ignored-preview-toggle";
    footer.append(marker, count, state, toggle);

    let collapsed = readCollapsed(storage);
    let regionCount = 0;

    const render = (): void => {
        preview.classList.toggle("ignored-preview-collapsed", collapsed && regionCount > 0);
        for (const region of preview.querySelectorAll<HTMLElement>(".dd-preview-ignored-region")) {
            if (collapsed) {
                region.setAttribute(
                    "aria-label",
                    `Ignored ${region.dataset.ignoredSummary ?? "Markdown"}`,
                );
            } else {
                region.removeAttribute("aria-label");
            }
        }
        count.textContent = `${regionCount} ignored`;
        state.textContent =
            regionCount === 0
                ? "nothing omitted"
                : collapsed
                  ? "hidden in Preview"
                  : "shown in Preview";
        toggle.disabled = regionCount === 0;
        toggle.setAttribute("aria-expanded", String(!collapsed));
        toggle.setAttribute(
            "aria-label",
            regionCount === 0
                ? "No ignored content in Preview"
                : collapsed
                  ? "Show all ignored content in Preview"
                  : "Hide all ignored content in Preview",
        );
        toggle.title = toggle.getAttribute("aria-label")!;
        toggle.replaceChildren(
            codicon(collapsed ? "eye" : "eye-closed", "dd-ignored-preview-toggle-icon"),
        );
    };

    const onToggle = (): void => {
        collapsed = !collapsed;
        rememberCollapsed(storage, collapsed);
        render();
    };
    toggle.addEventListener("click", onToggle);

    const refresh = (): void => {
        regionCount = preview.querySelectorAll(".dd-preview-ignored-region").length;
        render();
    };
    refresh();

    return {
        footer,
        refresh,
        destroy: () => toggle.removeEventListener("click", onToggle),
    };
}

function readCollapsed(storage: Storage | undefined): boolean {
    try {
        return storage?.getItem(STORAGE_KEY) === "1";
    } catch {
        return false;
    }
}

function rememberCollapsed(storage: Storage | undefined, collapsed: boolean): void {
    try {
        if (collapsed) storage?.setItem(STORAGE_KEY, "1");
        else storage?.removeItem(STORAGE_KEY);
    } catch {
        // A sandboxed file report can deny storage; the current view still toggles.
    }
}

function localStorageOrUndefined(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
