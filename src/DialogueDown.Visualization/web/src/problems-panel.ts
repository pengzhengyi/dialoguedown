import { codicon } from "./codicon";
import { errorCodeUrl } from "./diagnostics-overlay";
import type { LspDiagnostic, LspSeverity } from "./model";

/**
 * The Problems panel: every diagnostic the compiler reported for the document, as a list whose
 * rows navigate to the text they describe.
 *
 * Until this existed the only way to find a problem was to scroll the Source editor hunting for
 * squiggles, and on the graph tabs the diagnostics were invisible entirely.
 */

/** How many diagnostics of each severity the document currently has. */
export interface DiagnosticCounts {
    readonly error: number;
    readonly warning: number;
    readonly info: number;
}

/** The severity names the panel styles and counts by. LSP's `4` (hint) reads as info here. */
const SEVERITY_NAME: Record<LspSeverity, keyof DiagnosticCounts> = {
    1: "error",
    2: "warning",
    3: "info",
    4: "info",
};

/** The codicon per severity, matching the glyphs the editor's gutter already uses. */
const SEVERITY_ICON: Record<keyof DiagnosticCounts, string> = {
    error: "error",
    warning: "warning",
    info: "info",
};

export interface ProblemsPanelOptions {
    /**
     * Navigate to a diagnostic. The panel hands over the whole value rather than a resolved
     * offset, so it never needs an editor to render — converting the LSP range is the caller's
     * job, which keeps the panel testable without a laid-out document.
     */
    goTo(diagnostic: LspDiagnostic): void;
}

export interface ProblemsPanel {
    /** The panel body to mount in the footer drawer. */
    readonly element: HTMLElement;
    /** Replace the listed diagnostics. An empty list renders the clean-compile state. */
    setDiagnostics(diagnostics: readonly LspDiagnostic[]): void;
    /** The current per-severity totals, for the status-line summary. */
    counts(): DiagnosticCounts;
}

/** Order by position: the order the writer reads in, so the list walks forward through the text. */
function byPosition(a: LspDiagnostic, b: LspDiagnostic): number {
    return (
        a.range.start.line - b.range.start.line || a.range.start.character - b.range.start.character
    );
}

/** Render a zero-based LSP position the way an editor reports it, one-based. */
function locationLabel(diagnostic: LspDiagnostic): string {
    const { line, character } = diagnostic.range.start;
    return `Ln ${line + 1}, Col ${character + 1}`;
}

function buildRow(diagnostic: LspDiagnostic, onActivate: () => void): HTMLElement {
    const severity = SEVERITY_NAME[diagnostic.severity];
    const row = document.createElement("div");
    row.className = "problem-row";
    row.dataset.severity = severity;

    // The row holds two *sibling* controls rather than nesting the docs link inside the jump
    // button: an anchor inside a button is invalid, and assistive technology cannot offer a
    // control it cannot reach.
    const jump = document.createElement("button");
    jump.type = "button";
    jump.className = "problem-jump";

    jump.appendChild(codicon(SEVERITY_ICON[severity], "problem-icon"));

    const message = document.createElement("span");
    message.className = "problem-message";
    message.textContent = diagnostic.message;
    jump.appendChild(message);

    const location = document.createElement("span");
    location.className = "problem-location";
    location.textContent = locationLabel(diagnostic);
    jump.appendChild(location);

    jump.setAttribute(
        "aria-label",
        `${severity}: ${diagnostic.message} at ${locationLabel(diagnostic)} — go to the problem`,
    );
    jump.addEventListener("click", onActivate);
    row.appendChild(jump);

    // The code explains *what the rule is*, which is a different question from *where the
    // problem is*, so it gets its own control pointing at that code's documentation.
    const code = document.createElement("a");
    code.className = "problem-code";
    code.textContent = diagnostic.code;
    code.href = errorCodeUrl(diagnostic.code);
    code.target = "_blank";
    code.rel = "noreferrer";
    code.title = `What ${diagnostic.code} means`;
    row.appendChild(code);

    return row;
}

export function createProblemsPanel(options: ProblemsPanelOptions): ProblemsPanel {
    const element = document.createElement("div");
    element.className = "problems-panel";

    const list = document.createElement("div");
    list.className = "problem-list";
    element.appendChild(list);

    const empty = document.createElement("p");
    empty.className = "problem-empty";
    empty.textContent = "No problems — this script compiles cleanly.";
    element.appendChild(empty);

    let current: DiagnosticCounts = { error: 0, warning: 0, info: 0 };

    function setDiagnostics(diagnostics: readonly LspDiagnostic[]): void {
        list.replaceChildren(
            ...[...diagnostics].sort(byPosition).map((d) => buildRow(d, () => options.goTo(d))),
        );

        const tally = { error: 0, warning: 0, info: 0 };
        for (const d of diagnostics) tally[SEVERITY_NAME[d.severity]] += 1;
        current = tally;

        const clean = diagnostics.length === 0;
        empty.hidden = !clean;
        list.hidden = clean;
    }

    setDiagnostics([]);
    return { element, setDiagnostics, counts: () => current };
}
