import { codicon } from "./codicon";

const STORAGE_KEY = "dd-ignored-preview-collapsed";
const HIDDEN_CLASS = "dd-ignored-region-hidden";

export interface IgnoredPreviewController {
    /** The fixed footer mounted below the scrollable Preview document. */
    readonly footer: HTMLElement;
    /** Reapply the current view after the Preview document is rendered again. */
    refresh(): void;
    /** Release the footer and region listeners. */
    destroy(): void;
}

/**
 * Decide how much ignored content a Preview shows.
 *
 * A persisted **baseline** says how the writer wants to read the whole project, and session-only
 * **overrides** let single regions differ from it while they are being worked on. The two footer
 * commands are commands rather than toggles: each sets the baseline *and* discards every override,
 * so however scattered the view becomes, one click returns it to a state the writer can name.
 *
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

    const marker = codicon("circle-slash", "dd-ignored-preview-footer-marker");
    const count = document.createElement("strong");
    count.className = "dd-ignored-preview-count";
    const state = document.createElement("span");
    state.className = "dd-ignored-preview-state";
    const expandAll = commandButton("expand", "expand-all", "Show all ignored content in Preview");
    const collapseAll = commandButton(
        "collapse",
        "collapse-all",
        "Hide all ignored content in Preview",
    );
    footer.append(marker, count, state, expandAll, collapseAll);

    let baselineShown = !readCollapsed(storage);
    /** Regions the writer chose to view differently from the baseline, by region key. */
    const overrides = new Map<string, boolean>();

    const regions = (): HTMLElement[] => [
        ...preview.querySelectorAll<HTMLElement>(".dd-preview-ignored-region"),
    ];

    const isShown = (region: HTMLElement): boolean =>
        overrides.get(region.dataset.ignoredKey ?? "") ?? baselineShown;

    const render = (): void => {
        const all = regions();
        let shownCount = 0;
        for (const region of all) {
            const shown = isShown(region);
            if (shown) shownCount += 1;
            region.classList.toggle(HIDDEN_CLASS, !shown);
            const control = region.querySelector<HTMLElement>(".dd-ignored-region-toggle");
            if (!control) continue;
            const summary = region.dataset.ignoredSummary ?? "Markdown";
            const label = `${shown ? "Hide" : "Show"} ignored ${summary}`;
            control.setAttribute("aria-expanded", String(shown));
            control.setAttribute("aria-label", label);
            control.title = label;
        }

        count.textContent = `${all.length} ignored`;
        state.textContent = viewSummary(all.length, shownCount);
        expandAll.disabled = all.length === 0;
        collapseAll.disabled = all.length === 0;
    };

    /** A global command: adopt one baseline for the whole Preview and drop every exception. */
    const applyToAll = (shown: boolean) => (): void => {
        baselineShown = shown;
        overrides.clear();
        rememberCollapsed(storage, !shown);
        render();
    };
    const onExpandAll = applyToAll(true);
    const onCollapseAll = applyToAll(false);
    expandAll.addEventListener("click", onExpandAll);
    collapseAll.addEventListener("click", onCollapseAll);

    // Delegated once on the stable Preview element, because every keystroke replaces the rendered
    // document and with it every region control.
    const onRegionClick = (event: MouseEvent): void => {
        const control = (event.target as Element | null)?.closest(".dd-ignored-region-toggle");
        const region = control?.closest<HTMLElement>(".dd-preview-ignored-region");
        const key = region?.dataset.ignoredKey;
        if (!region || !key) return;
        overrides.set(key, !isShown(region));
        render();
    };
    preview.addEventListener("click", onRegionClick);

    render();

    return {
        footer,
        refresh: render,
        destroy: () => {
            expandAll.removeEventListener("click", onExpandAll);
            collapseAll.removeEventListener("click", onCollapseAll);
            preview.removeEventListener("click", onRegionClick);
        },
    };
}

/** State the view exactly, so a mixed Preview never reads as if it were showing everything. */
function viewSummary(total: number, shown: number): string {
    if (total === 0) return "nothing omitted";
    if (shown === total) return "all shown in Preview";
    if (shown === 0) return "all hidden in Preview";
    return `${shown} of ${total} shown in Preview`;
}

function commandButton(name: string, glyph: string, label: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dd-ignored-preview-command";
    button.dataset.command = name;
    button.setAttribute("aria-label", label);
    button.title = label;
    button.append(codicon(glyph, "dd-ignored-preview-command-icon"));
    return button;
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
        // A sandboxed file report can deny storage; the current view still responds.
    }
}

function localStorageOrUndefined(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
