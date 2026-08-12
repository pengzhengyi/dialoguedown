/**
 * Where an edge runs between two laid-out nodes.
 *
 * A node writes its label to the right of its dot, so a line that leaves from the dot strikes
 * through the very words it belongs to. And a cross-link that spans the drawing lies across every
 * row it passes. Both are answered here as pure geometry, so the shapes can be tested without a
 * browser.
 *
 * Coordinates are the drawing's own: x runs left to right along the flow, y down the rows.
 */

export interface Point {
    readonly x: number;
    readonly y: number;
}

/** A cubic curve, named so a test can ask where it goes without parsing a path string. */
export interface Curve {
    readonly start: Point;
    readonly control1: Point;
    readonly control2: Point;
    readonly end: Point;
}

/**
 * A cross-link's three moves: it drops out of its row, runs the length of the lane, and rises to
 * its target. The two vertical moves happen at the columns the nodes themselves stand in, which is
 * why the route can cross rows without ever crossing their words.
 */
export interface LaneRoute {
    readonly start: Point;
    readonly drop: Point;
    readonly rise: Point;
    readonly end: Point;
}

export interface RouteOptions {
    /** How far past the source dot the line starts, so it clears the source's own label. */
    readonly clearance?: number;
    /**
     * A row-free lane, given as a y below the whole drawing, that a cross-link travels along.
     * Absent for the ordinary step from one node to the next, which needs no detour.
     */
    readonly lane?: number;
    /**
     * Which corridor this cross-link climbs in, counted back from its target's own column.
     *
     * Several routes often end at one node — every jump into a scene lands on its entry — and if
     * each climbs in that node's column they lie on top of one another: one line to the eye, and
     * a coin toss to the pointer. A corridor apiece keeps them separate for all but the last few
     * pixels.
     */
    readonly corridor?: number;
    /**
     * How far off its target's row the line makes its final approach, so that even the last leg
     * of two routes to the same node arrives at a different angle.
     */
    readonly port?: number;
}

/** Where a node's text block starts, relative to its dot. Negative: it reaches back a little. */
export const LABEL_BLOCK_ORIGIN = -8;

const LABEL_INSET = 12;
const LABEL_PADDING = 10;
const LEAD_GAP = 6;

/** How much of a forward run is kept for the curve itself, however wide the source's label. */
const MIN_RUN = 12;

/** How far a cross-link steps aside from its source's dot before dropping. */
const LANE_LEAD = 14;
/** The radius of the turn where a cross-link's drop meets its run, and its run meets its rise. */
const LANE_CORNER = 24;
/**
 * How far apart neighboring corridors sit, and how far back they may reach in total.
 *
 * A corridor climbs in the gap between the previous column's words and this one's dot, so the
 * reach is bounded by that gap: past it, a corridor climbs straight through a label. Widening the
 * separation therefore means widening the gap first — clipping labels to a measured budget rather
 * than a character count — which is why these stay narrow for now.
 */
const CORRIDOR_STEP = 7;
const MAX_CORRIDOR_REACH = 38;

/**
 * The width of the block a node's label and attributes occupy, measured from
 * {@link LABEL_BLOCK_ORIGIN}, given the width of its widest line of text.
 */
export function labelBlockWidth(textWidth: number): number {
    return LABEL_INSET + textWidth + LABEL_PADDING;
}

/** How far past its dot a node's outgoing lines should start, so they clear its own text. */
export function labelClearance(textWidth: number): number {
    return LABEL_INSET + textWidth + LEAD_GAP;
}

/**
 * The curve an ordinary step follows, from one node to the next along the flow.
 *
 * It leaves past the source's label, so it never strikes through it, and sweeps to the target's
 * dot, where its arrowhead lands.
 */
export function routeCurve(from: Point, to: Point, clearance = 0): Curve {
    const span = to.x - from.x;
    // Clear the label if the run allows, but always keep a stretch of curve to draw in — a long
    // label on a short run yields ground rather than overshooting its own target.
    const lead = Math.max(0, Math.min(clearance, span - MIN_RUN));
    const start = { x: from.x + lead, y: from.y };
    const middle = (start.x + to.x) / 2;
    return {
        start,
        control1: { x: middle, y: start.y },
        control2: { x: middle, y: to.y },
        end: to,
    };
}

/**
 * The route a cross-link follows, down into the lane and along it.
 *
 * It drops in the empty column just outside its source's dot and rises in its target's own dot
 * column — both to the left of any label — so the only rows it can touch are its own two ends.
 */
export function laneRoute(from: Point, to: Point, lane: number, corridor = 0): LaneRoute {
    const backward = to.x <= from.x;
    const start = { x: from.x + (backward ? -LANE_LEAD : LANE_LEAD), y: from.y };
    const step = backward ? -LANE_CORNER : LANE_CORNER;
    // Climb one corridor further back for each route already claiming this target's approach.
    const reach = Math.min(LANE_CORNER + corridor * CORRIDOR_STEP, MAX_CORRIDOR_REACH);
    return {
        start,
        drop: { x: start.x + step, y: lane },
        rise: { x: to.x - (backward ? -reach : reach), y: lane },
        end: to,
    };
}

/** The line as an SVG path, rounded so the markup stays readable and diffable. */
export function edgePath(from: Point, to: Point, options: RouteOptions = {}): string {
    const { clearance = 0, lane } = options;
    if (lane === undefined) {
        const { start, control1, control2, end } = routeCurve(from, to, clearance);
        return `M${at(start)}C${at(control1)},${at(control2)},${at(end)}`;
    }
    const { start, drop, rise, end } = laneRoute(from, to, lane, options.corridor ?? 0);
    const corner = (turn: Point, into: Point): string => `C${at(turn)},${at(turn)},${at(into)}`;
    return [
        `M${at(start)}`,
        corner({ x: start.x, y: lane }, drop),
        `L${at(rise)}`,
        // Climb the corridor, then lean in to the dot. The lean-in row is the route's port, so
        // two lines ending at one node arrive at different angles instead of one on top of the
        // other — and their arrowheads fan rather than stack.
        `C${at({ x: rise.x, y: lane })},${at({ x: rise.x, y: end.y + (options.port ?? 0) })},${at(end)}`,
    ].join("");
}

function at(point: Point): string {
    return `${round(point.x)},${round(point.y)}`;
}

function round(value: number): number {
    return Math.round(value * 100) / 100;
}
