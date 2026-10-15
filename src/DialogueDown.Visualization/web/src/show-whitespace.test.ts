import { describe, it, expect, afterEach, vi } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import {
    isWhitespaceVisible,
    onWhitespaceVisibilityChange,
    readWhitespacePreference,
    setWhitespaceVisible,
    whitespaceVisibility,
} from "./show-whitespace";

/** A stand-in for `localStorage`, which a sandboxed report may not have. */
function storageOf(entries: Record<string, string> = {}): Storage {
    const map = new Map(Object.entries(entries));
    return {
        get length() {
            return map.size;
        },
        clear: () => map.clear(),
        getItem: (key: string) => map.get(key) ?? null,
        key: (index: number) => [...map.keys()][index] ?? null,
        removeItem: (key: string) => void map.delete(key),
        setItem: (key: string, value: string) => void map.set(key, value),
    } as Storage;
}

/** Storage that throws on every access, as a sandboxed `file://` report's does. */
const BLOCKED_STORAGE = {
    getItem: () => {
        throw new Error("blocked");
    },
    setItem: () => {
        throw new Error("blocked");
    },
} as unknown as Storage;

let views: EditorView[] = [];

afterEach(() => {
    for (const view of views) view.destroy();
    views = [];
    setWhitespaceVisible(false, storageOf());
});

/** An editor over `doc` that takes part in the shared whitespace switch. */
function editor(doc: string): EditorView {
    const view = new EditorView({
        state: EditorState.create({ doc, extensions: [whitespaceVisibility()] }),
        parent: document.body,
    });
    views.push(view);
    return view;
}

/** How many spaces and tabs the editor marks as visible whitespace. */
function markedWhitespace(view: EditorView): { spaces: number; tabs: number } {
    return {
        spaces: view.contentDOM.querySelectorAll(".cm-highlightSpace").length,
        tabs: view.contentDOM.querySelectorAll(".cm-highlightTab").length,
    };
}

describe("readWhitespacePreference", () => {
    it("hides whitespace until the reader asks to see it", () => {
        expect(readWhitespacePreference(storageOf())).toBe(false);
    });

    it("reads back a remembered choice", () => {
        const storage = storageOf();

        setWhitespaceVisible(true, storage);

        expect(readWhitespacePreference(storage)).toBe(true);
    });

    it("ignores a value it did not write", () => {
        expect(readWhitespacePreference(storageOf({ "dd-show-whitespace": "yes" }))).toBe(false);
    });

    it("hides whitespace when storage is unavailable", () => {
        expect(readWhitespacePreference(BLOCKED_STORAGE)).toBe(false);
        expect(readWhitespacePreference(undefined)).toBe(false);
    });
});

describe("whitespaceVisibility", () => {
    it("marks nothing while whitespace is hidden", () => {
        const view = editor("Alice:  hello\n\tBob: hi");

        expect(markedWhitespace(view)).toEqual({ spaces: 0, tabs: 0 });
    });

    it("marks every space and tab once whitespace is shown", () => {
        const view = editor("Alice:  hello\n\tBob: hi");

        setWhitespaceVisible(true, storageOf());

        // "Alice:␣␣hello" has two spaces; "\tBob:␣hi" has a tab and one space.
        expect(markedWhitespace(view)).toEqual({ spaces: 3, tabs: 1 });
    });

    it("switches every open editor together", () => {
        const source = editor("a b");
        const config = editor("c d");

        setWhitespaceVisible(true, storageOf());

        expect(markedWhitespace(source).spaces).toBe(1);
        expect(markedWhitespace(config).spaces).toBe(1);
    });

    it("opens a new editor in the current setting", () => {
        setWhitespaceVisible(true, storageOf());

        const view = editor("a b");

        expect(markedWhitespace(view).spaces).toBe(1);
    });

    it("hides the marks again when whitespace is hidden", () => {
        const view = editor("a b");
        setWhitespaceVisible(true, storageOf());

        setWhitespaceVisible(false, storageOf());

        expect(markedWhitespace(view).spaces).toBe(0);
    });

    it("keeps the document, selection, and undo history across a switch", () => {
        const view = editor("a b");
        view.dispatch({ selection: { anchor: 2 } });

        setWhitespaceVisible(true, storageOf());

        expect(view.state.doc.toString()).toBe("a b");
        expect(view.state.selection.main.head).toBe(2);
    });

    it("stops switching an editor once it is destroyed", () => {
        const view = editor("a b");
        view.destroy();

        expect(() => setWhitespaceVisible(true, storageOf())).not.toThrow();
    });
});

describe("setWhitespaceVisible", () => {
    it("remembers the choice for the next visit", () => {
        const storage = storageOf();

        setWhitespaceVisible(true, storage);

        expect(storage.getItem("dd-show-whitespace")).toBe("true");
        expect(isWhitespaceVisible()).toBe(true);
    });

    it("still applies the choice when storage is unavailable", () => {
        const view = editor("a b");

        setWhitespaceVisible(true, BLOCKED_STORAGE);

        expect(isWhitespaceVisible()).toBe(true);
        expect(markedWhitespace(view).spaces).toBe(1);
    });

    it("tells each listener the new setting", () => {
        const listener = vi.fn();
        const unsubscribe = onWhitespaceVisibilityChange(listener);

        setWhitespaceVisible(true, storageOf());
        unsubscribe();
        setWhitespaceVisible(false, storageOf());

        expect(listener.mock.calls).toEqual([[true]]);
    });
});

describe("isWhitespaceVisible", () => {
    it("starts from the choice remembered in localStorage", async () => {
        localStorage.setItem("dd-show-whitespace", "true");
        vi.resetModules();

        const fresh = await import("./show-whitespace");

        expect(fresh.isWhitespaceVisible()).toBe(true);
        localStorage.removeItem("dd-show-whitespace");
    });
});
