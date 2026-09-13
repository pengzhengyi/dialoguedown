import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { elementLine, lineOf, nodeLine } from "./playbook-jump";

/** A playbook whose node ids are deliberately *not* their positions, as the corpus allows. */
const PLAYBOOK = `{
  "$schema": "https://example.test/playbook-0.schema.json",
  "format": {
    "version": 0
  },
  "script": "scene.dialogue.md",
  "entry": 0,
  "anchors": {
    "the-inn": 5
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
      "out": [
        {
          "kind": "next",
          "target": 5
        }
      ]
    },
    {
      "kind": "line",
      "id": 5,
      "out": []
    }
  ]
}`;

function state(doc = PLAYBOOK): EditorState {
    return EditorState.create({ doc });
}

/** The text of a located line, so a failure reads as the line it landed on. */
function textAt(line: number | null, doc = PLAYBOOK): string | null {
    return line === null ? null : doc.split("\n")[line - 1];
}

describe("nodeLine", () => {
    it("finds a node by its id, not by its position", () => {
        // The corpus carries `node-out-of-position` for exactly this reason: ids need not be
        // dense, so the second element's id is 5. Indexing would land on the wrong node.
        const line = nodeLine(state(), 5);

        expect(textAt(line)).toBe("    {");
        expect(textAt((line ?? 0) + 2)).toBe('      "id": 5,');
    });

    it("lands on the opening brace, so the whole node is revealed", () => {
        const line = nodeLine(state(), 0);

        expect(textAt(line)).toBe("    {");
        expect(textAt((line ?? 0) + 1)).toBe('      "kind": "line",');
    });

    it("does not answer for a position that has no matching id", () => {
        // There *is* a second element, so anything indexing the array would happily return it for
        // 1. There is no node with id 1, and the honest answer is nothing.
        expect(elementLine(state(), "nodes", 1)).not.toBeNull();
        expect(nodeLine(state(), 1)).toBeNull();
    });

    it("returns null for a node the playbook does not hold", () => {
        expect(nodeLine(state(), 47)).toBeNull();
    });
});

describe("elementLine", () => {
    it("finds a speaker by index, stepping over an element's nested blocks", () => {
        expect(textAt(elementLine(state(), "speakers", 1))).toBe("    {");
        expect(textAt((elementLine(state(), "speakers", 1) ?? 0) + 1)).toBe('      "name": "Bob",');
    });

    it("returns null past the end of the array", () => {
        expect(elementLine(state(), "speakers", 2)).toBeNull();
    });

    it("returns null when the document has no such array", () => {
        expect(elementLine(state('{\n  "entry": 0\n}'), "speakers", 0)).toBeNull();
    });
});

describe("lineOf", () => {
    it("routes a node target by id and a speaker target by index", () => {
        expect(lineOf(state(), { kind: "node", id: 5 })).toBe(nodeLine(state(), 5));
        expect(lineOf(state(), { kind: "speaker", index: 0 })).toBe(
            elementLine(state(), "speakers", 0),
        );
    });
});
