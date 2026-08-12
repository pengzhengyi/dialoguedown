import { ARROWHEAD_PATH, CROSS_PATH, edgeStyle, type EdgeStyle } from "./edge-style";
import { colorOf } from "./palette";

/**
 * A legend swatch drawn as the edge itself, rather than approximated.
 *
 * The legend used to redraw each pattern as a CSS repeating gradient, which was a second
 * implementation of the dash vocabulary — free to drift from the drawing, unable to carry an
 * arrowhead, and unable to show the glyphs a stamped line is marked with. Here the swatch is a
 * tiny SVG using the very same `stroke-dasharray`, the same round caps, and the same arrowhead as
 * the route it stands for, so what the reader learns is what the reader will find.
 */

/**
 * How wide a swatch is drawn. Wide enough that the longest pattern repeats — a pattern shown once
 * is not a pattern but a bar of unknown length, which is what made a jump and a conditional
 * indistinguishable when the swatch was 14px. `periodsShown` is the guard, and it is tested.
 */
const SWATCH_WIDTH = 48;
const SWATCH_HEIGHT = 12;

/** The drawn line's own width, so the swatch is the same weight as what it stands for. */
const STROKE_WIDTH = 1.5;

/** The arrowhead's drawn size, and how far the line stops short so the head is its tip. */
const ARROW_SIZE = 7;
const ARROW_STANDOFF = 5;

/** A repeated glyph is stamped along the swatch at this spacing, as the drawing stamps it. */
const SYMBOL_SPACING = 12;
const SYMBOL_SIZE = 7;

const SVG_NS = "http://www.w3.org/2000/svg";

/**
 * The swatch for one route: its line, drawn as it is drawn on the canvas.
 *
 * `scope` namespaces the arrowhead marker, because several legends share one document and an
 * `id` collision would give every stage the first one's colors.
 */
export function edgeSwatch(category: string, scope: string): SVGSVGElement {
    const style = edgeStyle(category);
    const color = colorOf(category);
    const svg = document.createElementNS(SVG_NS, "svg");
    svg.setAttribute("class", "edge-swatch");
    svg.setAttribute("viewBox", `0 0 ${SWATCH_WIDTH} ${SWATCH_HEIGHT}`);
    svg.setAttribute("width", String(SWATCH_WIDTH));
    svg.setAttribute("height", String(SWATCH_HEIGHT));
    svg.setAttribute("aria-hidden", "true");

    const middle = SWATCH_HEIGHT / 2;
    const isRoute = style?.isRoute ?? false;
    const end = isRoute ? SWATCH_WIDTH - ARROW_STANDOFF : SWATCH_WIDTH;

    const id = `legend-${scope}-${category}`;
    if (isRoute) svg.appendChild(marker(`arrow-${id}`, ARROWHEAD_PATH, ARROW_SIZE, color, true));
    if (style?.symbol) svg.appendChild(marker(`tick-${id}`, CROSS_PATH, SYMBOL_SIZE, color, false));

    const line = document.createElementNS(SVG_NS, "path");
    line.setAttribute("class", "swatch-line");
    line.setAttribute("d", trace(end, middle, style?.symbol !== undefined));
    line.setAttribute("stroke", color);
    line.setAttribute("stroke-width", String(STROKE_WIDTH));
    line.setAttribute("stroke-linecap", "round");
    line.setAttribute("fill", "none");
    if (style?.dash) line.setAttribute("stroke-dasharray", style.dash);
    if (isRoute) line.setAttribute("marker-end", `url(#arrow-${id})`);
    if (style?.symbol) line.setAttribute("marker-mid", `url(#tick-${id})`);
    svg.appendChild(line);
    return svg;
}

/**
 * The swatch's line. A stamped route is drawn as a run of segments rather than one, because a
 * glyph is placed at a vertex — the same trick the canvas plays when it resamples such a line.
 */
function trace(end: number, middle: number, stamped: boolean): string {
    if (!stamped) return `M0,${middle}L${end},${middle}`;
    const stops = [];
    for (let x = 0; x <= end; x += SYMBOL_SPACING) stops.push(`${x},${middle}`);
    return `M${stops.join("L")}L${end},${middle}`;
}

/**
 * One of the glyphs a line carries, built exactly as the canvas builds it. A marker cannot inherit
 * its line's color, so it is given one; a cross stays upright, while an arrowhead turns to point.
 */
function marker(
    id: string,
    shape: string,
    size: number,
    color: string,
    pointing: boolean,
): SVGDefsElement {
    const defs = document.createElementNS(SVG_NS, "defs");
    const element = document.createElementNS(SVG_NS, "marker");
    element.setAttribute("id", id);
    element.setAttribute("viewBox", "0 0 10 10");
    element.setAttribute("refX", "5");
    element.setAttribute("refY", "5");
    element.setAttribute("markerWidth", String(size));
    element.setAttribute("markerHeight", String(size));
    element.setAttribute("markerUnits", "userSpaceOnUse");
    element.setAttribute("orient", pointing ? "auto-start-reverse" : "0");

    const glyph = document.createElementNS(SVG_NS, "path");
    glyph.setAttribute("d", shape);
    if (pointing) {
        glyph.setAttribute("fill", color);
    } else {
        glyph.setAttribute("stroke", color);
        glyph.setAttribute("stroke-width", String(STROKE_WIDTH));
        glyph.setAttribute("fill", "none");
    }
    element.appendChild(glyph);
    defs.appendChild(element);
    return defs;
}

/** How many whole periods of a pattern the swatch shows — the measure of whether it reads. */
export function periodsShown(style: EdgeStyle | undefined): number {
    if (!style?.dash) return Infinity; // a solid line is its own pattern at any length
    const period = style.dash
        .split(/[\s,]+/)
        .map(Number)
        .reduce((total, part) => total + part, 0);
    return period === 0 ? Infinity : (SWATCH_WIDTH - ARROW_STANDOFF) / period;
}
