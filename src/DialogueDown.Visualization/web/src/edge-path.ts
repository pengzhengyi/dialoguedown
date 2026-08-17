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
    /**
     * How far short of the target the line stops, so it ends on the dot's edge rather than at its
     * center.
     *
     * A line that runs to the center has to be hidden by the dot, and its arrowhead pushed back
     * to compensate. The head is pushed back along the line's *final direction*, though, which on
     * a curved approach is not the direction of the center — so the head lands beside the line
     * and stroke shows past it. Stopping the line where the dot begins leaves nothing to hide and
     * nothing to compensate for.
     */
    readonly standoff?: number;
}

/** Where a node's text block starts, relative to its dot. Negative: it reaches back a little. */
export const LABEL_BLOCK_ORIGIN = -8;

/** How far a node's words start from its dot. */
export const LABEL_INSET = 12;
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
 * A corridor climbs in the gutter between the previous column's words and this column's dot, so
 * the reach is bounded by that gutter: past it, a corridor would climb straight through a label.
 * The gutter is a known width because labels are clipped to a measured budget, and the layout
 * derives that budget from this reach — so the two cannot drift apart.
 */
export const CORRIDOR_STEP = 18;
export const MAX_CORRIDOR_REACH = LANE_CORNER + CORRIDOR_STEP * 5;

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
    const { clearance = 0, lane, standoff = 0 } = options;
    if (lane === undefined) {
        const { start, control1, control2, end } = routeCurve(from, to, clearance);
        const tip = pullBack(end, control2, standoff, start);
        return `M${at(start)}C${at(control1)},${at(control2)},${at(tip)}`;
    }
    const { start, drop, rise, end } = laneRoute(from, to, lane, options.corridor ?? 0);
    const corner = (turn: Point, into: Point): string => `C${at(turn)},${at(turn)},${at(into)}`;
    // The lean-in row is the route's own port, and also the direction its head must point.
    const lean = { x: rise.x, y: end.y + (options.port ?? 0) };
    const tip = pullBack(end, lean, standoff, rise);
    return [
        `M${at(start)}`,
        corner({ x: start.x, y: lane }, drop),
        `L${at(rise)}`,
        // Climb the corridor, then lean in to the dot. The lean-in row is the route's port, so
        // two lines ending at one node arrive at different angles instead of one on top of the
        // other — and their arrowheads fan rather than stack.
        `C${at({ x: rise.x, y: lane })},${at(lean)},${at(tip)}`,
    ].join("");
}

/**
 * Step back from the line's end along the direction it arrives from, so it stops on the dot's
 * edge while keeping the heading it would have had.
 *
 * Backing along the final control point is what preserves that heading: a cubic leaves its last
 * control point pointing at its end, so the trimmed end sits on the line's own tangent rather
 * than off to one side. The standoff yields ground when the whole approach is shorter than it,
 * which would otherwise turn a short step into a line running backwards.
 */
function pullBack(end: Point, from: Point, standoff: number, floor: Point): Point {
    if (standoff <= 0) return end;
    const dx = end.x - from.x;
    const dy = end.y - from.y;
    const length = Math.hypot(dx, dy);
    if (length === 0) return end;
    const reach = Math.min(standoff, Math.hypot(end.x - floor.x, end.y - floor.y));
    return { x: end.x - (dx / length) * reach, y: end.y - (dy / length) * reach };
}

function at(point: Point): string {
    return `${round(point.x)},${round(point.y)}`;
}

function round(value: number): number {
    return Math.round(value * 100) / 100;
}
