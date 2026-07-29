import { describe, it, expect, beforeEach } from "vitest";
import { createDetailPanel, type DetailPanel } from "./detail-panel";
import { colorOf } from "./palette";
import type { DisplayNode } from "./model";

describe("createDetailPanel", () => {
    let panel: DetailPanel;
    let title: HTMLElement;
    let body: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        title = document.getElementById("detail-title")!;
        body = document.getElementById("detail-body")!;
        panel = createDetailPanel();
    });

    const note = () => body.querySelector<HTMLElement>(".node-note");
    const editor = () => body.querySelector(".node-source .cm-editor");

    it("starts with a placeholder after clear", () => {
        panel.clear();
        expect(title.textContent).toBe("Node details");
        expect(note()?.hidden).toBe(false);
        expect(note()?.textContent).toContain("Click any node");
    });

    it("shows a category color dot and the escaped label", () => {
        const node: DisplayNode = {
            id: "n1",
            label: "Code <span>",
            category: "call",
            attributes: [],
        };
        panel.show(node);
        const dot = title.querySelector<HTMLElement>(".dot");
        expect(dot?.style.background).toBeTruthy();
        expect(title.textContent).toContain("Code <span>");
        expect(title.innerHTML).toContain("&lt;span&gt;");
        const probe = document.createElement("span");
        probe.style.background = colorOf("call");
        expect(dot?.style.background).toBe(probe.style.background);
    });

    it("omits the dot when the node has no category", () => {
        panel.show({ id: "n1", label: "Text", attributes: [] });
        expect(title.querySelector(".dot")).toBeNull();
    });

    it("renders a table of attributes", () => {
        panel.show({
            id: "n1",
            label: "Heading",
            attributes: [
                { name: "level", value: "2" },
                { name: "text", value: "Scene" },
            ],
        });
        const rows = body.querySelectorAll(".node-attributes table tr");
        expect(rows).toHaveLength(2);
        expect(rows[0].querySelector("th")?.textContent).toBe("level");
        expect(rows[0].querySelector("td")?.textContent).toBe("2");
    });

    it("mounts an editor showing the node's source when source is present", () => {
        panel.show({
            id: "n1",
            label: "Heading",
            attributes: [],
            source: "# Scene",
            span: { start: 0, end: 7 },
        });
        expect(editor()).not.toBeNull();
        expect(body.querySelector(".node-source")?.textContent).toContain("# Scene");
        expect(note()?.hidden).toBe(true);
    });

    it("shows an inserted note (no editor) for a synthetic node with no source", () => {
        panel.show({ id: "n1", label: "Speaker (default)", attributes: [] });
        expect(note()?.hidden).toBe(false);
        expect(note()?.textContent).toContain("Inserted by the compiler");
        expect(note()?.textContent).toContain("names no speaker");
        // A static panel (no edit) has nothing to act on, so it offers no edit call to action.
        expect(note()?.textContent).not.toContain("Edit the line");
    });

    it("adds an edit call to action to the synthetic note when the session is editable", () => {
        const editablePanel = createDetailPanel({
            edit: {
                isEditable: () => true,
                getDocument: () => "",
                onNodeEdit: () => {},
            },
        });
        editablePanel.show({ id: "n1", label: "Speaker (default)", attributes: [] });
        expect(note()?.textContent).toContain("names no speaker");
        expect(note()?.textContent).toContain("Edit the line to name one");
    });

    it("reuses the one editor across selections, swapping its content", () => {
        panel.show({
            id: "n1",
            label: "A",
            attributes: [],
            source: "# A",
            span: { start: 0, end: 3 },
        });
        const first = editor();
        panel.show({
            id: "n2",
            label: "B",
            attributes: [],
            source: "# B",
            span: { start: 0, end: 3 },
        });
        expect(editor()).toBe(first); // same editor instance
        expect(body.querySelector(".node-source")?.textContent).toContain("# B");
    });

    it("hides the editor and shows the note when a synthetic node follows a sourced one", () => {
        panel.show({
            id: "n1",
            label: "A",
            attributes: [],
            source: "# A",
            span: { start: 0, end: 3 },
        });
        panel.show({ id: "n2", label: "Speaker (default)", attributes: [] });
        expect(note()?.hidden).toBe(false);
    });

    describe("jump to source", () => {
        const jumpButton = () => body.querySelector<HTMLButtonElement>(".node-jump");

        it("offers no jump affordance when no jump handler is provided", () => {
            panel.show({
                id: "n1",
                label: "A",
                attributes: [],
                source: "# A",
                span: { start: 0, end: 3 },
            });
            expect(jumpButton()).toBeNull();
        });

        it("selects a real node's span, labeling the action as a selection", () => {
            const jumps: Array<{ start: number; end: number }> = [];
            const jumping = createDetailPanel({ jumpToSource: (span) => jumps.push(span) });
            jumping.show({
                id: "n1",
                label: "Line",
                attributes: [],
                source: "Alice: Hi",
                span: { start: 5, end: 14 },
            });

            const button = jumpButton()!;
            expect(button.hidden).toBe(false);
            expect(button.title).toContain("Select");
            button.click();
            expect(jumps).toEqual([{ start: 5, end: 14 }]);
        });

        it("places the caret for a synthetic node's zero-width span, labeling it a position", () => {
            const jumps: Array<{ start: number; end: number }> = [];
            const jumping = createDetailPanel({ jumpToSource: (span) => jumps.push(span) });
            // A synthetic node has no source (shows the note) but now carries a zero-width caret.
            jumping.show({
                id: "n1",
                label: "Speaker (default)",
                attributes: [],
                span: { start: 7, end: 7 },
            });

            const button = jumpButton()!;
            expect(button.hidden).toBe(false);
            expect(button.title).toContain("position");
            button.click();
            expect(jumps).toEqual([{ start: 7, end: 7 }]);
        });

        it("hides the jump for a node with no span, and after clear", () => {
            const jumping = createDetailPanel({ jumpToSource: () => {} });
            jumping.show({ id: "n1", label: "Orphan", attributes: [] });
            expect(jumpButton()?.hidden).toBe(true);

            jumping.show({
                id: "n2",
                label: "Line",
                attributes: [],
                source: "Hi",
                span: { start: 0, end: 2 },
            });
            expect(jumpButton()?.hidden).toBe(false);

            jumping.clear();
            expect(jumpButton()?.hidden).toBe(true);
        });
    });
});
