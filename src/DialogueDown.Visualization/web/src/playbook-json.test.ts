import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { blockEnd, depthOf, opensBlock } from "./playbook-json";

/**
 * A playbook shaped exactly as `JsonSerializer` with `WriteIndented` writes one: two spaces per
 * level, one property or one bracket per line. Every helper here reads that shape, so the fixture
 * must not be tidier than the real output.
 */
const PLAYBOOK = `{
  "format": {
    "version": 0,
    "requires": [
      "core"
    ]
  },
  "script": "scene.dialogue.md",
  "speakers": [
    {
      "id": "keeper"
    }
  ]
}`;

const state = EditorState.create({ doc: PLAYBOOK });
const lineOf = (needle: string): number =>
    PLAYBOOK.split("\n").findIndex((line) => line.includes(needle)) + 1;

describe("depthOf", () => {
    it("counts the writer's two-space levels", () => {
        expect(depthOf("{")).toBe(0);
        expect(depthOf('  "script": "x"')).toBe(1);
        expect(depthOf('      "core"')).toBe(3);
    });
});

describe("opensBlock", () => {
    it("is true only for a line a block hangs beneath", () => {
        expect(opensBlock('  "format": {')).toBe(true);
        expect(opensBlock('    "requires": [')).toBe(true);
        expect(opensBlock('  "script": "scene.dialogue.md",')).toBe(false);
        expect(opensBlock("  },")).toBe(false);
    });
});

describe("blockEnd", () => {
    it("finds the line closing the block", () => {
        // `"format": {` closes at the `},` two lines after its own last member.
        expect(blockEnd(state, lineOf('"format"'))).toBe(lineOf('"script"') - 1);
    });

    it("is null for a line that opens nothing", () => {
        expect(blockEnd(state, lineOf('"script"'))).toBeNull();
    });
});
