import { afterEach, describe, expect, it } from "vitest";
import { createDebugToolbar } from "./debug-toolbar";
import {
    createFakeDebugController,
    type FakeDebugController,
    type FakeDebugProgram,
} from "./fake-debug-controller";

const SOURCE = "Entry\nBranch\nLeft\nRight\nEnd\n";
const PROGRAM: FakeDebugProgram = {
    id: "toolbar",
    entryId: "entry",
    locations: [
        {
            id: "entry",
            anchor: "Entry",
            label: "Entry",
            paths: [{ id: "next", label: "Next", targetId: "branch" }],
        },
        {
            id: "branch",
            anchor: "Branch",
            label: "Branch",
            paths: [
                { id: "left", label: "Take left", targetId: "left" },
                { id: "right", label: "Take right", targetId: "right" },
            ],
        },
        {
            id: "left",
            anchor: "Left",
            label: "Left",
            paths: [{ id: "end-left", label: "End", targetId: "end" }],
        },
        {
            id: "right",
            anchor: "Right",
            label: "Right",
            paths: [{ id: "end-right", label: "End", targetId: "end" }],
        },
        { id: "end", anchor: "End", label: "End", paths: [] },
    ],
};

let mounted: Array<{ destroy(): void }> = [];

afterEach(() => {
    for (const toolbar of mounted) toolbar.destroy();
    mounted = [];
    document.body.replaceChildren();
});

function mount(source = SOURCE): { debug: FakeDebugController; element: HTMLElement } {
    const debug = createFakeDebugController(source, PROGRAM);
    const toolbar = createDebugToolbar(debug);
    mounted.push(toolbar);
    document.body.appendChild(toolbar.element);
    return { debug, element: toolbar.element };
}

function button(element: HTMLElement, name: string): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>(`button[aria-label="${name}"]`)!;
}

describe("createDebugToolbar", () => {
    it("starts ready with only Start enabled and a visible prototype label", () => {
        const { element } = mount();

        expect(element.querySelector(".dd-debug-status")?.textContent).toBe("Ready");
        expect(element.querySelector(".dd-debug-prototype")?.textContent).toContain("Prototype");
        expect(button(element, "Start debugging").disabled).toBe(false);
        expect(button(element, "Continue").disabled).toBe(true);
        expect(button(element, "Step over").disabled).toBe(true);
        expect(button(element, "Stop debugging").disabled).toBe(true);
    });

    it("drives Start, Step Over, and Stop from the controller snapshot", () => {
        const { element } = mount();

        button(element, "Start debugging").click();

        expect(element.querySelector(".dd-debug-status")?.textContent).toBe("Paused · line 1");
        expect(button(element, "Continue").disabled).toBe(false);
        expect(button(element, "Step over").disabled).toBe(false);
        expect(button(element, "Stop debugging").disabled).toBe(false);

        button(element, "Step over").click();
        expect(element.querySelector(".dd-debug-status")?.textContent).toBe("Paused · line 2");

        button(element, "Stop debugging").click();
        expect(element.querySelector(".dd-debug-status")?.textContent).toBe("Ready");
    });

    it("shows path choices only while awaiting a path and pauses at the chosen target", () => {
        const { element } = mount();
        button(element, "Start debugging").click();
        button(element, "Step over").click();

        button(element, "Step over").click();

        const paths = element.querySelector<HTMLElement>(".dd-debug-paths")!;
        expect(paths.hidden).toBe(false);
        expect(paths.textContent).toContain("Choose path");
        expect(paths.textContent).toContain("Take left");
        expect(paths.textContent).toContain("Take right");
        expect(button(element, "Continue").disabled).toBe(true);

        paths.querySelector<HTMLButtonElement>('button[data-path-id="left"]')!.click();

        expect(paths.hidden).toBe(true);
        expect(element.querySelector(".dd-debug-status")?.textContent).toBe("Paused · line 3");
    });

    it("renders unavailable and stale messages with every run command disabled", () => {
        const unavailable = mount("Not the fixture");
        expect(unavailable.element.querySelector(".dd-debug-status")?.textContent).toContain(
            "Prototype fixture unavailable",
        );
        expect(button(unavailable.element, "Start debugging").disabled).toBe(true);

        const active = mount();
        button(active.element, "Start debugging").click();
        active.debug.sourceChanged();

        expect(active.element.querySelector(".dd-debug-status")?.textContent).toBe(
            "Source changed — save and restart.",
        );
        expect(button(active.element, "Start debugging").disabled).toBe(true);
        expect(button(active.element, "Stop debugging").disabled).toBe(true);
    });
});
