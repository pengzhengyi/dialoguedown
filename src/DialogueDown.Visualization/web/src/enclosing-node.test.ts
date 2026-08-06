import { describe, expect, it } from "vitest";
import { findEnclosingNode } from "./enclosing-node";
import type { DisplayNode } from "./model";

function node(id: string, start: number, end: number): DisplayNode {
    return { id, label: id, span: { start, end } };
}

describe("findEnclosingNode", () => {
    it("returns the tightest node whose span encloses the selection", () => {
        const document = node("document", 0, 100);
        const scene = node("scene", 10, 60);
        const line = node("line", 20, 40);

        expect(findEnclosingNode([document, scene, line], 25, 30)?.id).toBe("line");
        expect(findEnclosingNode([document, scene, line], 12, 55)?.id).toBe("scene");
        expect(findEnclosingNode([document, scene, line], 5, 8)?.id).toBe("document");
    });

    it("treats a caret (zero-width selection) as a point inside a span", () => {
        const scene = node("scene", 10, 60);
        const line = node("line", 20, 40);

        expect(findEnclosingNode([scene, line], 30, 30)?.id).toBe("line");
        expect(findEnclosingNode([scene, line], 15, 15)?.id).toBe("scene");
    });

    it("normalizes a backwards selection", () => {
        const line = node("line", 20, 40);
        expect(findEnclosingNode([line], 35, 25)?.id).toBe("line");
    });

    it("falls back to the node enclosing the selection start when none contains the whole range", () => {
        const first = node("first", 0, 30);
        const second = node("second", 30, 60);
        // Selection straddles the boundary between two siblings.
        expect(findEnclosingNode([first, second], 20, 45)?.id).toBe("first");
    });

    it("ignores zero-width (synthetic caret) spans as enclosers", () => {
        const synthetic: DisplayNode = { id: "synthetic", label: "synthetic", span: { start: 25, end: 25 } };
        const line = node("line", 20, 40);
        expect(findEnclosingNode([synthetic, line], 25, 25)?.id).toBe("line");
    });

    it("returns null when no span-bearing node encloses the offset", () => {
        const line = node("line", 20, 40);
        const spanless: DisplayNode = { id: "spanless", label: "spanless" };
        expect(findEnclosingNode([line, spanless], 50, 55)).toBeNull();
        expect(findEnclosingNode([], 0, 0)).toBeNull();
    });
});
