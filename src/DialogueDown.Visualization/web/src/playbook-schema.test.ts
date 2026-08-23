import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { schemaPathAt, describeSchemaPath } from "./playbook-schema";

/**
 * A playbook shaped exactly as `JsonSerializer` with `WriteIndented` writes one: two spaces per
 * level, one property per line. The resolver's whole contract is reading a path out of that
 * shape, so the fixture must not be prettier than the real thing.
 */
const PLAYBOOK = `{
  "$schema": "https://example.invalid/playbook-0.schema.json",
  "format": {
    "version": 0,
    "requires": [
      "core"
    ]
  },
  "script": "scene.dialogue.md",
  "anchors": {
    "the-tavern": 0
  },
  "speakers": [
    {
      "id": "keeper",
      "tags": [
        {
          "name": "wary"
        }
      ]
    }
  ],
  "nodes": [
    {
      "kind": "line",
      "id": 1,
      "speech": [
        {
          "kind": "text",
          "text": "Hello."
        },
        {
          "kind": "styled",
          "style": "italic",
          "children": [
            {
              "kind": "text",
              "text": "quietly"
            }
          ]
        }
      ]
    }
  ]
}`;

const state = EditorState.create({ doc: PLAYBOOK });
const lineOf = (needle: string): number =>
    PLAYBOOK.split("\n").findIndex((line) => line.includes(needle)) + 1;
const pathOf = (needle: string): string => schemaPathAt(state, lineOf(needle)).path;

describe("schemaPathAt", () => {
    it("names a top-level property", () => {
        expect(pathOf('"script"')).toBe("script");
        expect(pathOf('"$schema"')).toBe("$schema");
    });

    it("names a property nested in an object", () => {
        expect(pathOf('"version"')).toBe("format/version");
        expect(pathOf('"requires"')).toBe("format/requires");
    });

    it("marks an array element with a star rather than its index", () => {
        expect(pathOf('"core"')).toBe("format/requires/*");
    });

    it("walks through an array of objects", () => {
        expect(pathOf('"id"')).toBe("speakers/*/id");
        expect(pathOf('"kind": "line"')).toBe("nodes/*/kind");
    });

    it("walks through nested arrays of objects", () => {
        expect(pathOf('"text": "quietly"')).toBe("nodes/*/speech/*/children/*/text");
        expect(pathOf('"name": "wary"')).toBe("speakers/*/tags/*/name");
    });

    it("treats a map's key as an element, since the schema describes them alike", () => {
        expect(pathOf('"the-tavern"')).toBe("anchors/the-tavern");
    });

    it("gives an element opener the path of the element itself", () => {
        // The bare `{` that opens the first speaker.
        expect(schemaPathAt(state, lineOf('"id"') - 1).path).toBe("speakers/*");
    });
});

const describe_ = (needle: string) => {
    const located = schemaPathAt(state, lineOf(needle));
    return describeSchemaPath(located.path, located.kinds);
};

describe("describeSchemaPath", () => {
    it("gives the format's own words for a described property", () => {
        expect(describe_('"requires"')?.description).toMatch(/Capabilities a runtime must/);
        expect(describe_('"requires"')?.path).toBe("format/requires");
    });

    it("resolves a map's own key through additionalProperties", () => {
        expect(describe_('"the-tavern"')?.path).toBe("anchors/the-tavern");
    });

    it("picks the variant the document's kind names, not just the shape holding it", () => {
        // `nodes/*` is a oneOf tagged by `kind`; the `line` branch is the one this node is.
        expect(describe_('"kind": "line"')?.description).toBe("A line somebody says.");
        expect(describe_('"kind": "text"')?.description).toBe("Plain words, as written.");
    });

    it("falls back to the enclosing shape for a leaf the format leaves undocumented", () => {
        // The schema documents the variant, not its `text` field, so the answer is the variant's
        // — reported under the variant's path so it is never mistaken for the leaf's own.
        const text = describe_('"text": "Hello."');
        expect(text?.path).toBe("nodes/*/speech/*");
        expect(text?.description).toBe("Plain words, as written.");
    });

    it("describes nothing outside the format", () => {
        expect(describeSchemaPath("nowhere/at/all")).toBeNull();
    });
});
