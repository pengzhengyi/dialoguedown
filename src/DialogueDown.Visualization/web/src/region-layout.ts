import { PAD_BOTTOM, PAD_TOP } from "./region-bands";

/**
 * Laying a graph out so a scene's band is never drawn over another's.
 *
 * The tree layout places a node from its place among its siblings and the extent of its subtree,
 * and never reads which scene the node belongs to. Where flow crosses between scenes their rows
 * interleave, and the bands drawn behind them — each the bounding box of its own nodes — cross.
 *
 * This pass runs after that layout and rewrites the cross-axis coordinate alone: every region is
 * given one contiguous run of rows, a **tier**, and the tiers are stacked clear of one another.
 * Two bands then cannot intersect, and no node can fall inside a band that is not its own.
 *
 * Depth is never touched, so a node keeps the column the tree layout gave it.
 */

/** What the pass needs of a node the tree layout has already placed. */
export interface RankInput {
    readonly id: string;
    /** The scene the node sits in. Absent for the entry and anything above the first heading. */
    readonly region?: string;
    /** The row the tree layout gave it. Several nodes routinely share one. */
    readonly row: number;
}

/**
 * The distance between two rows of one tier.
 *
 * It matches the tree layout's own node size, so a scene whose rows the layout already separated
 * keeps the spacing a reader is used to.
 */
export const ROW_PITCH = 62;

/**
 * The distance from one tier's last row to the next tier's first.
 *
 * A band reaches {@link PAD_TOP} above its nodes and {@link PAD_BOTTOM} below them, so parting the
 * rows by less than their sum would leave the bands themselves touching. The remainder is the air
 * a reader sees between two scenes.
 */
export const TIER_GAP = PAD_TOP + PAD_BOTTOM + 28;

/** The tier holding every node that belongs to no scene. It is laid down first, above the rest. */
const PROLOGUE = Symbol("prologue");

type TierKey = string | typeof PROLOGUE;

/**
 * Where each node should sit, keyed by id, so that regions do not interleave.
 *
 * `tierOrder` fixes the order the scenes are stacked in; the caller passes the order the stage
 * names them, which is what the legend lists. A region named there but absent from `nodes` takes
 * no room, and a region present in `nodes` but missing from the order is still placed, after the
 * ones that were named, so the pass is total whatever it is handed.
 */
export function rankByRegion(
    nodes: readonly RankInput[],
    tierOrder: readonly string[],
): Map<string, number> {
    const placed = new Map<string, number>();
    if (nodes.length === 0) {
        return placed;
    }

    const members = groupByTier(nodes);
    // The origin is kept so the drawing does not slide wholesale away from where the root sat.
    let cursor = nodes.reduce((lowest, node) => Math.min(lowest, node.row), nodes[0].row);

    for (const tier of tiersIn(members, tierOrder)) {
        const tierMembers = members.get(tier)!;
        // The rows a tier occupies, not the nodes on them: a straight run of dialogue shares one
        // row across many columns, and splitting it apart would turn a line into a staircase.
        const rows = [...new Set(tierMembers.map((node) => node.row))].sort(
            (left, right) => left - right,
        );
        const rowPlacement = new Map(rows.map((row, index) => [row, cursor + index * ROW_PITCH]));
        for (const node of tierMembers) {
            placed.set(node.id, rowPlacement.get(node.row)!);
        }
        cursor += (rows.length - 1) * ROW_PITCH + TIER_GAP;
    }

    return placed;
}

function groupByTier(nodes: readonly RankInput[]): Map<TierKey, RankInput[]> {
    const members = new Map<TierKey, RankInput[]>();
    for (const node of nodes) {
        const tier: TierKey = node.region ?? PROLOGUE;
        const existing = members.get(tier);
        if (existing) {
            existing.push(node);
        } else {
            members.set(tier, [node]);
        }
    }
    return members;
}

/**
 * The tiers to lay down, in order: the prologue first, then the named regions, then any region
 * the caller did not name. Only tiers that actually hold a node are returned, so a region the
 * stage names but does not draw costs no vertical space.
 */
function tiersIn(
    members: ReadonlyMap<TierKey, readonly RankInput[]>,
    tierOrder: readonly string[],
): TierKey[] {
    const ordered: TierKey[] = [PROLOGUE, ...tierOrder];
    const named = new Set<TierKey>(ordered);
    for (const tier of members.keys()) {
        if (!named.has(tier)) {
            ordered.push(tier);
            named.add(tier);
        }
    }
    return ordered.filter((tier) => (members.get(tier)?.length ?? 0) > 0);
}
