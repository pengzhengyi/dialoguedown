import {
    createFakeDebugController,
    type FakeDebugController,
    type FakeDebugProgram,
} from "./fake-debug-controller";

/** The only registered fake program in the branch-only line-debugger exploration. */
export const LINE_DEBUGGER_FIXTURE_ID = "line-debugger-v1";

/** The dedicated source fixture mirrored by `examples/line-debugger-prototype.dialogue.md`. */
export const LINE_DEBUGGER_SOURCE = `# Crossroads

Guide: The road divides ahead.
Guide: Choose a road.

- Take the forest.
  Guide: The trees close behind you.
- Take the tunnel.
  Guide: The tunnel returns to the crossroads.

# Safe at last

Guide: Safe at last.
`;

const LINE_DEBUGGER_PROGRAM: FakeDebugProgram = {
    id: LINE_DEBUGGER_FIXTURE_ID,
    entryId: "approach",
    locations: [
        {
            id: "approach",
            anchor: "Guide: The road divides ahead.",
            label: "Approach the crossroads",
            paths: [{ id: "choose", label: "Continue", targetId: "choose" }],
        },
        {
            id: "choose",
            anchor: "Guide: Choose a road.",
            label: "Choose a road",
            paths: [
                { id: "forest", label: "Take the forest", targetId: "forest" },
                { id: "tunnel", label: "Take the tunnel", targetId: "tunnel" },
            ],
        },
        {
            id: "forest",
            anchor: "  Guide: The trees close behind you.",
            label: "Forest path",
            paths: [{ id: "safe", label: "Reach safety", targetId: "safe" }],
        },
        {
            id: "tunnel",
            anchor: "  Guide: The tunnel returns to the crossroads.",
            label: "Tunnel loop",
            paths: [{ id: "loop", label: "Return to the choice", targetId: "choose" }],
        },
        {
            id: "safe",
            anchor: "Guide: Safe at last.",
            label: "Safe at last",
            paths: [],
        },
    ],
};

/**
 * Create the fake debugger only for the explicit registered fixture query. An unknown fixture
 * leaves the report untouched; a known fixture over different source returns an unavailable
 * controller rather than inventing locations.
 */
export function createLineDebuggerPrototype(
    source: string | undefined,
    search: string,
): FakeDebugController | undefined {
    const query = new URLSearchParams(search);
    if (
        query.get("debug") !== "fake" ||
        query.get("fixture") !== LINE_DEBUGGER_FIXTURE_ID ||
        source === undefined
    ) {
        return undefined;
    }
    return createFakeDebugController(source, LINE_DEBUGGER_PROGRAM);
}
