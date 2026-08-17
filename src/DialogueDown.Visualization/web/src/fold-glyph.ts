import { codicon } from "./codicon";

/**
 * The one look folding has, wherever the report offers it.
 *
 * The Source editor, the Preview, and the Dialogue Graph each fold a different kind of thing, but
 * a reader who learns the gesture on one should recognize it on the next. Keeping the glyphs here
 * means a new surface cannot quietly introduce a fifth rendering of "fold this".
 *
 * A chevron always performs the action; a status mark such as `circle-slash` states what a thing
 * *is* and stays a static, unfocusable mark beside it.
 */

/** The chevron a fold control shows: down over an open item, right over a shut one. */
export function foldGlyphName(expanded: boolean): string {
    return expanded ? "chevron-down" : "chevron-right";
}

/**
 * The same chevron as a bare character, for surfaces that draw text instead of HTML. The Dialogue
 * Graph renders SVG, and the codicon font is declared for the whole document, so an SVG `text`
 * node in that font shows the identical glyph rather than a hand-drawn lookalike.
 */
export function foldGlyphCharacter(expanded: boolean): string {
    return expanded ? "\ueab4" : "\ueab6";
}

/** The shared chevron as a decorative element, for surfaces that build HTML controls. */
export function foldControlIcon(expanded: boolean, extraClass: string): HTMLElement {
    return codicon(foldGlyphName(expanded), extraClass);
}

/**
 * The fold marker a CodeMirror gutter shows. Supplying this replaces the library's default text
 * characters, which are the one place the report spoke a different visual language from itself.
 * The titles match the library's own so nothing that reads them has to change.
 */
export function foldGutterMarker(open: boolean): HTMLElement {
    const marker = foldControlIcon(open, "cm-fold-marker");
    marker.title = open ? "Fold line" : "Unfold line";
    return marker;
}

/** The pair every surface uses for the two commands that fold or open everything it holds. */
export const FOLD_COMMAND_GLYPHS = {
    expandAll: "expand-all",
    collapseAll: "collapse-all",
} as const;
