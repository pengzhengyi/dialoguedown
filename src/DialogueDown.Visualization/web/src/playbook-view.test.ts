import { describe, it, expect, vi } from "vitest";
import { EditorView } from "@codemirror/view";
import { insertNewlineAndIndent } from "@codemirror/commands";
import { createPlaybookView } from "./playbook-view";
import type { PlaybookReport } from "./model";

/** Real enough to jump inside: two speakers, and a second node whose id is not its position. */
const JUMPABLE_JSON = `{
  "entry": 0,
  "speakers": [
    {
      "name": "Alice"
    },
    {
      "name": "(anonymous)"
    }
  ],
  "nodes": [
    {
      "kind": "line",
      "id": 0
    },
    {
      "kind": "line",
      "id": 9
    }
  ]
}`;

/** A compiled playbook with a named speaker, an anonymous default, and one tag. */
function compiled(): PlaybookReport {
    return {
        json: JUMPABLE_JSON,
        metadata: {
            script: "scene.dialogue.md",
            formatVersion: 0,
            schemaUrl: "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
            requires: ["core"],
            uses: [],
            entry: 0,
            nodeCount: 6,
            anchorCount: 2,
        },
        anchors: [{ name: "the-tavern", node: 9 }],
        speakers: [
            {
                id: "alice",
                name: "Alice",
                default: false,
                tags: [{ name: "role", value: "guide", reserved: false }],
            },
            { default: true, tags: [] },
        ],
    };
}

/** One named table panel in the right pane. */
function panel(view: HTMLElement, title: string): HTMLElement | undefined {
    return [...view.querySelectorAll<HTMLElement>(".table-panel")].find(
        (candidate) => candidate.querySelector(".table-panel-title")?.textContent === title,
    );
}

