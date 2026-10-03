/**
 * Cross-links the Semantic and Playbook tabs: hovering any element that carries an entity key
 * highlights every element sharing it — for example a scene's graph node, its anchor-table row,
 * and any jump that resolves to it. The key is the single source of truth (no title or position
 * matching), carried as `data-entity-key` (the element *is* that entity) or `data-ref-key` (the
 * element *references* it). Both map to the same highlight.
 */
export interface EntityHighlighter {
    /** Clear the active highlight, so hovering the same key after a re-render highlights again. */
    refresh(): void;
}

const HIGHLIGHT_CLASS = "entity-highlight";
const KEYED_SELECTOR = "[data-entity-key],[data-ref-key]";

/**
 * Wire hover cross-linking within `root`. Any element carrying `data-entity-key` or
 * `data-ref-key` highlights, on hover, every element in `root` sharing that key. Uses
 * delegated listeners on `root`, so it keeps working as tables and the graph re-render.
 */
export function createEntityHighlighter(root: HTMLElement): EntityHighlighter {
    const keyOf = (target: EventTarget | null): string | null => {
        const element = (target as Element | null)?.closest?.(KEYED_SELECTOR);
        return (
            element?.getAttribute("data-entity-key") ??
            element?.getAttribute("data-ref-key") ??
            null
        );
    };

    const setHighlight = (key: string, on: boolean): void => {
        for (const element of root.querySelectorAll(
            `[data-entity-key="${cssEscape(key)}"],[data-ref-key="${cssEscape(key)}"]`,
        )) {
            element.classList.toggle(HIGHLIGHT_CLASS, on);
        }
    };

    let active: string | null = null;
    const clear = (): void => {
        if (active !== null) {
            setHighlight(active, false);
            active = null;
        }
    };

    root.addEventListener("pointerover", (event) => {
        const key = keyOf(event.target);
        if (key === active) return;
        clear();
        if (key !== null) {
            active = key;
            setHighlight(key, true);
        }
    });
    root.addEventListener("pointerleave", clear);

    return { refresh: clear };
}

/** Escape a key for use inside an attribute-value selector (keys are simple, but be safe). */
function cssEscape(value: string): string {
    const api = globalThis.CSS;
    return api?.escape ? api.escape(value) : value.replace(/["\\]/g, "\\$&");
}
