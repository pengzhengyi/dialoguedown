import { describe, it, expect } from "vitest";
import { EditorView } from "@codemirror/view";
import { insertNewlineAndIndent } from "@codemirror/commands";
import { createPlaybookView } from "./playbook-view";
import type { PlaybookReport } from "./model";

/** A compiled playbook with a named speaker, an anonymous default, and one tag. */
function compiled(): PlaybookReport {
    return {
        json: '{\n  "version": 0,\n  "entry": 0\n}',
        metadata: {
            script: "scene.dialogue.md",
            formatVersion: 0,
            requires: ["core"],
            uses: [],
            entry: 0,
            nodeCount: 6,
            anchorCount: 2,
        },
        speakers: [
            { id: "alice", name: "Alice", default: false, tags: ["role=guide"] },
            { default: true, tags: [] },
        ],
    };
}

describe("createPlaybookView", () => {
    it("shows the serialized playbook in a read-only editor", () => {
        const view = createPlaybookView(compiled());

        const editor = view.querySelector(".playbook-source .cm-editor");
        expect(editor).not.toBeNull();
        expect(view.querySelector(".playbook-source")?.textContent).toContain('"entry"');
    });

    it("refuses a reader's edit — the playbook is compiled, not authored", () => {
        const view = createPlaybookView(compiled());
        const editor = EditorView.findFromDOM(view.querySelector(".playbook-source .cm-editor")!)!;
        const before = editor.state.doc.toString();
        editor.dispatch({ selection: { anchor: 5 } });

        // The command an editing keystroke runs through: it consults `readOnly` and declines.
        expect(insertNewlineAndIndent(editor)).toBe(false);

        expect(editor.state.doc.toString()).toBe(before);
        expect(
            view.querySelector(".playbook-source .cm-content")?.getAttribute("aria-readonly"),
        ).toBe("true");
    });

    it("summarizes the playbook's header as a label/value table", () => {
        const view = createPlaybookView(compiled());

        const text = view.querySelector(".playbook-metadata-table")?.textContent ?? "";
        expect(text).toContain("scene.dialogue.md");
        expect(text).toContain("core");
        expect(text).toContain("6");
    });

    it("shows an em dash for a header list the playbook left empty", () => {
        const view = createPlaybookView(compiled());

        const rows = [...view.querySelectorAll(".playbook-metadata-table tbody tr")];
        const uses = rows.find((row) => row.querySelector("th")?.textContent === "Uses");
        expect(uses?.querySelector("td")?.textContent).toBe("—");
    });

    it("names the anonymous default speaker rather than showing an empty cell", () => {
        const view = createPlaybookView(compiled());

        const rows = view.querySelectorAll(".playbook-speakers-table tbody tr");
        expect(rows).toHaveLength(2);
        expect(rows[0].textContent).toContain("Alice");
        expect(
            rows[0].querySelector<HTMLElement>(".playbook-copy[data-copy='alice']"),
        ).not.toBeNull();
        expect(rows[1].querySelector(".playbook-anonymous")?.textContent).toBe("(anonymous)");
        expect(rows[1].querySelector(".playbook-default")?.textContent).toBe("yes");
    });

    it("renders each speaker tag as a copyable chip", () => {
        const view = createPlaybookView(compiled());

        const chip = view.querySelector<HTMLElement>(".playbook-tag");
        expect(chip?.textContent).toBe("role=guide");
        expect(chip?.dataset.copy).toBe("role=guide");
    });

    it("explains why there is no playbook instead of showing an empty editor", () => {
        const view = createPlaybookView({
            speakers: [],
            unavailable: "The compile did not reach a playbook.",
        });

        expect(view.querySelector(".playbook-source .cm-editor")).toBeNull();
        expect(view.querySelector(".playbook-empty-state")?.textContent).toContain(
            "did not reach a playbook",
        );
        expect(view.querySelector(".playbook-speakers")?.textContent).toContain("no speakers");
    });

    it("gives the tables panel its own collapse toggle and split", () => {
        const view = createPlaybookView(compiled());

        expect(view.querySelector(".playbook-divider .collapse-toggle")).not.toBeNull();
        expect(view.querySelector(".playbook-side")).not.toBeNull();
    });
});
