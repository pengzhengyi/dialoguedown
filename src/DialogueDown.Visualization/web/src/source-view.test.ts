import { afterEach, describe, expect, it, vi } from "vitest";
import {
    createFakeDebugController,
    type FakeDebugProgram,
} from "./test-support/fake-debug-controller";
import type { ReservedTarget } from "./model";
import { createSourceView, initSplitDivider, type SourceViewHandle } from "./source-view";

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
const END: ReservedTarget = { anchor: "END", label: "End", role: "Terminal" };

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

    describe("createSourceView reserved targets", () => {
        it("mounts the compiler-projected End sentinel below the document", () => {
            const source = sourceView({ reservedTargets: [END] });

            expect(source.element.querySelector(".dd-reserved-target-label")?.textContent).toBe(
                "End",
            );
            expect(source.element.querySelector(".dd-reserved-target-anchor")?.textContent).toBe(
                "#END",
            );
        });

        it("updates the fixed panel without rebuilding the editor", () => {
            const source = sourceView({ reservedTargets: [END] });

            source.setReservedTargets([]);

            expect(source.element.querySelector(".dd-reserved-targets")).toBeNull();
        });
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

    it("toggles a breakpoint on the cursor line from the accessible toolbar action", async () => {
        const debug = createFakeDebugController(SOURCE, PROGRAM);
        const source = sourceView({ debug });

        source.element
            .querySelector<HTMLButtonElement>('button[aria-label="Toggle breakpoint at cursor"]')!
            .click();
        await flushDebugUpdate();

        expect(source.element.querySelector(".dd-debug-breakpoint-verified")).not.toBeNull();
    });
});

describe("initSplitDivider", () => {
    it("returns a disposer that removes document drag listeners", () => {
        const container = document.createElement("div");
        const divider = document.createElement("div");
        container.appendChild(divider);
        document.body.appendChild(container);
        Object.defineProperty(container, "getBoundingClientRect", {
            value: () => ({ left: 0, top: 0, width: 100, height: 100 }),
        });
        const dispose = initSplitDivider(container, divider);
        divider.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));

        dispose();
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 75, bubbles: true }));

        expect(container.style.getPropertyValue("--source-split")).toBe("");
        expect(document.body.style.userSelect).toBe("");
    });
});

describe("createSourceView jump-to menu", () => {
    afterEach(() => {
        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    });

    it("offers a Jump to submenu whose stage runs with the current selection", () => {
        const run = vi.fn();
        const source = sourceView({
            jumpTargets: [{ title: "Dialogue AST", run, preview: () => null }],
        });
        source.selectRange(0, 5);

        source.element
            .querySelector(".cm-content")!
            .dispatchEvent(
                new MouseEvent("contextmenu", { bubbles: true, clientX: 5, clientY: 5 }),
            );

        const jumpItem = [...document.querySelectorAll<HTMLElement>(".context-menu-item")].find(
            (el) => el.textContent?.includes("Jump to"),
        );
        expect(jumpItem, "a Jump to entry should be offered").toBeDefined();
        jumpItem!.click();

        const stageItem = document.querySelector<HTMLButtonElement>(
            ".context-submenu .context-menu-item",
        );
        expect(stageItem?.textContent).toContain("Dialogue AST");
        stageItem!.click();

        expect(run).toHaveBeenCalledWith(0, 5);
    });

    it("opens the Jump-to picker at the caret on Alt-J", () => {
        const run = vi.fn();
        const source = sourceView({
            jumpTargets: [{ title: "Semantic Model", run, preview: () => null }],
        });

        source.element
            .querySelector(".cm-content")!
            .dispatchEvent(new KeyboardEvent("keydown", { key: "j", altKey: true, bubbles: true }));

        const item = document.querySelector<HTMLButtonElement>(".context-menu .context-menu-item");
        expect(item?.textContent).toContain("Semantic Model");
    });

    it("previews the enclosing span in the editor while a stage is hovered", () => {
        const source = sourceView({
            jumpTargets: [
                { title: "Dialogue AST", run: () => {}, preview: () => ({ start: 0, end: 5 }) },
            ],
        });

        source.element
            .querySelector(".cm-content")!
            .dispatchEvent(new MouseEvent("contextmenu", { bubbles: true }));
        const jumpItem = [...document.querySelectorAll<HTMLElement>(".context-menu-item")].find(
            (el) => el.textContent?.includes("Jump to"),
        )!;
        jumpItem.click();
        const stageItem = document.querySelector<HTMLElement>(
            ".context-submenu .context-menu-item",
        )!;

        stageItem.dispatchEvent(new MouseEvent("mouseenter", { bubbles: true }));
        expect(source.element.querySelector(".dd-jump-preview")).not.toBeNull();

        stageItem.dispatchEvent(new MouseEvent("mouseleave", { bubbles: true }));
        expect(source.element.querySelector(".dd-jump-preview")).toBeNull();
    });

    it("opens no menu in a read-only view with no jump targets", () => {
        const source = sourceView({ editable: false });

        source.element
            .querySelector(".cm-content")!
            .dispatchEvent(new MouseEvent("contextmenu", { bubbles: true }));

        expect(document.querySelector(".context-menu")).toBeNull();
    });
});
