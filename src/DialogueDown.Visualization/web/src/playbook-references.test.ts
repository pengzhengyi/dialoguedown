import { describe, it, expect } from "vitest";
import { EditorSelection, EditorState } from "@codemirror/state";
import { EditorView, keymap } from "@codemirror/view";
import {
    referenceKindAt,
    referenceRanges,
    definitionLineFor,
    playbookReferences,
    playbookReferenceKeymap,
} from "./playbook-references";

/**
 * A playbook whose node ids are deliberately not their positions — the corpus allows sparse
 * ids — so a jump that indexed the array would land on the wrong node. Shaped exactly as
 * `JsonSerializer` with `WriteIndented` writes one: two spaces per level, one property per line.
 */
const PLAYBOOK = `{
  "$schema": "https://example.invalid/playbook-0.schema.json",
  "format": {
    "version": 0
  },
  "script": "scene.dialogue.md",
  "entry": 0,
  "anchors": {
    "the-inn": 9
  },
  "speakers": [
    {
      "name": "Alice",
      "tags": []
    },
    {
      "name": "Bob",
      "id": "bob",
      "tags": []
    }
  ],
  "nodes": [
    {
      "kind": "line",
      "id": 0,
      "speaker": 1,
      "out": [
        {
          "kind": "succession",
          "target": 9
        }
      ]
    },
    {
      "kind": "end",
      "id": 9,
      "out": []
    }
  ]
}`;

const state = (doc = PLAYBOOK): EditorState => EditorState.create({ doc });
const lineWith = (needle: string, doc = PLAYBOOK): number =>
    doc.split("\n").findIndex((line) => line.includes(needle)) + 1;
/** The `{` line opening the object whose first property is `kindNeedle`. */
const openerBefore = (kindNeedle: string, doc = PLAYBOOK): number => lineWith(kindNeedle, doc) - 1;

describe("referenceKindAt", () => {
    const kindOf = (needle: string): ReturnType<typeof referenceKindAt> =>
        referenceKindAt(state(), lineWith(needle));

    it("reads a node reference on entry, an anchor, and an edge target", () => {
        expect(kindOf('"entry": 0')).toBe("node");
        expect(kindOf('"the-inn": 9')).toBe("node");
        expect(kindOf('"target": 9')).toBe("node");
    });

    it("reads a speaker reference on a line's speaker", () => {
        expect(kindOf('"speaker": 1')).toBe("speaker");
    });

    it("does not treat a node's own id as a reference", () => {
        expect(kindOf('"id": 0')).toBeNull();
        expect(kindOf('"id": 9')).toBeNull();
    });

    it("does not treat a plain number as a reference", () => {
        expect(kindOf('"version": 0')).toBeNull();
    });

    it("ignores a non-numeric value and a line with no property", () => {
        expect(kindOf('"id": "bob"')).toBeNull();
        expect(kindOf('"out": [')).toBeNull();
        expect(kindOf('"the-inn"')).toBe("node"); // sanity: the numbered line still resolves
    });
});

describe("referenceRanges", () => {
    const all = (doc = PLAYBOOK): ReturnType<typeof referenceRanges> => {
        const s = state(doc);
        return referenceRanges(s, 0, s.doc.length);
    };

    it("finds every reference, and nothing that is not one", () => {
        expect(all().map((range) => range.value)).toEqual([0, 9, 1, 9]);
        expect(all().map((range) => range.kind)).toEqual(["node", "node", "speaker", "node"]);
    });

    it("spans exactly the digits", () => {
        const s = state();
        for (const range of referenceRanges(s, 0, s.doc.length)) {
            expect(s.sliceDoc(range.from, range.to)).toBe(String(range.value));
        }
    });

    it("returns only the references within the given range", () => {
        const s = state();
        const speakerLine = s.doc.line(lineWith('"speaker": 1'));
        const found = referenceRanges(s, speakerLine.from, speakerLine.to);

        expect(found).toHaveLength(1);
        expect(found[0]).toMatchObject({ kind: "speaker", value: 1 });
    });

    it("finds nothing in a stretch with no reference", () => {
        const s = state();
        const scriptLine = s.doc.line(lineWith('"script"'));
        expect(referenceRanges(s, scriptLine.from, scriptLine.to)).toEqual([]);
    });
});

