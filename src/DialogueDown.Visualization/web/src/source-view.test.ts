import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { EditorState } from "@codemirror/state";
import {
    createFakeDebugController,
    type FakeDebugProgram,
} from "./test-support/fake-debug-controller";
import type { ReservedTarget, SemanticToken } from "./model";
import {
    createSourceView,
    initSplitDivider,
    mapPreviewSpans,
    type SourceViewHandle,
} from "./source-view";

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
    return mountSource(SOURCE, options);
}

function mountSource(
    content: string,
    options: Parameters<typeof createSourceView>[1] = {},
): SourceViewHandle {
    const source = createSourceView(content, options);
    mounted.push(source);
    document.body.appendChild(source.element);
    return source;
}

async function flushDebugUpdate(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
}

describe("createSourceView ignored Markdown preview", () => {
    const table = "| A | B |\n| - | - |\n| x | y |";

    // The Preview view baseline is persisted, so a test that runs a global command would otherwise
    // decide what the next test's region control does.
    beforeEach(() => {
        try {
            globalThis.localStorage?.removeItem("dd-ignored-preview-collapsed");
        } catch {
            // A test environment without storage already starts from the default baseline.
        }
    });

    const ignoredTable: SemanticToken = {
        kind: "IgnoredMarkdown",
        range: {
            start: { line: 0, character: 0 },
            end: { line: 2, character: 9 },
        },
    };

    it("always mounts a Preview footer mirroring the fixed Source footer", () => {
        const source = mountSource("Alice: Hi.");

        const shell = source.element.querySelector(".source-preview-shell");
        const footer = source.element.querySelector(".dd-ignored-preview-footer");

        expect(shell?.contains(source.element.querySelector(".source-preview"))).toBe(true);
        expect(shell?.contains(footer)).toBe(true);
        expect(footer?.textContent).toContain("0 ignored");
    });

    it("mirrors the compiler's ignored cue in the rendered preview", () => {
        const source = mountSource(table);

        source.setSemanticTokens([ignoredTable]);

        expect(
            source.element
                .querySelector(".source-preview table")
                ?.classList.contains("dd-preview-ignored"),
        ).toBe(true);
        expect(source.element.querySelector(".dd-preview-ignored-region")).not.toBeNull();
        expect(source.element.querySelector(".dd-ignored-preview-footer")?.textContent).toContain(
            "1 ignored",
        );
    });

    it("restores full-strength preview rendering when the compiler keeps the construct", () => {
        const source = mountSource(table);
        source.setSemanticTokens([ignoredTable]);

        source.setSemanticTokens([]);

        expect(
            source.element
                .querySelector(".source-preview table")
                ?.classList.contains("dd-preview-ignored"),
        ).toBe(false);
        expect(source.element.querySelector(".dd-preview-ignored-region")).toBeNull();
    });

    it("marks a conditional blockquote from the compiler's control-keyword token", () => {
        const source = mountSource("> `if` `Ready?`\n>\n> Alice: Go.");
        const controlKeyword: SemanticToken = {
            kind: "ControlKeyword",
            range: {
                start: { line: 0, character: 2 },
                end: { line: 0, character: 6 },
            },
        };

        source.setSemanticTokens([controlKeyword]);

        const region = source.element.querySelector(".source-preview blockquote");
        expect(region?.classList.contains("dd-preview-control-region")).toBe(true);
        expect(region?.getAttribute("title")).toBe("Conditional dialogue");
    });

    it("globally hides ignored Preview regions without changing Source content", () => {
        const source = mountSource(table);
        source.setSemanticTokens([ignoredTable]);

        source.element
            .querySelector<HTMLButtonElement>(
                '.dd-ignored-preview-command[data-command="collapse"]',
            )
            ?.click();

        expect(
            source.element
                .querySelector(".dd-preview-ignored-region")
                ?.classList.contains("dd-ignored-region-hidden"),
        ).toBe(true);
        expect(source.getContent()).toBe(table);
    });

    it("hides one ignored region on its own control, leaving Source content alone", () => {
        const source = mountSource(table);
        source.setSemanticTokens([ignoredTable]);

        source.element.querySelector<HTMLButtonElement>(".dd-ignored-region-toggle")?.click();

        expect(
            source.element
                .querySelector(".dd-preview-ignored-region")
                ?.classList.contains("dd-ignored-region-hidden"),
        ).toBe(true);
        expect(source.getContent()).toBe(table);
    });
});

describe("mapPreviewSpans", () => {
    const span = { start: 0, end: 5 };

    it("keeps text inserted immediately before a construct outside its preview span", () => {
        const state = EditorState.create({ doc: "table" });
        const changes = state.update({ changes: { from: 0, insert: "note\n" } }).changes;

        expect(mapPreviewSpans([span], changes)).toEqual([{ start: 5, end: 10 }]);
    });

    it("keeps text inserted immediately after a construct outside its preview span", () => {
        const state = EditorState.create({ doc: "table" });
        const changes = state.update({ changes: { from: 5, insert: "\nnote" } }).changes;

        expect(mapPreviewSpans([span], changes)).toEqual([span]);
    });

    it("drops a preview span when its construct is deleted", () => {
        const state = EditorState.create({ doc: "table" });
        const changes = state.update({ changes: { from: 0, to: 5 } }).changes;

        expect(mapPreviewSpans([span], changes)).toEqual([]);
    });
});

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
