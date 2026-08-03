import { afterEach, describe, expect, it } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import {
    debugEditor,
    requestedBreakpointLines,
    shouldRevealDebugLocation,
    toggleBreakpointAt,
} from "./debug-editor";
import {
    createFakeDebugController,
    type FakeDebugController,
    type FakeDebugProgram,
} from "./fake-debug-controller";

const SOURCE = "Alpha\n\nBeta\nGamma\n";
const PROGRAM: FakeDebugProgram = {
    id: "editor-test",
    entryId: "alpha",
    locations: [
        {
            id: "alpha",
            anchor: "Alpha",
            label: "Alpha",
            paths: [{ id: "beta", label: "Beta", targetId: "beta" }],
        },
        {
            id: "beta",
            anchor: "Beta",
            label: "Beta",
            paths: [{ id: "gamma", label: "Gamma", targetId: "gamma" }],
        },
        { id: "gamma", anchor: "Gamma", label: "Gamma", paths: [] },
    ],
};

let mounted: EditorView[] = [];

afterEach(() => {
    for (const view of mounted) view.destroy();
    mounted = [];
    document.body.replaceChildren();
});

function mount(): { view: EditorView; debug: FakeDebugController } {
    const debug = createFakeDebugController(SOURCE, PROGRAM);
    const parent = document.createElement("div");
    document.body.appendChild(parent);
    const view = new EditorView({
        parent,
        state: EditorState.create({
            doc: SOURCE,
            selection: { anchor: SOURCE.indexOf("Beta") + 1 },
            extensions: [debugEditor(debug)],
        }),
    });
    mounted.push(view);
    return { view, debug };
}

async function flushDebugUpdate(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
}

describe("debugEditor", () => {
    it("toggles requested breakpoints and synchronizes one-based lines to the controller", async () => {
        const { view, debug } = mount();

        toggleBreakpointAt(view, view.state.doc.line(3).from);
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([3]);
        expect(debug.snapshot().breakpoints).toEqual([{ line: 3, verified: true }]);

        toggleBreakpointAt(view, view.state.doc.line(3).from);
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([]);
        expect(debug.snapshot().breakpoints).toEqual([]);
    });

    it("renders verified dots and unverified rings in the breakpoint gutter", async () => {
        const { view } = mount();

        toggleBreakpointAt(view, view.state.doc.line(1).from);
        toggleBreakpointAt(view, view.state.doc.line(2).from);
        await flushDebugUpdate();

        expect(view.dom.querySelectorAll(".dd-debug-breakpoint-verified")).toHaveLength(1);
        expect(view.dom.querySelectorAll(".dd-debug-breakpoint-unverified")).toHaveLength(1);
    });

    it("maps a breakpoint with its line when text is inserted above it", async () => {
        const { view } = mount();
        toggleBreakpointAt(view, view.state.doc.line(3).from);
        await flushDebugUpdate();

        view.dispatch({ changes: { from: 0, insert: "Prelude\n" } });
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([4]);
    });

    it("keeps a breakpoint when the first character of its line is edited", async () => {
        const { view } = mount();
        const line = view.state.doc.line(3);
        toggleBreakpointAt(view, line.from);
        await flushDebugUpdate();

        view.dispatch({ changes: { from: line.from, to: line.from + 1, insert: "b" } });
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([3]);
    });

    it("keeps breakpoint line requests across a full-buffer replacement", async () => {
        const { view } = mount();
        toggleBreakpointAt(view, view.state.doc.line(1).from);
        toggleBreakpointAt(view, view.state.doc.line(3).from);
        await flushDebugUpdate();

        view.dispatch({
            changes: {
                from: 0,
                to: view.state.doc.length,
                insert: `Prelude\n${view.state.doc.toString()}`,
            },
        });
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([1, 3]);
    });

    it("removes a breakpoint when its entire line is deleted", async () => {
        const { view } = mount();
        const line = view.state.doc.line(3);
        toggleBreakpointAt(view, line.from);
        await flushDebugUpdate();

        view.dispatch({ changes: { from: line.from, to: line.to + 1 } });
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([]);
    });

    it("removes a breakpoint when CodeMirror deletes a middle line with its preceding newline", async () => {
        const { view } = mount();
        const line = view.state.doc.line(3);
        toggleBreakpointAt(view, line.from);
        await flushDebugUpdate();

        // This is the range CodeMirror's delete-line command uses for a non-first line.
        view.dispatch({ changes: { from: line.from - 1, to: line.to } });
        await flushDebugUpdate();

        expect(requestedBreakpointLines(view.state)).toEqual([]);
    });

    it("shows a separate execution arrow and paused-line decoration without moving selection", async () => {
        const { view, debug } = mount();
        const selection = view.state.selection.main;

        debug.start();
        await flushDebugUpdate();

        expect(view.dom.querySelectorAll(".dd-debug-current-arrow")).toHaveLength(1);
        expect(view.dom.querySelectorAll(".dd-debug-current-line")).toHaveLength(1);
        expect(view.state.selection.main).toEqual(selection);
    });

    it("clears execution visuals but keeps a hollow breakpoint when source changes", async () => {
        const { view, debug } = mount();
        toggleBreakpointAt(view, view.state.doc.line(1).from);
        debug.start();
        await flushDebugUpdate();

        view.dispatch({ changes: { from: view.state.doc.length, insert: "\nEdited" } });
        await flushDebugUpdate();

        expect(debug.snapshot().status).toBe("stale");
        expect(requestedBreakpointLines(view.state)).toEqual([1]);
        expect(view.dom.querySelectorAll(".dd-debug-current-arrow")).toHaveLength(0);
        expect(view.dom.querySelectorAll(".dd-debug-current-line")).toHaveLength(0);
        expect(view.dom.querySelectorAll(".dd-debug-breakpoint-unverified")).toHaveLength(1);
    });

    it("renders distinct breakpoint and execution gutters before line numbers", () => {
        const { view } = mount();

        const gutters = [...view.dom.querySelectorAll(".cm-gutter")].map((gutter) =>
            gutter.className.toString(),
        );

        expect(gutters[0]).toContain("dd-debug-breakpoint-gutter");
        expect(gutters[1]).toContain("dd-debug-execution-gutter");
    });

    it("reveals only a newly paused location, not same-location snapshot updates", () => {
        const { debug } = mount();
        const ready = debug.snapshot();
        debug.start();
        const paused = debug.snapshot();
        debug.setBreakpoints([1]);
        const sameLocation = debug.snapshot();
        debug.stepOver();
        const nextLocation = debug.snapshot();

        expect(shouldRevealDebugLocation(ready, paused)).toBe(true);
        expect(shouldRevealDebugLocation(paused, sameLocation)).toBe(false);
        expect(shouldRevealDebugLocation(sameLocation, nextLocation)).toBe(true);
    });
});
