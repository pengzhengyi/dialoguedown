/**
 * Clipping a label to the width it is allowed, rather than to a count of characters.
 *
 * A character count is a poor proxy for width: thirty `W`s are more than twice as wide as thirty
 * `i`s, so a fixed count leaves the gap beside a column unknown. The cross-link corridors climb in
 * that gap, so "unknown" means they cannot be spread apart without some label, somewhere, being
 * struck through. Measuring instead makes the gap a number the layout can rely on.
 */

/** Measures a string as it would be drawn. Returns the width in the drawing's own units. */
export type MeasureText = (text: string) => number;

/** The mark left where words were cut. */
export const ELLIPSIS = "…";

/**
 * The longest prefix of `text` that fits `budget`, with an ellipsis where it was cut.
 *
 * Returns the text unchanged when it already fits. Narrows by binary search, so a long label costs
 * a handful of measurements rather than one per character — measuring is the expensive part.
 */
export function clipToWidth(text: string, budget: number, measure: MeasureText): string {
    if (text === "" || measure(text) <= budget) return text;

    // Not even the mark fits: there is no honest way to show anything at all.
    if (measure(ELLIPSIS) > budget) return "";

    let fits = 0; // a prefix length known to fit, with the ellipsis
    let tooWide = text.length; // a prefix length known not to
    while (tooWide - fits > 1) {
        const middle = Math.floor((fits + tooWide) / 2);
        if (measure(text.slice(0, middle) + ELLIPSIS) <= budget) fits = middle;
        else tooWide = middle;
    }
    return text.slice(0, fits).trimEnd() + ELLIPSIS;
}
