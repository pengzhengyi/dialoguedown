// @vitest-environment node

import { describe, expect, it } from "vitest";
import { GraphCameraStore } from "./graph-camera";

const camera = (k: number, x = 0, y = 0) => ({ k, x, y });

describe("GraphCameraStore", () => {
    it("uses the default framing (null) for an untouched graph with no shared camera", () => {
        const store = new GraphCameraStore();
        expect(store.cameraFor("Markdown AST")).toBeNull();
        expect(store.foldFor("Markdown AST")).toEqual([]);
    });

    it("pins a per-graph override, and shares only its zoom", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5, 400, 90));
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.5, 400, 90)); // its own override
        // An untouched graph frames itself from its own root at the shared scale. A pan means
        // something only against the graph it was made on.
        expect(store.cameraFor("Dialogue AST")).toBeNull();
        expect(store.inheritedZoom("Dialogue AST")).toBe(1.5);
    });

    it("keeps an adjusted graph's own camera while untouched graphs inherit the latest zoom", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5));
        store.adjustCamera("Dialogue AST", camera(0.8));
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.5)); // pinned, unaffected
        expect(store.inheritedZoom("Desugared AST")).toBe(0.8); // untouched inherits latest
    });

    it("does not offer an inherited zoom to a graph that pinned its own", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5));
        store.noteCamera(camera(3));

        expect(store.inheritedZoom("Markdown AST")).toBeNull();
    });

    it("noteCamera moves the shared zoom without pinning an override", () => {
        const store = new GraphCameraStore();
        store.noteCamera(camera(2));
        expect(store.inheritedZoom("Markdown AST")).toBe(2); // inherited
        store.adjustCamera("Markdown AST", camera(1.2)); // Markdown pins
        store.noteCamera(camera(2)); // current moves on
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.2)); // still pinned
        expect(store.inheritedZoom("Dialogue AST")).toBe(2); // inherits the new current
    });

    it("remembers per-graph fold independently", () => {
        const store = new GraphCameraStore();
        store.setFold("Markdown AST", ["n1", "n2"]);
        expect(store.foldFor("Markdown AST")).toEqual(["n1", "n2"]);
        expect(store.foldFor("Dialogue AST")).toEqual([]);
    });

    it("reset drops the override (back to its own framing) and clears the fold", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5)); // override = current = 1.5
        store.setFold("Markdown AST", ["n1"]);
        store.noteCamera(camera(3)); // current moves to 3
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.5)); // still pinned
        store.reset("Markdown AST");
        expect(store.cameraFor("Markdown AST")).toBeNull(); // frames itself again
        expect(store.inheritedZoom("Markdown AST")).toBe(3); // at the shared scale
        expect(store.foldFor("Markdown AST")).toEqual([]);
    });
});
