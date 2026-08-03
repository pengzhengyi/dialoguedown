import { afterEach, describe, expect, it } from "vitest";
import { createFakeDebugController, type FakeDebugProgram } from "./fake-debug-controller";
import { createSourceView, type SourceViewHandle } from "./source-view";

const SOURCE = "Entry\nEnd\n";
const PROGRAM: FakeDebugProgram = {
    id: "source-view",
    entryId: "entry",
    locations: [
        {
            id: "entry",
            anchor: "Entry",
            label: "Entry",
            paths: [{ id: "end", label: "End", targetId: "end" }],
        },
        { id: "end", anchor: "End", label: "End", paths: [] },
    ],
};

let mounted: SourceViewHandle[] = [];

afterEach(() => {
    for (const source of mounted) source.destroy();
    mounted = [];
    document.body.replaceChildren();
});

function sourceView(options: Parameters<typeof createSourceView>[1] = {}): SourceViewHandle {
    const source = createSourceView(SOURCE, options);
    mounted.push(source);
    document.body.appendChild(source.element);
    return source;
}

async function flushDebugUpdate(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
}

describe("createSourceView debugger integration", () => {
    it("mounts no debugger UI in an ordinary source view", () => {
        const source = sourceView();

        expect(source.element.querySelector(".dd-debug-toolbar")).toBeNull();
        expect(source.element.querySelector(".dd-debug-breakpoint-gutter")).toBeNull();
    });

    it("mounts the toolbar and two gutters when a controller is supplied", () => {
        const debug = createFakeDebugController(SOURCE, PROGRAM);
        const source = sourceView({ debug });

        expect(source.element.querySelector(".dd-debug-toolbar")).not.toBeNull();
        expect(source.element.querySelector(".dd-debug-breakpoint-gutter")).not.toBeNull();
        expect(source.element.querySelector(".dd-debug-execution-gutter")).not.toBeNull();
    });

    it("drives the editor execution marker from the mounted toolbar", async () => {
        const debug = createFakeDebugController(SOURCE, PROGRAM);
        const source = sourceView({ debug });

        source.element
            .querySelector<HTMLButtonElement>('button[aria-label="Start debugging"]')!
            .click();
        await flushDebugUpdate();

        expect(source.element.querySelector(".dd-debug-current-arrow")).not.toBeNull();
        expect(source.element.querySelector(".dd-debug-current-line")).not.toBeNull();
    });
});
