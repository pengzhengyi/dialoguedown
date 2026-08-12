/**
 * The bands drawn behind the nodes of a region.
 *
 * A scene is an area of the document, not a property of each line inside it. Printing its name
 * under every node says the same thing a dozen times and pushes the labels apart; drawing it once,
 * as a band the nodes sit in, says it where it belongs.
 */

export interface PlacedNode {
    /** The region this node belongs to, if any. Nodes without one sit outside every band. */
    readonly region?: string;
    /** The node's dot, in drawing coordinates. */
    readonly x: number;
    readonly y: number;
    /** How far the node's text reaches to the right of its dot. */
    readonly width: number;
}

export interface Band {
    readonly region: string;
    readonly x: number;
    readonly y: number;
    readonly width: number;
    readonly height: number;
    /** Which tint to fill with, so neighboring regions stay apart without a color per scene. */
    readonly tint: number;
}

/** How far a band reaches beyond the nodes it holds. Room above for the band's own name. */
const PAD_LEFT = 16;
const PAD_RIGHT = 16;
const PAD_TOP = 26;
const PAD_BOTTOM = 18;

/** How many tints the bands cycle through before repeating. */
export const REGION_TINTS = 5;

/**
 * Which tint each region wears, by order of first appearance.
 *
 * Read from the stage's own node order rather than the drawing's, so the band and everything that
 * names it elsewhere — a panel title, a row — always agree on a region's color.
 */
export function tintsOf(regions: readonly (string | undefined)[]): Map<string, number> {
    const tints = new Map<string, number>();
    for (const region of regions) {
        if (region && !tints.has(region)) tints.set(region, tints.size % REGION_TINTS);
    }
    return tints;
}

/**
 * One band per region, sized to hold every node in it.
 *
 * The tints come from {@link tintsOf}, so the same document always draws the same colors and a
 * reader moving between rebuilds is not asked to relearn them.
 */
export function bandsOf(
    nodes: readonly PlacedNode[],
    tints: Map<string, number> = tintsOf(nodes.map((node) => node.region)),
): Band[] {
    const order: string[] = [];
    const extents = new Map<string, { left: number; right: number; top: number; bottom: number }>();

    for (const node of nodes) {
        const { region } = node;
        if (!region) continue;
        const extent = extents.get(region);
        if (!extent) {
            order.push(region);
            extents.set(region, {
                left: node.x,
                right: node.x + node.width,
                top: node.y,
                bottom: node.y,
            });
            continue;
        }
        extent.left = Math.min(extent.left, node.x);
        extent.right = Math.max(extent.right, node.x + node.width);
        extent.top = Math.min(extent.top, node.y);
        extent.bottom = Math.max(extent.bottom, node.y);
    }

    return order.map((region) => {
        const { left, right, top, bottom } = extents.get(region)!;
        return {
            region,
            x: left - PAD_LEFT,
            y: top - PAD_TOP,
            width: right - left + PAD_LEFT + PAD_RIGHT,
            height: bottom - top + PAD_TOP + PAD_BOTTOM,
            tint: tints.get(region) ?? 0,
        };
    });
}