describe("definitionLineFor", () => {
    it("resolves an edge target to the node with that id, not that position", () => {
        const s = state();
        // The target is 9; the node with id 9 is the *second* element. Indexing would return the
        // first. `nodeLine` reads each element's own id.
        expect(definitionLineFor(s, lineWith('"target": 9'))).toBe(openerBefore('"kind": "end"'));
    });

    it("resolves a speaker reference to that speaker by position", () => {
        const s = state();
        expect(definitionLineFor(s, lineWith('"speaker": 1'))).toBe(openerBefore('"name": "Bob"'));
    });

    it("resolves entry and an anchor to their node", () => {
        const s = state();
        expect(definitionLineFor(s, lineWith('"entry": 0'))).toBe(openerBefore('"kind": "line"'));
        expect(definitionLineFor(s, lineWith('"the-inn": 9'))).toBe(openerBefore('"kind": "end"'));
    });

    it("is null on an id line and on a non-reference", () => {
        const s = state();
        expect(definitionLineFor(s, lineWith('"id": 0'))).toBeNull();
        expect(definitionLineFor(s, lineWith('"version": 0'))).toBeNull();
    });

    it("is null when the target is not in the document", () => {
        const missing = PLAYBOOK.replace('"target": 9', '"target": 47');
        expect(definitionLineFor(state(missing), lineWith('"target": 47', missing))).toBeNull();
    });
});

describe("F12 Go to Definition", () => {
    /** Mount an editor over the playbook with the reference extension and its keymap. */
    const mount = (doc = PLAYBOOK): EditorView => {
        const parent = document.createElement("div");
        document.body.appendChild(parent);
        return new EditorView({
            parent,
            state: EditorState.create({
                doc,
                extensions: [playbookReferences(), keymap.of([...playbookReferenceKeymap])],
            }),
        });
    };

    /** Run the F12 binding with the cursor somewhere on `lineNumber`. */
    const pressF12 = (view: EditorView, lineNumber: number): boolean => {
        view.dispatch({ selection: EditorSelection.cursor(view.state.doc.line(lineNumber).from) });
        const binding = playbookReferenceKeymap[0];
        return binding.run?.(view) ?? false;
    };

    it("moves the cursor to the definition of the reference on the line", () => {
        const view = mount();

        expect(pressF12(view, lineWith('"target": 9'))).toBe(true);
        expect(view.state.doc.lineAt(view.state.selection.main.head).number).toBe(
            openerBefore('"kind": "end"'),
        );

        view.destroy();
    });

    it("does nothing on a line that is not a reference", () => {
        const view = mount();
        const versionLine = view.state.doc.line(lineWith('"version": 0'));

        expect(pressF12(view, versionLine.number)).toBe(false);
        // The cursor stayed where the test put it, rather than jumping anywhere.
        expect(view.state.selection.main.head).toBe(versionLine.from);

        view.destroy();
    });
});

describe("reference marks", () => {
    const mount = (doc = PLAYBOOK): EditorView => {
        const parent = document.createElement("div");
        document.body.appendChild(parent);
        return new EditorView({
            parent,
            state: EditorState.create({ doc, extensions: [playbookReferences()] }),
        });
    };

    /** The `[from, to)` the editor currently decorates, in document order. */
    const painted = (view: EditorView): Array<{ from: number; to: number }> => {
        const out: Array<{ from: number; to: number }> = [];
        for (const source of view.state.facet(EditorView.decorations)) {
            const set = typeof source === "function" ? source(view) : source;
            set.between(0, view.state.doc.length, (from, to) => {
                out.push({ from, to });
            });
        }
        return out;
    };

    it("marks exactly the reference digits the ranges name", () => {
        const view = mount();
        const expected = referenceRanges(view.state, 0, view.state.doc.length).map(
            ({ from, to }) => ({ from, to }),
        );

        expect(painted(view)).toEqual(expected);
        expect(expected).toHaveLength(4); // entry, anchor, speaker, edge target

        view.destroy();
    });

    it("follows a reference when its mark is clicked", () => {
        const view = mount();
        const span = view.dom.querySelector(".dd-playbook-ref");
        expect(span).not.toBeNull();

        span?.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));

        // The first mark is `"entry": 0`, whose definition is the node with id 0.
        expect(view.state.doc.lineAt(view.state.selection.main.head).number).toBe(
            openerBefore('"kind": "line"'),
        );

        view.destroy();
    });

    it("does not follow a click that misses every mark", () => {
        const view = mount();
        view.dispatch({ selection: EditorSelection.cursor(1) });
        const before = view.state.selection.main.head;

        // A mousedown whose target is a plain text span, not a reference mark: the handler's
        // `closest(".dd-playbook-ref")` finds nothing and leaves the event alone.
        const plain = view.contentDOM.querySelector(".cm-line");
        plain?.dispatchEvent(new MouseEvent("mousedown", { bubbles: true, cancelable: true }));

        // No reveal ran: the caret is still on the opening line, not a node deep in the document.
        expect(view.state.doc.lineAt(before).number).toBe(1);
        expect(view.state.doc.lineAt(view.state.selection.main.head).number).toBeLessThan(3);

        view.destroy();
    });
});
