/**
 * How each edge category is drawn and named.
 *
 * Color alone is a poor encoding — it fails for a colorblind reader and washes out when zoomed
 * away — so a route also carries a line pattern. The names are the compiler's own: a reader who
 * has read the docs meets the same words on screen.
 */
export interface EdgeStyle {
    /** What this kind of route is called in the legend. */
    label: string;
    /** The line pattern, or undefined for a solid line. */
    dash?: string;
    /** Whether the line is a route control travels, and so carries an arrowhead. */
    isRoute: boolean;
    /**
     * The pointer shape shown over the line. Each one is a shape the reader already knows from the
     * rest of the desktop, borrowed to say what the route does before the tooltip arrives.
     */
    cursor: string;
    /**
     * A glyph repeated along the line, for a route that a dash pattern alone reads too much like.
     * A stroke cannot draw a symbol, so the line is resampled into a polyline and the glyph is
     * stamped at every vertex — which is what makes a barred `-x-x-` line possible at all.
     */
    symbol?: "cross";
    /**
     * What this kind of route means, in one sentence. Shown when a reader asks about an edge, so
     * the drawing can be read without first learning its vocabulary.
     */
    meaning: string;
}

export const EDGE_STYLES: Readonly<Record<string, EdgeStyle>> = {
    // The default flow: solid, because everything else is a departure from it. The onward arrow
    // says it simply carries on to the next node.
    break: {
        label: "Succession",
        isRoute: true,
        cursor: "e-resize",
        meaning: "The natural order: when this node is done, the next one runs.",
    },
    // Long dashes echo the `=>` a jump is written with; `alias` is the shortcut pointer.
    jump: {
        label: "Jump",
        dash: "10 4",
        isRoute: true,
        cursor: "alias",
        meaning: "A divert (`=>`): control leaves the written order and resumes at the target.",
    },
    // Dotted: one arm among several. The hand is the pointer for something the player picks.
    choice: {
        label: "Choice",
        dash: "2 3",
        isRoute: true,
        cursor: "pointer",
        meaning: "One arm of a choice: taken only if the player picks it.",
    },
    // Dashed: taken only when its condition holds, so the pointer asks the question.
    control: {
        label: "Conditional branch",
        dash: "6 4",
        isRoute: true,
        cursor: "help",
        meaning: "One branch of a conditional: taken only while its condition holds.",
    },
    // Not a route at all — it only says where an unreachable node sits, so it has no arrow. It is
    // barred rather than merely dotted, because "no one ever comes this way" is a different claim
    // from "this way is faint", and crosses say it where a fourth dash pattern would not.
    deferred: {
        label: "Not reached",
        dash: "0 6",
        isRoute: false,
        cursor: "not-allowed",
        symbol: "cross",
        meaning:
            "Not a route at all. Control never arrives here; the line only says where the " +
            "unreachable node sits in the document.",
    },
};

export function edgeStyle(category: string | undefined): EdgeStyle | undefined {
    return category ? EDGE_STYLES[category] : undefined;
}
