import { isTextEntryTarget } from "./text-entry";

/** The root-element class that maximizes the active tab and hides the app chrome. */
export const MAXIMIZED_CLASS = "maximized";

/** The root-element class that additionally hides the active tab's secondary panel. */
export const ZEN_CLASS = "zen";

/**
 * The app chrome, hidden in both full screen and Zen. It holds real controls — Save,
 * Discard, Reload, the save-mode capsule, the help toggle — so focus must not be left on
 * one of them: a control inside a `display: none` ancestor can keep keyboard focus in some
 * engines, and Enter would then activate something the reader cannot see.
 */
const CHROME_REGIONS = [".app-header", ".app-footer", "#live-banner"].join(", ");

/**
 * The secondary panels Zen additionally hides. Kept separate from {@link CHROME_REGIONS}
 * because full screen leaves these visible — blurring them there would steal focus from a
 * panel the reader can still see and use.
 */
const ZEN_PANEL_REGIONS = [
    ".source-preview",
    ".source-divider",
    ".config-side",
    ".config-divider",
    ".semantic-tables",
    ".semantic-divider",
    "#detail",
    "#resizer",
    "#explorer",
    "#explorer-resizer",
].join(", ");

/**
 * How much of the app is hidden to focus on the active tab:
 *
 * - `normal` — everything visible.
 * - `maximized` — app chrome hidden; the tab's own panels stay.
 * - `zen` — chrome hidden *and* the tab's secondary panel out of the way, leaving the
 *   editor (Source, Config) or the graph (AST, Semantic Model) alone.
 */
export type FocusMode = "normal" | "maximized" | "zen";

/** The page-level focus mode: full screen and its deeper Zen form. */
export interface Fullscreen {
    /** Enter full screen from normal; leave focus mode from either full screen or Zen. */
    toggle(): void;
    /** Enter Zen from normal or full screen; leave focus mode when already in Zen. */
    toggleZen(): void;
    /** Return to normal (a no-op when already there). */
    exit(): void;
    /** Whether the app chrome is hidden — true in both full screen and Zen. */
    isMaximized(): boolean;
    /** Whether the active tab's secondary panel is also hidden. */
    isZen(): boolean;
    /** The current focus mode. */
    mode(): FocusMode;
}

/**
 * The whole-viewport focus modes. Classes on the root element hide the app chrome (header +
 * tabs, the status footer, the live banner) so the active graph or source split fills the
 * window, and — in **Zen** — the active tab's secondary panel as well. They are plain CSS
 * flags, deliberately *not* the browser Fullscreen API, so they work in the offline
 * single-file report and inside an embedding iframe where that API is commonly blocked.
 *
 * Zen sets {@link MAXIMIZED_CLASS} too, so it reuses the maximized chrome-hiding rules
 * rather than restating them, and adds {@link ZEN_CLASS} for the panel hiding.
 *
 * Because Zen is only a presentation flag, it never writes to the reader's remembered
 * panel-collapse preferences: leaving Zen restores whatever they had chosen, with no state
 * to save or reconcile.
 *
 * Toggle full screen with a maximize button or `f`, Zen with `z`; leave either with the
 * corner chip, its key again, or Escape.
 */
export function initFullscreen(
    root: HTMLElement = document.body,
    doc: Document = document,
): Fullscreen {
    const mode = (): FocusMode => {
        if (root.classList.contains(ZEN_CLASS)) return "zen";
        return root.classList.contains(MAXIMIZED_CLASS) ? "maximized" : "normal";
    };

    const set = (next: FocusMode): void => {
        root.classList.toggle(MAXIMIZED_CLASS, next !== "normal");
        root.classList.toggle(ZEN_CLASS, next === "zen");
        releaseFocusFromHiddenRegions(doc, next);
        // The icon glyph follows the root class in CSS; here we only reflect the state on
        // each button's label and tooltip, which CSS cannot express.
        const focused = next !== "normal";
        doc.querySelectorAll<HTMLElement>(".maximize-button").forEach((button) => {
            button.setAttribute("aria-pressed", String(focused));
            button.title = focused ? "Exit full screen (Esc)" : "Full screen (f)";
            button.setAttribute("aria-label", focused ? "Exit full screen" : "Full screen");
        });
        // The Zen button tracks Zen specifically, not the shared chrome-hiding: in plain full
        // screen it is still an available action rather than an active one.
        const zen = next === "zen";
        doc.querySelectorAll<HTMLElement>(".zen-button").forEach((button) => {
            button.setAttribute("aria-pressed", String(zen));
            button.title = zen ? "Exit Zen mode (Esc)" : "Zen mode (z)";
            button.setAttribute("aria-label", zen ? "Exit Zen mode" : "Zen mode");
        });
    };

    // Either key leaves focus mode entirely from anywhere but its own "off" state, so one
    // press always gets the reader back rather than stepping down through a middle mode.
    const toggle = (): void => set(mode() === "normal" ? "maximized" : "normal");
    const toggleZen = (): void => set(mode() === "zen" ? "normal" : "zen");
    const exit = (): void => set("normal");

    doc.addEventListener("keydown", (event) => {
        // Yield to a handler that already acted (e.g. Escape closing the editor's search).
        if (event.defaultPrevented) return;
        if (event.key === "Escape") {
            if (mode() !== "normal") {
                exit();
                event.preventDefault();
            }
            return;
        }
        if (event.ctrlKey || event.metaKey || event.altKey) return;

        const key = event.key.toLowerCase();
        if (key !== "f" && key !== "z") return;
        // Leave the shortcut alone while the reader is typing (the editor or a form field).
        if (isTextEntryTarget(event.target)) return;

        if (key === "f") toggle();
        else toggleZen();
        event.preventDefault();
    });

    return {
        toggle,
        toggleZen,
        exit,
        isMaximized: () => mode() !== "normal",
        isZen: () => mode() === "zen",
        mode,
    };
}

/**
 * Blur the focused control when the mode just being entered hides the region it sits in.
 * Relying on the browser's own focus fixup is not enough: in some engines focus stays on a
 * control whose *ancestor* became `display: none`, so Enter would still activate it —
 * a hidden Save or Reload in the chrome, or a collapse toggle inside a Zen-hidden divider
 * whose activation would write the reader's persisted layout.
 */
function releaseFocusFromHiddenRegions(doc: Document, next: FocusMode): void {
    if (next === "normal") return;
    const active = doc.activeElement;
    if (!(active instanceof HTMLElement)) return;

    const hidden = next === "zen" ? `${CHROME_REGIONS}, ${ZEN_PANEL_REGIONS}` : CHROME_REGIONS;
    if (active.closest(hidden)) active.blur();
}
