/**
 * Lucide Icons (ISC): the standard "hide/show side panel" glyphs. A right-hand panel uses
 * `panel-right-close` (an inward chevron) to hide and `panel-right-open` (outward) to show; a
 * left-hand panel (the Explorer) uses the `panel-left-*` pair, so each side's chevron points the
 * way the panel moves — drawn correctly per side rather than mirrored with a CSS transform, which
 * some engines render inconsistently. Both glyphs render into the one button; CSS reveals whichever
 * matches the panel's collapsed state (a class on the panel's container), so a toggle built while
 * the panel is already collapsed still shows the correct glyph.
 */
export type PanelSide = "left" | "right";

const svgIcon = (variant: string, divider: string, chevron: string): string =>
    `<svg class="collapse-icon ${variant}" viewBox="0 0 24 24" width="15" height="15" ` +
    `fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" ` +
    `stroke-linejoin="round" aria-hidden="true">` +
    `<rect width="18" height="18" x="3" y="3" rx="2"/><path d="${divider}"/><path d="${chevron}"/></svg>`;

// Per side: the panel's divider, plus the hide (collapse) and show (expand) chevrons, drawn so the
// chevron points the way the panel moves — a right panel hides rightward, a left panel leftward.
const GLYPHS: Record<PanelSide, { divider: string; collapse: string; expand: string }> = {
    right: { divider: "M15 3v18", collapse: "m8 9 3 3-3 3", expand: "m10 15-3-3 3-3" },
    left: { divider: "M9 3v18", collapse: "m16 15-3-3 3-3", expand: "m14 9 3 3-3 3" },
};

/**
 * A hide/show toggle carrying both the collapse and expand panel glyphs for its {@link side}
 * (default the right). The visible glyph is chosen by CSS from the container's collapsed class
 * rather than per-button state, so a toggle rebuilt while the panel is collapsed still reads
 * correctly. The mousedown is swallowed so pressing the toggle on a resize divider never starts a
 * drag.
 */
export function createCollapseToggle(
    onToggle: () => void,
    side: PanelSide = "right",
): HTMLButtonElement {
    const glyph = GLYPHS[side];
    const button = document.createElement("button");
    button.type = "button";
    button.className = "collapse-toggle";
    button.innerHTML =
        svgIcon("icon-collapse", glyph.divider, glyph.collapse) +
        svgIcon("icon-expand", glyph.divider, glyph.expand);
    button.addEventListener("click", onToggle);
    button.addEventListener("mousedown", (event) => event.stopPropagation());
    return button;
}

/** A right-side panel that can be hidden to give the main content the full width. */
export interface CollapsiblePanel {
    /** The toggle button to place on the panel's divider. */
    readonly button: HTMLButtonElement;
    /** Hide the panel if shown, show it if hidden. */
    toggle(): void;
    /** Whether the panel is currently hidden. */
    isCollapsed(): boolean;
}

export interface CollapsiblePanelOptions {
    /** The element that carries {@link collapsedClass} while the panel is hidden. */
    container: HTMLElement;
    /** The class toggled on {@link container} to hide the panel. */
    collapsedClass: string;
    /** localStorage key remembering the collapsed state across reloads. */
    storageKey: string;
    /** Accessible name of the panel, e.g. "inspector" or "preview". */
    name: string;
    /** Which side the panel is on, so the toggle's chevrons point the right way. Defaults to "right". */
    side?: PanelSide;
    /** Storage for the remembered state; defaults to `localStorage`. */
    storage?: Storage;
    /**
     * Whether the panel starts hidden when the reader has never chosen. Defaults to shown.
     *
     * A panel that begins hidden needs the remembered value to say *which* state was chosen, so
     * "shown on purpose" is not mistaken for "never said" — see {@link initCollapsiblePanel}.
     */
    startCollapsed?: boolean;
    /**
     * Builds the control that drives the panel, given the toggle to call. Defaults to the
     * divider's chevron handle. Lets a panel be summoned from somewhere else entirely — the
     * Explorer is opened from a pinned control in the tab bar rather than from its divider.
     */
    createButton?(toggle: () => void): HTMLButtonElement;
}

/**
 * Wire a panel so a reader can hide it and bring it back. `container` carries
 * `collapsedClass` while hidden (CSS then hides the panel and lets the main pane fill),
 * and the choice is remembered in `localStorage` under `storageKey` — guarded, so a
 * `file://` report still works for the session. Returns a controller whose `button`
 * belongs wherever the panel is summoned from; for a divider handle that is the divider,
 * where it doubles as the always-present re-open handle.
 *
 * The remembered value records the state the reader chose (`"1"` hidden, `"0"` shown) rather
 * than only marking the hidden one, so a panel whose default is hidden can still tell a
 * deliberate "show it" from silence.
 */
export function initCollapsiblePanel(options: CollapsiblePanelOptions): CollapsiblePanel {
    const { container, collapsedClass, storageKey, name, startCollapsed = false } = options;
    const storage = options.storage ?? defaultStorage();

    const isCollapsed = (): boolean => container.classList.contains(collapsedClass);

    const reflect = (collapsed: boolean): void => {
        container.classList.toggle(collapsedClass, collapsed);
        const label = collapsed ? `Show ${name}` : `Hide ${name}`;
        button.title = label;
        button.setAttribute("aria-label", label);
        button.setAttribute("aria-expanded", String(!collapsed));
    };

    const toggle = (): void => {
        const collapsed = !isCollapsed();
        reflect(collapsed);
        try {
            storage?.setItem(storageKey, collapsed ? "1" : "0");
        } catch {
            // storage unavailable (private mode / file://) — the applied state still holds
        }
    };

    const button = options.createButton
        ? options.createButton(toggle)
        : createCollapseToggle(toggle, options.side);

    let remembered: string | null = null;
    try {
        remembered = storage?.getItem(storageKey) ?? null;
    } catch {
        // storage unavailable (private mode / file://) — fall back to the panel's own default
    }
    reflect(remembered === null ? startCollapsed : remembered === "1");

    return { button, toggle, isCollapsed };
}

/** `localStorage`, or `undefined` when it is not available (e.g. a sandboxed `file://`). */
function defaultStorage(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
