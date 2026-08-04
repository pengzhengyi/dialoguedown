import { describe, expect, it } from "vitest";
import {
    createFakeDebugController,
    type FakeDebugProgram,
    type FakeDebugController,
} from "./fake-debug-controller";

const SOURCE = `# Crossroads

Guide: Choose a road.
- Take the forest.
  Guide: The trees close behind you.
- Take the tunnel.
  Guide: The tunnel returns to the crossroads.
Guide: Safe at last.
`;

const PROGRAM: FakeDebugProgram = {
    id: "line-debugger-v1",
    entryId: "choose",
    locations: [
        {
            id: "choose",
            anchor: "Guide: Choose a road.",
            label: "Choose a road",
            paths: [{ id: "next", label: "Open the choice", targetId: "branch" }],
        },
        {
            id: "branch",
            anchor: "- Take the forest.",
            label: "Choose a path",
            paths: [
                { id: "forest", label: "Take the forest", targetId: "forest" },
                { id: "tunnel", label: "Take the tunnel", targetId: "tunnel" },
            ],
        },
        {
            id: "forest",
            anchor: "  Guide: The trees close behind you.",
            label: "Forest",
            paths: [{ id: "finish", label: "Continue", targetId: "end" }],
        },
        {
            id: "tunnel",
            anchor: "  Guide: The tunnel returns to the crossroads.",
            label: "Tunnel",
            paths: [{ id: "loop", label: "Return", targetId: "choose" }],
        },
        {
            id: "end",
            anchor: "Guide: Safe at last.",
            label: "End",
            paths: [],
        },
    ],
};

function controller(source = SOURCE, program = PROGRAM): FakeDebugController {
    return createFakeDebugController(source, program);
}

describe("createFakeDebugController", () => {
    it("binds unique full-line anchors and starts ready", () => {
        const debug = controller();

        expect(debug.snapshot()).toMatchObject({
            status: "ready",
            controls: { start: true, continue: false, stepOver: false, stop: false },
        });
    });

    it("is unavailable when the entry anchor is missing or ambiguous", () => {
        const missing = controller(SOURCE.replace("Guide: Choose a road.", "Guide: Go."));
        const duplicate = controller(`${SOURCE}Guide: Choose a road.\n`);

        expect(missing.snapshot()).toMatchObject({ status: "unavailable" });
        expect(duplicate.snapshot()).toMatchObject({ status: "unavailable" });
    });

    it("starts paused before the entry location", () => {
        const debug = controller();

        debug.start();

        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "choose", line: 3 },
            controls: { start: false, continue: true, stepOver: true, stop: true },
        });
    });

    it("steps over one path and pauses before its target", () => {
        const debug = controller();
        debug.start();

        debug.stepOver();

        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "branch", line: 4 },
        });
    });

    it("awaits an explicit path at a branch, then remains paused at the target", () => {
        const debug = controller();
        debug.start();
        debug.stepOver();

        debug.stepOver();

        expect(debug.snapshot()).toMatchObject({
            status: "awaiting-path",
            location: { id: "branch" },
            paths: [
                { id: "forest", label: "Take the forest" },
                { id: "tunnel", label: "Take the tunnel" },
            ],
        });

        debug.choosePath("forest");

        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "forest", line: 5 },
            paths: [],
        });
    });

    it("continues to the next verified breakpoint and ignores the one it leaves", () => {
        const debug = controller();
        debug.setBreakpoints([3, 8]);
        debug.start();

        debug.continue();

        expect(debug.snapshot()).toMatchObject({
            status: "awaiting-path",
            location: { id: "branch", line: 4 },
        });

        debug.choosePath("forest");
        debug.continue();

        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "end", line: 8 },
        });
    });

    it("pauses for a breakpoint on a branch before asking for its path", () => {
        const debug = controller();
        debug.setBreakpoints([4]);
        debug.start();

        debug.continue();

        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "branch", line: 4 },
        });

        debug.continue();

        expect(debug.snapshot()).toMatchObject({
            status: "awaiting-path",
            location: { id: "branch", line: 4 },
        });
    });

    it("detects a repeated location during Continue instead of looping forever", () => {
        const cycleProgram: FakeDebugProgram = {
            id: "cycle",
            entryId: "forest",
            locations: [
                {
                    id: "forest",
                    anchor: "  Guide: The trees close behind you.",
                    label: "Forest",
                    paths: [{ id: "to-tunnel", label: "Tunnel", targetId: "tunnel" }],
                },
                {
                    id: "tunnel",
                    anchor: "  Guide: The tunnel returns to the crossroads.",
                    label: "Tunnel",
                    paths: [{ id: "to-forest", label: "Forest", targetId: "forest" }],
                },
            ],
        };
        const debug = controller(SOURCE, cycleProgram);
        debug.start();

        debug.continue();

        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "forest" },
            message: expect.stringContaining("Cycle encountered"),
        });
    });

    it("marks requested breakpoints verified only on bound execution lines", () => {
        const debug = controller();

        debug.setBreakpoints([1, 3, 99, 3]);

        expect(debug.snapshot().breakpoints).toEqual([
            { line: 1, verified: false },
            { line: 3, verified: true },
            { line: 99, verified: false },
        ]);
    });

    it("invalidates active execution and breakpoint bindings until a clean rebind", () => {
        const debug = controller();
        debug.setBreakpoints([3]);
        debug.start();

        debug.sourceChanged();

        expect(debug.snapshot()).toMatchObject({
            status: "stale",
            breakpoints: [{ line: 3, verified: false }],
            controls: { start: false, continue: false, stepOver: false, stop: false },
        });
        expect(debug.snapshot().location).toBeUndefined();

        debug.rebind(`Intro\n${SOURCE}`);

        expect(debug.snapshot()).toMatchObject({
            status: "ready",
            breakpoints: [{ line: 3, verified: false }],
            controls: { start: true },
        });
    });

    it("stops an active or ended session and can restart from End", () => {
        const debug = controller();
        debug.start();
        debug.stop();
        expect(debug.snapshot().status).toBe("ready");

        debug.start();
        debug.stepOver();
        debug.stepOver();
        debug.choosePath("forest");
        debug.stepOver();
        debug.stepOver();
        expect(debug.snapshot().status).toBe("ended");

        debug.start();
        expect(debug.snapshot()).toMatchObject({
            status: "paused",
            location: { id: "choose" },
        });
    });

    it("surfaces a missing path target as an ended prototype error", () => {
        const broken: FakeDebugProgram = {
            ...PROGRAM,
            locations: [
                {
                    id: "choose",
                    anchor: "Guide: Choose a road.",
                    label: "Choose",
                    paths: [{ id: "missing", label: "Missing", targetId: "nowhere" }],
                },
            ],
        };
        const debug = controller(SOURCE, broken);
        debug.start();

        debug.stepOver();

        expect(debug.snapshot()).toMatchObject({
            status: "ended",
            message: expect.stringContaining("nowhere"),
        });
    });
});