/** The body rows of one named table panel. */
function bodyRows(view: HTMLElement, title: string): HTMLTableRowElement[] {
    return [...(panel(view, title)?.querySelectorAll<HTMLTableRowElement>("tbody tr") ?? [])];
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

    it("summarizes the playbook's header as a field/value panel", () => {
        const view = createPlaybookView(compiled());

        const text = panel(view, "Playbook")?.textContent ?? "";
        expect(text).toContain("scene.dialogue.md");
        expect(text).toContain("core");
        expect(text).toContain("6");
    });

    it("links out to the published schema the playbook names", () => {
        const view = createPlaybookView(compiled());

        const link = view.querySelector<HTMLAnchorElement>(".playbook-schema-link");
        expect(link?.href).toBe(
            "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
        );
        expect(link?.textContent).toBe("playbook-0.schema.json");
        // It leaves the report, so it must not hand the opener a window handle.
        expect(link?.rel).toBe("noopener noreferrer");
    });

    it("says nothing for a header list the playbook did not fill", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Playbook");
        const uses = rows.find((row) => row.cells[0]?.textContent === "Uses");

        expect(uses?.cells[1]?.textContent).toBe("");
    });

    it("names the anonymous default speaker, whose namelessness is the point", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        expect(rows).toHaveLength(2);
        expect(rows[0].textContent).toContain("Alice");
        expect(rows[0].textContent).toContain("alice");
        expect(rows[1].cells[0]?.textContent).toBe("(anonymous)");
    });

    it("ticks the default speaker and leaves the others' cell empty", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        expect(rows[0].cells[2]?.textContent).toBe("");
        expect(rows[1].cells[2]?.textContent).toBe("✓");
    });

    it("draws each tag as a capsule carrying the text to copy", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");
        const chip = rows[0].cells[3]?.querySelector<HTMLElement>(".dd-tag");

        expect(chip?.dataset.copy).toBe("#role=guide");
        expect(chip?.classList.contains("dd-tag-custom")).toBe(true);
        // The identity dot is what tells one writer-invented tag from the next.
        expect(chip?.querySelector(".dd-tag-dot")).not.toBeNull();
    });

    it("copies an @id and an anchor, the identifiers a writer pastes into a script", () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        const view = createPlaybookView(compiled());

        bodyRows(view, "Speakers")[0].cells[1]?.dispatchEvent(
            new MouseEvent("click", { bubbles: true }),
        );
        expect(writeText).toHaveBeenCalledExactlyOnceWith("@alice");

        writeText.mockClear();
        bodyRows(view, "Anchors")[0].cells[0]?.dispatchEvent(
            new MouseEvent("click", { bubbles: true }),
        );
        expect(writeText).toHaveBeenCalledExactlyOnceWith("#the-tavern");
    });

    it("offers a node's number, a speaker's name, and the entry node as jumps", () => {
        // Where the click *lands* is the resolver's business and is tested against a document in
        // playbook-jump.test.ts; what matters here is that the right cells carry the right target.
        const view = createPlaybookView(compiled());

        expect(bodyRows(view, "Anchors")[0].cells[1]?.dataset.jump).toBe('{"kind":"node","id":9}');
        expect(bodyRows(view, "Speakers")[1].cells[0]?.dataset.jump).toBe(
            '{"kind":"speaker","index":1}',
        );
        const entry = bodyRows(view, "Playbook").find(
            (row) => row.cells[0]?.textContent === "Entry node",
        );
        expect(entry?.cells[1]?.dataset.jump).toBe('{"kind":"node","id":0}');
    });

    it("binds a speaker's jump to its place in the array, not to its row", () => {
        // The panels sort and filter, so a row's position is not the speaker's index. Binding at
        // build time is what keeps a sorted table pointing at the right object.
        const view = createPlaybookView(compiled());
        const rows = bodyRows(view, "Speakers");

        expect(rows.map((row) => row.cells[0]?.dataset.jump)).toEqual([
            '{"kind":"speaker","index":0}',
            '{"kind":"speaker","index":1}',
        ]);
    });

    it("leaves prose alone, so only a place in the document is a destination", () => {
        const view = createPlaybookView(compiled());
        const script = bodyRows(view, "Playbook").find(
            (row) => row.cells[0]?.textContent === "Script",
        );

        expect(script?.cells[1]?.dataset.jump).toBeUndefined();
        expect(bodyRows(view, "Anchors")[0].cells[0]?.dataset.jump).toBeUndefined();
    });

    it("copies a tag when it is clicked, the same as the Config tab", () => {
        // The capsule wears a hover ring and carries the text to copy, so it promises a click
        // will work. That promise is the shared table's to keep, not the Config tab's alone.
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        rows[0].cells[3]
            ?.querySelector<HTMLElement>(".dd-tag")
            ?.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        // Exactly once: the table panel wires the copying, so the view must not wire it again.
        expect(writeText).toHaveBeenCalledExactlyOnceWith("#role=guide");
    });

    it("leaves an absent id and an empty tag list as empty cells", () => {
        // Nothing to say, so the table says nothing: the reader's eye goes to the speakers that
        // do carry an id or a tag, not to a column of placeholders.
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        // Written with its `@`, exactly as a script references it.
        expect(rows[0].cells[1]?.textContent).toBe("@alice");
        // Written with its `#`, exactly as a script writes it and as the other two tabs show it.
        expect(rows[0].cells[3]?.textContent).toBe("#role=guide");
        expect(rows[1].cells[1]?.textContent).toBe("");
        expect(rows[1].cells[3]?.textContent).toBe("");
    });

    it("lists every anchor a jump may name, with the node it lands on", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Anchors");

        expect(rows).toHaveLength(1);
        // Written with its `#`, exactly as a jump names it.
        expect(rows[0].cells[0]?.textContent).toBe("#the-tavern");
        expect(rows[0].cells[1]?.textContent).toBe("9");
    });

    it("gives each table the report's panel chrome — a count, a caret, and a search", () => {
        const view = createPlaybookView(compiled());

        expect([...view.querySelectorAll(".table-panel-title")].map((t) => t.textContent)).toEqual([
            "Playbook",
            "Speakers",
            "Anchors",
        ]);
        expect(panel(view, "Speakers")?.querySelector(".table-panel-count")?.textContent).toBe("2");
        expect(panel(view, "Speakers")?.querySelector(".table-panel-search")).not.toBeNull();
        expect(panel(view, "Speakers")?.querySelector(".table-panel-toggle")).not.toBeNull();
    });

    it("explains why there is no playbook instead of showing an empty editor", () => {
        const view = createPlaybookView({
            anchors: [],
            speakers: [],
            unavailable: "The compile did not reach a playbook.",
        });

        expect(view.querySelector(".playbook-source .cm-editor")).toBeNull();
        expect(view.querySelector(".playbook-empty-state")?.textContent).toContain(
            "did not reach a playbook",
        );
        expect(panel(view, "Speakers")?.textContent).toContain("no speakers");
        expect(panel(view, "Anchors")?.textContent).toContain("jumped to by name");
    });

    it("gives the tables panel its own collapse toggle and split", () => {
        const view = createPlaybookView(compiled());

        expect(view.querySelector(".playbook-divider .collapse-toggle")).not.toBeNull();
        expect(view.querySelector(".playbook-side")).not.toBeNull();
    });
});
