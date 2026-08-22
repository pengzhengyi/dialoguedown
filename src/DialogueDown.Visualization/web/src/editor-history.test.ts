import { describe, it, expect, afterEach } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { undo, undoDepth } from "@codemirror/commands";
import { documentHistory, openDocument, setDocumentContent } from "./editor-history";

let views: EditorView[] = [];

afterEach(() => {
    for (const view of views) view.destroy();
    views = [];
});

function editor(doc: string): EditorView {
    const view = new EditorView({
        state: EditorState.create({ doc, extensions: [documentHistory()] }),
        parent: document.body,
    });
    views.push(view);
    return view;
}

describe("setDocumentContent", () => {
    it("keeps the history, so the reader can undo back to the earlier text", () => {
        const view = editor("first\n");

        setDocumentContent(view, "second\n");
        expect(undoDepth(view.state)).toBe(1);
        undo(view);

        expect(view.state.doc.toString()).toBe("first\n");
    });
});

describe("openDocument", () => {
    it("drops the history, so undo cannot reach the document just left behind", () => {
        const view = editor("first\n");

        openDocument(view, "second\n");
        expect(undoDepth(view.state)).toBe(0);
        undo(view);

        // Undoing into another script's text would leave it in this buffer, and the next save
        // would write it to the wrong file.
        expect(view.state.doc.toString()).toBe("second\n");
    });

    it("starts a fresh document at its beginning", () => {
        const view = editor("first line\nsecond line\n");
        view.dispatch({ selection: { anchor: 15 } });

        openDocument(view, "another\n");

        expect(view.state.selection.main.head).toBe(0);
    });

    it("still records edits made after the new document is open", () => {
        const view = editor("first\n");

        openDocument(view, "second\n");
        view.dispatch({ changes: { from: 0, insert: "typed " } });
        undo(view);

        expect(view.state.doc.toString()).toBe("second\n");
    });
});
