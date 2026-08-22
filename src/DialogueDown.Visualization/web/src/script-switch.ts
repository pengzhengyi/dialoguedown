/**
 * Opening another script **in place**: ask the server to change its active document, fetch the new
 * report, and repaint — instead of loading a whole page. The reader keeps the window they were
 * working in, so their zoom and open tab survive the move.
 *
 * The browser wiring (`fetch`, `history`, `location`) is injected through {@link ScriptSwitchPorts},
 * so the sequence is unit-testable, mirroring the Explorer and the live-edit controller.
 */

import type { Report, ServedMode } from "./model";

/** The side-effecting collaborators a switch drives, injected so it is testable without a browser. */
export interface ScriptSwitchPorts {
    /** Settle unsaved work before leaving the current script; false keeps the reader where they are. */
    resolve(): Promise<boolean>;
    /** The mode a newly opened script inherits — the one the reader is in. */
    currentMode(): ServedMode;
    /**
     * Make {@link path} the server's active document, returning the report URL it redirected to,
     * or `null` when it refused (a deleted or unreadable script).
     */
    open(path: string, mode: ServedMode): Promise<string | null>;
    /** The active document's report payload, or `null` when the server has none. */
    document(): Promise<Report | null>;
    /**
     * Whether {@link report} fits the page as it was built. A script compiled against a different
     * `dialogue.toml` does not: the page wired its editors and panes from the config it started
     * with, so that script needs a full load rather than a repaint.
     */
    fitsPage(report: Report): boolean;
    /** Re-point the report — its content, editing state, and identity — at the opened script. */
    apply(path: string, report: Report, mode: ServedMode): void;
    /** Record {@link url} as a new history entry for {@link path}. */
    pushHistory(path: string, url: string): void;
    /** Replace the current history entry with {@link path}'s. */
    setHistory(path: string, url: string): void;
    /** Load {@link url} as a whole page — the fallback when a switch cannot be applied in place. */
    load(url: string): void;
    /** Surface a failed open to the reader. */
    showProblem(message: string): void;
}

/** Opens scripts into the report the reader already has. */
export interface ScriptSwitch {
    /** Open {@link path}, adding a history entry so Back returns to the current script. */
    open(path: string): Promise<void>;
    /**
     * Apply the script a Back or Forward landed on. It adds no history entry — the browser has
     * already moved — and lands in View, because Back is a navigation rather than an intent to edit.
     */
    restore(path: string): Promise<void>;
}

/**
 * Wire a script switch over {@link ports}, starting from the script the page loaded
 * ({@link initial}), which is recorded as the first history entry so Back can return to it.
 */
export function createScriptSwitch(
    ports: ScriptSwitchPorts,
    initial: { path: string; url: string },
): ScriptSwitch {
    let current = initial;
    // Only the newest switch may repaint. A slower earlier one must not land on top of the script
    // the reader has already moved to.
    let token = 0;

    ports.setHistory(current.path, current.url);

    async function go(path: string, mode: ServedMode, push: boolean): Promise<void> {
        const mine = ++token;
        if (!(await ports.resolve())) {
            // The reader kept their unsaved work. A Back already moved the address bar, so put it
            // back on the script still on screen rather than leaving the two disagreeing.
            if (!push && mine === token) ports.setHistory(current.path, current.url);
            return;
        }
        if (mine !== token) return;

        const url = await ports.open(path, mode);
        if (url === null) {
            // Nothing changed on the server, so staying put is safe and the address bar is right.
            if (mine === token) ports.showProblem(`Could not open ${path}.`);
            return;
        }

        const report = await ports.document();
        if (mine !== token) return;
        // Past this point the server has already switched, so anything that stops the repaint is
        // recovered by loading the new script's page: that is slower, never wrong.
        if (report === null || !ports.fitsPage(report) || !isActive(report, path)) {
            ports.load(url);
            return;
        }

        ports.apply(path, report, mode);
        current = { path, url };
        if (push) ports.pushHistory(path, url);
        else ports.setHistory(path, url);
    }

    return {
        open(path) {
            return go(path, ports.currentMode(), true);
        },
        restore(path) {
            return go(path, "view", false);
        },
    };
}

// Guards against repainting the wrong script: two opens in flight can reach the server in either
// order, and only the payload for the one being applied may be shown.
function isActive(report: Report, path: string): boolean {
    const active = report.project?.activePath;
    return active === undefined || active === path;
}
