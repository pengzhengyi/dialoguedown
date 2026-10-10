/**
 * @file Clipping a label to the width it is allowed, rather than to a count of characters.
 *
 * Thirty `W`s are more than twice as wide as thirty `i`s, so only a measured clip gives the gap
 * beside a column a known width. The tree view's cross-link corridors run in that gap, and a known
 * width lets the layout space them out without a line crossing any label.
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

    // Not even the ellipsis fits, so nothing is shown.
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
