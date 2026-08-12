// @vitest-environment node

import { describe, expect, it } from "vitest";
import { frameToFit, type Extent, type Viewport } from "./fit-view";

const viewport: Viewport = { width: 1000, height: 600 };

/** Where a point of the drawing lands on screen under a camera. */
function project(camera: { k: number; x: number; y: number }, x: number, y: number) {
    return { x: camera.x + x * camera.k, y: camera.y + y * camera.k };
}

describe("frameToFit", () => {
    it("shrinks a drawing larger than its viewport until the whole of it fits", () => {
        const content: Extent = { x: 0, y: 0, width: 4000, height: 1200 };

        const camera = frameToFit(content, viewport, {}, { padding: 0 });

        expect(camera.k).toBeCloseTo(0.25); // width is the binding constraint
        const end = project(camera, content.width, content.height);
        expect(end.x).toBeLessThanOrEqual(viewport.width + 0.001);
        expect(end.y).toBeLessThanOrEqual(viewport.height + 0.001);
    });

    it("places a small drawing rather than blowing it up", () => {
        // A three-node graph filling the screen at 400% reads as a mistake, not as a feature.
        const camera = frameToFit({ x: 0, y: 0, width: 100, height: 40 }, viewport);

        expect(camera.k).toBe(1);
    });

    it("stops shrinking at a legible floor, however large the drawing", () => {
        const camera = frameToFit({ x: 0, y: 0, width: 500000, height: 500000 }, viewport);

        expect(camera.k).toBe(0.1);
    });

    it("centers the drawing in what is left of the viewport", () => {
        const content: Extent = { x: 0, y: 0, width: 500, height: 300 };

        const camera = frameToFit(content, viewport, {}, { padding: 0, maxScale: 1 });

        const start = project(camera, 0, 0);
        const end = project(camera, content.width, content.height);
        expect(start.x).toBeCloseTo(viewport.width - end.x);
        expect(start.y).toBeCloseTo(viewport.height - end.y);
    });

    it("keeps the drawing clear of a panel floating over the canvas", () => {
        // The legend has grown tall enough to cover a good part of what it describes.
        const content: Extent = { x: 0, y: 0, width: 500, height: 300 };

        const camera = frameToFit(content, viewport, { right: 300 }, { padding: 0 });

        expect(project(camera, content.width, content.height).x).toBeLessThanOrEqual(700.001);
    });

    it("frames a drawing that does not start at the origin", () => {
        // A laid-out tree has negative rows above its root; the camera has to account for them.
        const camera = frameToFit(
            { x: -200, y: -150, width: 400, height: 300 },
            viewport,
            {},
            {
                padding: 0,
            },
        );

        const start = project(camera, -200, -150);
        expect(start.x).toBeGreaterThanOrEqual(-0.001);
        expect(start.y).toBeGreaterThanOrEqual(-0.001);
    });

    it("survives an empty drawing without dividing by zero", () => {
        const camera = frameToFit({ x: 0, y: 0, width: 0, height: 0 }, viewport);

        expect(Number.isFinite(camera.k)).toBe(true);
        expect(Number.isFinite(camera.x)).toBe(true);
        expect(Number.isFinite(camera.y)).toBe(true);
    });

    it("survives a viewport smaller than the room its panels ask for", () => {
        const camera = frameToFit(
            { x: 0, y: 0, width: 500, height: 300 },
            { width: 80, height: 60 },
            {
                right: 300,
            },
        );

        expect(Number.isFinite(camera.k)).toBe(true);
        expect(camera.k).toBeGreaterThanOrEqual(0.1);
    });
});
