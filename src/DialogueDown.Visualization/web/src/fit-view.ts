import type { CameraTransform } from "./graph-camera";

/** A rectangle in the drawing's own coordinates. */
export interface Extent {
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface Viewport {
    width: number;
    height: number;
}

/** Room to keep clear of the drawing, for the panels that float over the canvas. */
export interface Insets {
    top?: number;
    right?: number;
    bottom?: number;
    left?: number;
}

export interface FitOptions {
    /**
     * The most the drawing may be magnified. A stage smaller than its viewport is *placed*, not
     * blown up: a three-node graph filling the screen at 400% reads as a mistake.
     */
    maxScale?: number;
    /** The least it may be shrunk, so an enormous graph stops at a legible floor. */
    minScale?: number;
    /** Air between the drawing and the edges it is framed within. */
    padding?: number;
}

const DEFAULT_MAX_SCALE = 1;
const DEFAULT_MIN_SCALE = 0.1;
const DEFAULT_PADDING = 24;

/**
 * The camera that shows the whole drawing at once.
 *
 * A stage opens on what it contains rather than on wherever its root happens to be: the dialogue
 * graph runs wide enough that a root-anchored view at full size shows a handful of nodes and
 * leaves the reader to hunt for the rest.
 *
 * The insets keep the drawing clear of the panels floating over the canvas — the legend most of
 * all, which has grown tall enough to cover a good part of what it is describing.
 */
export function frameToFit(
    content: Extent,
    viewport: Viewport,
    insets: Insets = {},
    options: FitOptions = {},
): CameraTransform {
    const {
        maxScale = DEFAULT_MAX_SCALE,
        minScale = DEFAULT_MIN_SCALE,
        padding = DEFAULT_PADDING,
    } = options;
    const { top = 0, right = 0, bottom = 0, left = 0 } = insets;

    // The free rectangle: what is left of the viewport once the floating panels have their room.
    const free = {
        x: left + padding,
        y: top + padding,
        width: Math.max(1, viewport.width - left - right - padding * 2),
        height: Math.max(1, viewport.height - top - bottom - padding * 2),
    };

    const scale = clamp(
        Math.min(
            free.width / Math.max(content.width, 1),
            free.height / Math.max(content.height, 1),
        ),
        minScale,
        maxScale,
    );

    // Center what there is inside the free rectangle.
    return {
        k: scale,
        x: free.x + (free.width - content.width * scale) / 2 - content.x * scale,
        y: free.y + (free.height - content.height * scale) / 2 - content.y * scale,
    };
}

function clamp(value: number, low: number, high: number): number {
    return Math.min(high, Math.max(low, value));
}
