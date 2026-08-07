import { codicon } from "./codicon";
import type { DiagnosticCounts } from "./problems-panel";

/**
 * The status-line diagnostic summary: an error, warning, and info count that opens the Problems
 * panel.
 *
 * It lives on the status line because that is the only chrome present on **every** tab. Before
 * this, a diagnostic was visible only as a squiggle inside the Source editor, so on the five
 * graph tabs the reader had no signal at all that the script had problems.
 */
export interface DiagnosticSummary {
    /** The status-line control to mount. */
    readonly element: HTMLButtonElement;
    /** Update the displayed totals. */
    setCounts(counts: DiagnosticCounts): void;
    /** Reflect whether the Problems panel is currently open. */
    setOpen(open: boolean): void;
}

const ORDER = [
    { key: "error", icon: "error", label: "errors" },
    { key: "warning", icon: "warning", label: "warnings" },
    { key: "info", icon: "info", label: "infos" },
] as const;

export function createDiagnosticSummary(onOpen: () => void): DiagnosticSummary {
    const element = document.createElement("button");
    element.type = "button";
    element.className = "diagnostic-summary";
    element.setAttribute("aria-expanded", "false");
    element.setAttribute("aria-controls", "footer-drawer");

    const values = new Map<string, HTMLElement>();
    for (const { key, icon } of ORDER) {
        const group = document.createElement("span");
        group.className = `diagnostic-count diagnostic-${key}`;
        group.appendChild(codicon(icon, "diagnostic-icon"));
        const value = document.createElement("span");
        value.className = "diagnostic-value";
        value.textContent = "0";
        group.appendChild(value);
        element.appendChild(group);
        values.set(key, value);
    }

    element.addEventListener("click", onOpen);

    function setCounts(counts: DiagnosticCounts): void {
        for (const { key } of ORDER) values.get(key)!.textContent = String(counts[key]);
        // Shown even at zero, so its absence never has to be interpreted: a clean compile reads
        // "0 0 0" rather than the control disappearing.
        element.classList.toggle("clean", counts.error + counts.warning + counts.info === 0);
        const parts = ORDER.map(({ key, label }) => `${counts[key]} ${label}`).join(", ");
        element.title = `${parts} — open the Problems panel`;
        element.setAttribute("aria-label", element.title);
    }

    setCounts({ error: 0, warning: 0, info: 0 });
    return {
        element,
        setCounts,
        setOpen: (open) => element.setAttribute("aria-expanded", String(open)),
    };
}
