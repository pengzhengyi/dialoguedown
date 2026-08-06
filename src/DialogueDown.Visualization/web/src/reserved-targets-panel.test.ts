import { afterEach, describe, expect, it, vi } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { reservedTargetsPanel, setEditorReservedTargets } from "./reserved-targets-panel";
import type { ReservedTarget } from "./model";

const END: ReservedTarget = { anchor: "END", label: "End", role: "Terminal" };
const START: ReservedTarget = { anchor: "START", label: "Start", role: "Entry" };

let views: EditorView[] = [];

afterEach(() => {
    for (const view of views) view.destroy();
    views = [];
    document.body.replaceChildren();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

function mount(targets: readonly ReservedTarget[] = []): EditorView {
    const parent = document.createElement("div");
    document.body.appendChild(parent);
    const view = new EditorView({
        parent,
        state: EditorState.create({
            doc: "# Scene\n\nAlice: Hi.",
            extensions: [reservedTargetsPanel()],
        }),
    });
    views.push(view);
    setEditorReservedTargets(view, targets);
    return view;
}

describe("reservedTargetsPanel", () => {
    it("mounts no fixed row when there are no compiler-projected targets", () => {
        const view = mount();

        expect(view.dom.querySelector(".dd-reserved-targets")).toBeNull();
    });

    it("shows the End sentinel as a read-only bottom row with a symbolic marker", () => {
        const view = mount([END]);
        const panel = view.dom.querySelector<HTMLElement>(".dd-reserved-targets")!;

        expect(panel.closest(".cm-panels-bottom")).not.toBeNull();
        expect(panel.querySelector(".dd-reserved-target-marker")?.textContent).toBe("∞");
        expect(panel.querySelector(".dd-reserved-target-label")?.textContent).toBe("End");
        expect(panel.querySelector(".dd-reserved-target-anchor")?.textContent).toBe("#END");
        expect(panel.querySelector("input, textarea, [contenteditable='true']")).toBeNull();
    });

    it("copies the same paste-ready jump link as a source heading", async () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal("navigator", { clipboard: { writeText } });
        const view = mount([END]);

        view.dom.querySelector<HTMLButtonElement>(".dd-reserved-target-copy")!.click();
        await new Promise((resolve) => setTimeout(resolve));

        expect(writeText).toHaveBeenCalledWith("[End](#END)");
        expect(document.querySelector(".toast.visible")?.textContent).toBe("Copied [End](#END)");
        expect(view.state.doc.toString()).toBe("# Scene\n\nAlice: Hi.");
    });

    it("supports future entry and terminal targets without a panel redesign", () => {
        const view = mount([START, END]);
        const rows = [...view.dom.querySelectorAll<HTMLElement>(".dd-reserved-target-row")];

        expect(rows.map((row) => row.dataset.role)).toEqual(["Entry", "Terminal"]);
        expect(
            rows.map((row) => row.querySelector(".dd-reserved-target-marker")?.textContent),
        ).toEqual(["▶", "∞"]);
        expect(rows.map((row) => row.textContent)).toEqual([
            expect.stringContaining("Start#START"),
            expect.stringContaining("End#END"),
        ]);
    });

    it("updates and removes the fixed panel when compiler metadata changes", () => {
        const view = mount([END]);
        expect(view.dom.querySelectorAll(".dd-reserved-target-row")).toHaveLength(1);

        setEditorReservedTargets(view, [START, END]);
        expect(view.dom.querySelectorAll(".dd-reserved-target-row")).toHaveLength(2);

        setEditorReservedTargets(view, []);
        expect(view.dom.querySelector(".dd-reserved-targets")).toBeNull();
    });
});
